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

    readonly Face[] faces = new Face[]
    {
            new Face(new int[]{0,1,2,3}, Vector3.up),    // top
            new Face(new int[]{5,4,7,6}, Vector3.down),  // bottom
            new Face(new int[]{1,5,6,2}, Vector3.forward), // front
            new Face(new int[]{3,7,4,0}, Vector3.back),    // back
            new Face(new int[]{2,6,7,3}, Vector3.right),   // right
            new Face(new int[]{0,4,5,1}, Vector3.left)     // left
    };

    //TODO: change to vector3int later
    [SerializeField] Vector3 Dimensions;
    [SerializeField] MeshFilter meshFilter;

    [SerializeField] bool showGizmos = false;


    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    void Start()
    {
        GenerateMesh();
    }

    private void GenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();

        for (int x = 0; x < Dimensions.x; x++)
        {
            for (int y = 0; y < Dimensions.y; y++)
            {
                for (int z = 0; z < Dimensions.z; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (GetBlock(pos) != Blocks.Air)
                    {
                        foreach (var face in faces)
                        {
                            if (GetBlock(pos + Vector3Int.RoundToInt(face.direction)) == Blocks.Air)
                                AddFace(pos, face);
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
        };

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void AddFace(Vector3Int pos, Face face)
    {
        int vertIndex = vertices.Count;
        foreach (var i in face.vertexIndices)
        {
            vertices.Add(vertexPos[i] + pos - new Vector3(0.5f, 0.5f, 0.5f));
        }

        triangles.AddRange(new int[] { vertIndex, vertIndex + 1, vertIndex + 2, vertIndex + 2, vertIndex + 3, vertIndex });
    }
    [SerializeField] Vector3 offset;

    Blocks GetBlock(Vector3Int coordinates)
    {
        float height = Mathf.Floor(Mathf.PerlinNoise(coordinates.x * 0.1f, coordinates.z * 0.1f) * 5f);

        if (coordinates.y > height)
        {
            if (Perlin3D((coordinates.x + offset.x) * 0.03f, (coordinates.y + offset.y) * 0.05f, (coordinates.z + offset.z) * 0.05f) >= 0.75f)
            {
                return Blocks.Dirt;
            }
            return Blocks.Air;
        }
        else
        {
            return Blocks.Dirt;
        }



        //float typeNoise = Mathf.PerlinNoise(coordinates.x * 0.3f, coordinates.z * 0.3f);

        //if (typeNoise > 0.6f)
        //    return Blocks.Stone;
        //else
        //    return Blocks.Dirt;
    }

    private void OnValidate()
    {
        if (meshFilter != null)
        {
            GenerateMesh();
        }
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
                        if (GetBlock(new Vector3Int(x, y, z)) == Blocks.Air)
                        {
                            continue;
                        }
                        Gizmos.color = GetBlock(new Vector3Int(x, y, z)) switch
                        {
                            Blocks.Grass => Color.green,
                            Blocks.Dirt => new Color(0.545f, 0.271f, 0.075f, 1),
                            Blocks.Stone => Color.gray,
                            Blocks.Bedrock => Color.black,
                            _ => Color.magenta,
                        };
                        Gizmos.DrawWireSphere(new Vector3(x, y, z) - new Vector3(0.5f, 0.5f, 0.5f), 0.5f);
                    }
                }
            }
        }
    }
}
