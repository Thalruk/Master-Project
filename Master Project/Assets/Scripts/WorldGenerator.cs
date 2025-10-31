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

    //TODO: change to vector3int later
    [SerializeField] Vector3 Dimensions;
    [SerializeField] MeshFilter meshFilter;

    [SerializeField] bool showGizmos = false;
    void Start()
    {
        GenerateMesh();
    }

    private void GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<int> uvs = new List<int>();

        vertices.Clear();
        triangles.Clear();

        Vector3[] vertexPos = new Vector3[8]
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

        Face[] faces = new Face[]
        {
            new Face(new int[]{0,1,2,3}, Vector3.up),    // top
            new Face(new int[]{5,4,7,6}, Vector3.down),  // bottom
            new Face(new int[]{1,5,6,2}, Vector3.forward), // front
            new Face(new int[]{3,7,4,0}, Vector3.back),    // back
            new Face(new int[]{2,6,7,3}, Vector3.right),   // right
            new Face(new int[]{0,4,5,1}, Vector3.left)     // left
        };

        for (int x = 0; x < Dimensions.x; x++)
        {
            for (int y = 0; y < Dimensions.y; y++)
            {
                for (int z = 0; z < Dimensions.z; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (GetBlock(pos) != Blocks.Air)
                    {
                        int vertIndex;

                        //top
                        if (GetBlock(pos + Vector3Int.up) == Blocks.Air)
                        {
                            vertIndex = vertices.Count;
                            vertices.Add(vertexPos[0] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[1] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[2] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[3] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));

                            triangles.AddRange(new List<int> {
                                vertIndex + 0, vertIndex + 1, vertIndex + 2,
                                vertIndex + 2, vertIndex + 3, vertIndex + 0
                            });
                        }

                        //bottom
                        if (GetBlock(pos + Vector3Int.down) == Blocks.Air)
                        {
                            vertIndex = vertices.Count;

                            vertices.Add(vertexPos[5] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[4] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[7] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[6] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));

                            triangles.AddRange(new List<int> {
                                vertIndex + 0, vertIndex + 1, vertIndex + 2,
                                vertIndex + 2, vertIndex + 3, vertIndex +0
                            });
                        }

                        //front
                        if (GetBlock(pos + Vector3Int.forward) == Blocks.Air)
                        {
                            vertIndex = vertices.Count;
                            vertices.Add(vertexPos[1] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[5] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[6] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[2] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            triangles.AddRange(new List<int> {
                                vertIndex + 0, vertIndex + 1, vertIndex + 2,
                                vertIndex + 2, vertIndex + 3, vertIndex +0
                            });
                        }

                        //back
                        if (GetBlock(pos + Vector3Int.back) == Blocks.Air)
                        {
                            vertIndex = vertices.Count;
                            vertices.Add(vertexPos[3] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[7] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[4] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[0] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            triangles.AddRange(new List<int> {
                                vertIndex + 0, vertIndex + 1, vertIndex + 2,
                                vertIndex + 2, vertIndex + 3, vertIndex +0
                            });
                        }

                        //right
                        if (GetBlock(pos + Vector3Int.right) == Blocks.Air)
                        {
                            vertIndex = vertices.Count;
                            vertices.Add(vertexPos[2] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[6] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[7] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[3] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            triangles.AddRange(new List<int> {
                                vertIndex + 0, vertIndex + 1, vertIndex + 2,
                                vertIndex + 2, vertIndex + 3, vertIndex +0
                            });
                        }

                        //left
                        if (GetBlock(pos + Vector3Int.left) == Blocks.Air)
                        {
                            vertIndex = vertices.Count;
                            vertices.Add(vertexPos[0] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[4] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[5] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            vertices.Add(vertexPos[1] + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                            triangles.AddRange(new List<int> {
                                vertIndex + 0, vertIndex + 1, vertIndex + 2,
                                vertIndex + 2, vertIndex + 3, vertIndex +0
                            });
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


    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            for (int x = 0; x < Dimensions.x; x++)
            {
                for (int y = 0; y < Dimensions.y; y++)
                {
                    for (int z = 0; z < Dimensions.z; z++)
                    {
                        Gizmos.color = GetBlock(new Vector3Int(x, y, z)) switch
                        {
                            Blocks.Air => new Color(0, 1, 1, 0.25f),
                            Blocks.Grass => Color.green,
                            Blocks.Dirt => new Color(0.545f, 0.271f, 0.075f, 1),
                            Blocks.Stone => Color.gray,
                            Blocks.Bedrock => Color.black,
                            _ => Color.magenta,
                        };
                        Gizmos.DrawWireSphere(new Vector3(x, y, z), 0.5f);
                    }
                }
            }
        }
    }
    Blocks GetBlock(Vector3Int coordinates)
    {
        float height = Mathf.Floor(Mathf.PerlinNoise(coordinates.x * 0.1f, coordinates.z * 0.1f) * 5f);

        if (coordinates.y > height)
            return Blocks.Air;

        float typeNoise = Mathf.PerlinNoise(coordinates.x * 0.3f, coordinates.z * 0.3f);

        if (typeNoise > 0.6f)
            return Blocks.Stone;
        else
            return Blocks.Dirt;
    }
}
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