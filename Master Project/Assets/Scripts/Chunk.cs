using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Chunk : MonoBehaviour
{
    public byte[] VoxelData { get; private set; }

    public Vector3Int GridCoord { get; private set; }

    private int size;
    private World worldRef;
    private ComputeShader voxelShader;

    [SerializeField] public MeshFilter meshFilter;
    [SerializeField] public MeshRenderer meshRenderer;

    [SerializeField] ComputeShader assignedShader;
    struct FaceData
    {
        public int[] vertIndices;
        public Vector3Int direction;
        public FaceData(int[] i, Vector3Int d) { vertIndices = i; direction = d; }
    }

    readonly Vector3[] vertexPos = new Vector3[8] {
        new Vector3(-0.5f, 0.5f,-0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3( 0.5f, 0.5f, 0.5f), new Vector3( 0.5f, 0.5f,-0.5f),
        new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(-0.5f,-0.5f, 0.5f), new Vector3( 0.5f,-0.5f, 0.5f), new Vector3( 0.5f,-0.5f,-0.5f)
    };

    readonly FaceData[] faces = new FaceData[] {
        new FaceData(new int[]{0,1,2,3}, Vector3Int.up),
        new FaceData(new int[]{5,4,7,6}, Vector3Int.down),
        new FaceData(new int[]{1,5,6,2}, new Vector3Int(0,0,1)), // Front
        new FaceData(new int[]{3,7,4,0}, new Vector3Int(0,0,-1)),// Back
        new FaceData(new int[]{2,6,7,3}, Vector3Int.right),
        new FaceData(new int[]{0,4,5,1}, Vector3Int.left)
    };

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector3> uvs = new List<Vector3>();

    public void InitializeData(Vector3Int coord, int chunkSize, World world)
    {
        this.GridCoord = coord;
        this.size = chunkSize;
        this.worldRef = world;
        this.voxelShader = assignedShader;

        int totalVoxels = size * size * size;
        if (VoxelData == null || VoxelData.Length != totalVoxels)
        {
            VoxelData = new byte[totalVoxels];
        }

        ComputeBuffer buffer = new ComputeBuffer(totalVoxels / 4, 4);

        buffer.SetData(new uint[totalVoxels / 4]);
        int kernel = voxelShader.FindKernel("GenerateVoxelData");
        voxelShader.SetBuffer(kernel, "ResultBuffer", buffer);
        voxelShader.SetInt("ChunkSize", size);
        voxelShader.SetVector("ChunkOffset", transform.position);

        voxelShader.Dispatch(kernel, size / 8, size / 8, size / 8);

        AsyncGPUReadback.Request(buffer, (AsyncGPUReadbackRequest request) =>
        {
            if (request.hasError) return;

            var data = request.GetData<byte>();

            if (data.Length == VoxelData.Length)
            {
                data.CopyTo(VoxelData);
            }

            buffer.Dispose();

            worldRef.OnDataReady(this);
        });
    }
    public void UpdateMesh()
    {
        vertices.Clear(); triangles.Clear(); uvs.Clear();

        Mesh mesh = meshFilter.sharedMesh;

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            meshFilter.sharedMesh = mesh;
        }
        else
        {
            mesh.Clear();
        }

        Chunk nUp = worldRef.GetChunk(GridCoord + Vector3Int.up);
        Chunk nDown = worldRef.GetChunk(GridCoord + Vector3Int.down);
        Chunk nRight = worldRef.GetChunk(GridCoord + Vector3Int.right);
        Chunk nLeft = worldRef.GetChunk(GridCoord + Vector3Int.left);
        Chunk nFront = worldRef.GetChunk(GridCoord + new Vector3Int(0, 0, 1));
        Chunk nBack = worldRef.GetChunk(GridCoord + new Vector3Int(0, 0, -1));

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    if (!IsSolidFast(x, y, z, nUp, nDown, nRight, nLeft, nFront, nBack)) continue;


                    int myBlockID = GetBlockLocal(x, y, z);
                    if (myBlockID == 0) continue;

                    Vector3Int pos = new Vector3Int(x, y, z);
                    BlockType renderType = (BlockType)myBlockID;

                    if (renderType == BlockType.Grass)
                    {
                        if (IsSolidFast(x, y + 1, z, nUp, nDown, nRight, nLeft, nFront, nBack))
                        {
                            renderType = BlockType.Dirt;
                        }
                    }
                    else if (renderType == BlockType.Dirt)
                    {
                        if (!IsSolidFast(x, y + 1, z, nUp, nDown, nRight, nLeft, nFront, nBack))
                        {
                            renderType = BlockType.Grass;
                        }
                    }

                    foreach (var face in faces)
                    {
                        Vector3Int neighborPos = pos + face.direction;
                        if (!IsSolidFast(neighborPos.x, neighborPos.y, neighborPos.z, nUp, nDown, nRight, nLeft, nFront, nBack))
                        {
                            AddFace(pos, face, renderType);
                        }
                    }
                }
            }
        }
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
    bool IsSolidFast(int x, int y, int z, Chunk up, Chunk down, Chunk right, Chunk left, Chunk front, Chunk back)
    {
        if (x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size)
        {
            return VoxelData[x + (y * size) + (z * size * size)] != 0;
        }

        Chunk target = null;
        if (x < 0) target = left;
        else if (x >= size) target = right;
        else if (y < 0) target = down;
        else if (y >= size) target = up;
        else if (z < 0) target = back;
        else if (z >= size) target = front;

        if (target != null)
        {
            int nx = ((x % size) + size) % size;
            int ny = ((y % size) + size) % size;
            int nz = ((z % size) + size) % size;
            return target.VoxelData[nx + (ny * size) + (nz * size * size)] != 0;
        }

        return false;
    }

    int GetBlockLocal(int x, int y, int z)
    {
        return VoxelData[x + (y * size) + (z * size * size)];
    }

    void AddFace(Vector3Int pos, FaceData face, BlockType type)
    {
        int v = vertices.Count;
        foreach (int i in face.vertIndices) vertices.Add(vertexPos[i] + pos);
        triangles.AddRange(new int[] { v, v + 1, v + 2, v + 2, v + 3, v });


        float idx = 0; // Default is Magenta

        if (type == BlockType.Grass)
        {
            idx = (face.direction.y > 0 ? 1 : (face.direction.y < 0 ? 3 : 2));
        }
        else if (type == BlockType.Dirt)
        {
            idx = 3; // Dirt
        }
        else if (type == BlockType.Stone)
        {
            idx = 4; // Stone
        }
        else if ((int)type == 4)
        {
            idx = 5; // Island Debug Color
        }

        uvs.Add(new Vector3(0, 1, idx)); uvs.Add(new Vector3(0, 0, idx));
        uvs.Add(new Vector3(1, 0, idx)); uvs.Add(new Vector3(1, 1, idx));
    }
}