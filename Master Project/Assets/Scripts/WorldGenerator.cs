using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    enum Blocks
    {
        Air,
        Grass,
        Dirt,
        Stone,
        Bedrock
    }

    [SerializeField] Blocks[,,] world;
    //TODO: change to vector3int later
    [SerializeField] Vector3 Dimensions;
    [SerializeField] MeshFilter meshFilter;
    void Start()
    {
        GenerateMesh();
    }

    private void GenerateData()
    {
        world = new Blocks[(int)Dimensions.x, (int)Dimensions.y, (int)Dimensions.z];
        for (int x = 0; x < Dimensions.x; x++)
        {
            for (int y = 0; y < Dimensions.y; y++)
            {
                for (int z = 0; z < Dimensions.z; z++)
                {

                    world[x, y, z] = GetBlock(new Vector3Int(x, y, z));
                    //if (y < Dimensions.y / 2)
                    //{
                    //    world[x, y, z] = Blocks.Dirt;
                    //}
                    //else
                    //{
                    //    world[x, y, z] = Blocks.Air;
                    //}
                }
            }
        }
    }

    private void GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int x = 0; x < Dimensions.x; x++)
        {
            for (int y = 0; y < Dimensions.y; y++)
            {
                for (int z = 0; z < Dimensions.z; z++)
                {

                    Vector3[] vertexPos = new Vector3[8]
                    {
                        new Vector3(-1,1,-1), new Vector3(-1,1,1),
                        new Vector3(1,1,1), new Vector3(1,1,-1),
                        new Vector3(-1,-1,-1), new Vector3(-1,-1,1),
                        new Vector3(1,-1,-1), new Vector3(1,-1,1),
                    };

                    void AddQuad(int ai, int bi, int ci, int di, int i)
                    {
                        Vector3 a = vertexPos[ai];
                        Vector3 b = vertexPos[bi];
                        Vector3 c = vertexPos[ci];
                        Vector3 d = vertexPos[di];
                        vertices.AddRange(new List<Vector3> { a, b, c, d });
                        triangles.AddRange(new List<int> { i, i + 1, i + 2, i, i + 2, i + 3 });
                    }
                    if (GetBlock(new Vector3Int(x, y, z)) != Blocks.Air)
                    {
                        if (GetBlock(new Vector3Int(x, y + 1, z)) == Blocks.Air)
                        {
                            //top
                            AddQuad(0, 1, 2, 3, vertices.Count);
                        }
                        if (GetBlock(new Vector3Int(x, y - 1, z)) == Blocks.Air)
                        {
                            //bottom
                            AddQuad(7, 6, 5, 4, vertices.Count);
                        }

                        if (GetBlock(new Vector3Int(x, y, z + 1)) == Blocks.Air)
                        {
                            //front
                            AddQuad(3, 2, 6, 7, vertices.Count);
                        }
                        if (GetBlock(new Vector3Int(x, y, z - 1)) == Blocks.Air)
                        {
                            //back
                            AddQuad(1, 0, 4, 5, vertices.Count);
                        }

                        if (GetBlock(new Vector3Int(x + 1, y, z)) == Blocks.Air)
                        {
                            //right
                            AddQuad(2, 1, 5, 6, vertices.Count);
                        }
                        if (GetBlock(new Vector3Int(x - 1, y, z)) == Blocks.Air)
                        {
                            //left
                            AddQuad(0, 3, 7, 4, vertices.Count);
                        }
                    }
                }
            }
        }

        meshFilter.mesh = new Mesh()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();
    }



    //private void OnDrawGizmos()
    //{
    //    if (world == null) return;
    //    for (int x = 0; x < Dimensions.x; x++)
    //    {
    //        for (int y = 0; y < Dimensions.y; y++)
    //        {
    //            for (int z = 0; z < Dimensions.z; z++)
    //            {
    //                Gizmos.color = world[x, y, z] switch
    //                {
    //                    Blocks.Air => Color.cyan,
    //                    Blocks.Grass => Color.green,
    //                    Blocks.Dirt => new Color(0.545f, 0.271f, 0.075f),
    //                    Blocks.Stone => Color.gray,
    //                    Blocks.Bedrock => Color.black,
    //                    _ => Color.magenta,
    //                };
    //                Gizmos.DrawWireSphere(new Vector3(x, y, z), 0.5f);
    //            }
    //        }
    //    }

    //}
    Blocks GetBlock(Vector3Int coordinates)
    {
        if (Mathf.PerlinNoise(coordinates.x * 0.1f, coordinates.y * 0.1f) > 0.5f)
        {
            return Blocks.Dirt;
        }
        else
        {
            return Blocks.Air;
        }
    }
}
