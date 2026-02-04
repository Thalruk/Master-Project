using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public byte[] VoxelData { get; private set; }

    public Vector3Int GridCoord { get; private set; }

    private int size;
    private World worldRef;
    private ComputeShader voxelShader;

    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;

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
        VoxelData = new byte[totalVoxels];

        ComputeBuffer buffer = new ComputeBuffer(totalVoxels / 4, 4);
        try
        {
            buffer.SetData(VoxelData);
            int kernel = voxelShader.FindKernel("CSMain");
            voxelShader.SetBuffer(kernel, "ResultBuffer", buffer);
            voxelShader.SetInt("ChunkSize", size);

            voxelShader.SetVector("ChunkOffset", transform.position);

            voxelShader.Dispatch(kernel, size / 8, size / 8, size / 8);
            buffer.GetData(VoxelData);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public void UpdateMesh()
    {
        vertices.Clear(); triangles.Clear(); uvs.Clear();
        if (meshFilter.sharedMesh != null) meshFilter.sharedMesh.Clear();

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {

                    if (!IsSolid(x, y, z)) continue;

                    Vector3Int pos = new Vector3Int(x, y, z);
                    int myBlockID = GetBlockLocal(x, y, z);

                    foreach (var face in faces)
                    {
                        Vector3Int neighborPos = pos + face.direction;

                        if (!IsSolid(neighborPos.x, neighborPos.y, neighborPos.z))
                        {
                            AddFace(pos, face, (BlockType)myBlockID);
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    int GetBlockLocal(int x, int y, int z)
    {
        return VoxelData[x + (y * size) + (z * size * size)];
    }

    bool IsSolid(int x, int y, int z)
    {
        if (x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size)
        {
            return GetBlockLocal(x, y, z) != 0;
        }

        Vector3Int neighborDir = new Vector3Int(0, 0, 0);
        if (x < 0) neighborDir.x = -1;
        else if (x >= size) neighborDir.x = 1;

        if (y < 0) neighborDir.y = -1;
        else if (y >= size) neighborDir.y = 1;

        if (z < 0) neighborDir.z = -1;
        else if (z >= size) neighborDir.z = 1;

        Chunk neighborChunk = worldRef.GetChunk(GridCoord + neighborDir);

        if (neighborChunk != null)
        {
            int nx = (x + size) % size;
            int ny = (y + size) % size;
            int nz = (z + size) % size;

            int neighborBlock = neighborChunk.VoxelData[nx + (ny * size) + (nz * size * size)];
            return neighborBlock != 0;
        }

        return false;
    }

    void AddFace(Vector3Int pos, FaceData face, BlockType type)
    {
        int v = vertices.Count;
        foreach (int i in face.vertIndices) vertices.Add(vertexPos[i] + pos);
        triangles.AddRange(new int[] { v, v + 1, v + 2, v + 2, v + 3, v });

        float idx = (type == BlockType.Grass) ? (face.direction.y > 0 ? 0 : (face.direction.y < 0 ? 2 : 1)) :
                    (type == BlockType.Dirt ? 2 : 3);
        uvs.Add(new Vector3(0, 1, idx)); uvs.Add(new Vector3(0, 0, idx));
        uvs.Add(new Vector3(1, 0, idx)); uvs.Add(new Vector3(1, 1, idx));
    }
}