using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public BlockType[,,] blocks;
    public Vector3Int dimensions;
    [SerializeField] MeshFilter meshFilter;

    readonly Vector3[] vertexPos = new Vector3[8]
     {
            //TOP
            new Vector3(-0.5f,  0.5f, -0.5f), // 0: Back-Left-Up
            new Vector3(-0.5f,  0.5f,  0.5f), // 1: Front-Left-Up
            new Vector3( 0.5f,  0.5f,  0.5f), // 2: Front-Right-Up
            new Vector3( 0.5f,  0.5f, -0.5f), // 3: Back-Right-Up

            //BOTTOM 
            new Vector3(-0.5f, -0.5f, -0.5f), // 4: Back-Left-Down
            new Vector3(-0.5f, -0.5f,  0.5f), // 5: Front-Left-Down
            new Vector3( 0.5f, -0.5f,  0.5f), // 6: Front-Right-Down
            new Vector3( 0.5f, -0.5f, -0.5f), // 7: Back-Right-Down
     };

    readonly Face[] faces = new Face[]
    {
            new Face(new int[]{0,1,2,3}, Vector3.up),    // top
            new Face(new int[]{5,4,7,6}, Vector3.down),  // bottom
            new Face(new int[]{1,5,6,2}, Vector3.forward), // front
            new Face(new int[]{3,7,4,0}, Vector3.back),    // back
            new Face(new int[]{2,6,7,3}, Vector3.right),   // right
            new Face(new int[]{0,4,5,1}, Vector3.left)     // left
    };

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector3> uvs = new List<Vector3>();
    struct Face
    {
        public int[] vertexIndices;
        public Vector3 direction;

        public Face(int[] vertexIndices, Vector3 dir)
        {
            this.vertexIndices = vertexIndices;
            this.direction = dir;
        }
    }

    public void Initialize(Vector3Int chunkSize)
    {
        this.dimensions = chunkSize;
        blocks = new BlockType[dimensions.x, dimensions.y, dimensions.z];
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    blocks[x, y, z] = GetBlockData(new Vector3Int(x, y, z));
                }
            }
        }
        GenerateMesh();
    }

    private void GenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        if (meshFilter.sharedMesh != null)
        {
            meshFilter.sharedMesh.Clear();
        }


        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (GetBlock(pos) != BlockType.Air)
                    {
                        foreach (var face in faces)
                        {
                            if (GetBlock(pos + Vector3Int.RoundToInt(face.direction)) == BlockType.Air)
                                AddFace(pos, face, GetBlock(pos));
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.SetUVs(0, uvs);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }
    void AddTextureUVs(BlockType blockType, Vector3 faceDir)
    {
        float textureIndex = 0;

        switch (blockType)
        {
            case BlockType.Grass:
                if (faceDir == Vector3.up) textureIndex = 0; // Trawa góra (Index 0)
                else if (faceDir == Vector3.down) textureIndex = 2; // Ziemia (Index 2)
                else textureIndex = 1; // Trawa bok (Index 1)
                break;
            case BlockType.Dirt:
                textureIndex = 2; // Ziemia (Index 2)
                break;
            case BlockType.Stone:
                textureIndex = 3; // Kamieñ (Index 3)
                break;
        }

        uvs.Add(new Vector3(0, 1, textureIndex)); // 0: Lewy-Górny róg tekstury
        uvs.Add(new Vector3(0, 0, textureIndex)); // 1: Lewy-Dolny róg tekstury
        uvs.Add(new Vector3(1, 0, textureIndex)); // 2: Prawy-Dolny róg tekstury
        uvs.Add(new Vector3(1, 1, textureIndex)); //
    }

    private BlockType GetBlock(Vector3Int pos)
    {
        if (pos.x >= 0 && pos.x < dimensions.x &&
            pos.y >= 0 && pos.y < dimensions.y &&
            pos.z >= 0 && pos.z < dimensions.z)
        {
            return blocks[pos.x, pos.y, pos.z];
        }

        return GetBlockData(pos);
    }

    private BlockType GetBlockData(Vector3Int pos)
    {
        float globalX = pos.x + transform.position.x;
        float globalY = pos.y + transform.position.y;
        float globalZ = pos.z + transform.position.z;

        float caveThreshold = 0.5f;
        float islandThreshold = 0.65f;
        int islandMinHeight = 20;

        //if (globalY <= -64)
        //{
        //    return BlockType.Bedrock;
        //}

        float terrainHeight = Mathf.PerlinNoise(globalX * 0.05f, globalZ * 0.05f) * 15f;
        int groundHeight = Mathf.FloorToInt(terrainHeight);

        BlockType blockType = BlockType.Air;

        if (globalY <= groundHeight)
        {
            if (globalY == groundHeight) blockType = BlockType.Grass;
            else if (globalY > groundHeight - 4) blockType = BlockType.Dirt;
            else blockType = BlockType.Stone;
        }

        if (globalY > islandMinHeight)
        {
            float islandNoise = Perlin3D(globalX * 0.03f, globalY * 0.05f, globalZ * 0.03f);

            if (islandNoise > islandThreshold)
            {

                if (islandNoise < islandThreshold + 0.05f)
                    blockType = BlockType.Dirt; // Lub Grass, jeœli chcesz zielone ca³e wyspy
                else
                    blockType = BlockType.Stone;

                if (blockType == BlockType.Dirt) blockType = BlockType.Grass;
            }
        }

        if (blockType == BlockType.Air) return BlockType.Air;

        if (blockType != BlockType.Bedrock)
        {
            float caveNoise = Perlin3D(globalX * 0.05f, (globalY + 100) * 0.05f, globalZ * 0.05f);

            if (caveNoise > caveThreshold)
            {
                return BlockType.Air;
            }
        }

        return blockType;
    }

    public static float Perlin3D(float x, float y, float z)
    {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);

        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);

        float abc = ab + bc + ac + ba + cb + ca;
        return abc / 6f;
    }


    void AddFace(Vector3Int pos, Face face, BlockType blockType)
    {
        int vertIndex = vertices.Count;

        foreach (var i in face.vertexIndices)
        {
            vertices.Add(vertexPos[i] + pos);
        }

        triangles.AddRange(new int[] { vertIndex, vertIndex + 1, vertIndex + 2, vertIndex + 2, vertIndex + 3, vertIndex });
        AddTextureUVs(blockType, face.direction);
    }

    [Header("Debug Settings")]
    [SerializeField] bool showGizmos = false;

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Vector3 chunkCenter = transform.position + new Vector3(dimensions.x / 2f, dimensions.y / 2f, dimensions.z / 2f);
        Gizmos.DrawWireCube(chunkCenter, dimensions);

        if (blocks == null) return;

        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.up, Vector3Int.down,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.forward, Vector3Int.back
        };

        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    BlockType currentType = blocks[x, y, z];

                    if (currentType == BlockType.Air) continue;

                    bool isFullySurrounded = true;

                    foreach (var dir in directions)
                    {
                        Vector3Int neighborPos = new Vector3Int(x, y, z) + dir;

                        if (neighborPos.x >= 0 && neighborPos.x < dimensions.x &&
                            neighborPos.y >= 0 && neighborPos.y < dimensions.y &&
                            neighborPos.z >= 0 && neighborPos.z < dimensions.z)
                        {
                            if (blocks[neighborPos.x, neighborPos.y, neighborPos.z] != currentType)
                            {
                                isFullySurrounded = false;
                                break;
                            }
                        }
                        else
                        {
                            isFullySurrounded = false;
                            break;
                        }
                    }

                    if (isFullySurrounded) continue;

                    Gizmos.color = currentType switch
                    {
                        BlockType.Grass => Color.green,
                        BlockType.Dirt => new Color(0.59f, 0.29f, 0.0f),
                        BlockType.Stone => Color.gray,
                        BlockType.Bedrock => Color.black,
                        _ => Color.white
                    };

                    Vector3 blockCenterGlobal = transform.position + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    Gizmos.DrawWireCube(blockCenterGlobal, Vector3.one);
                }
            }
        }
    }
}
