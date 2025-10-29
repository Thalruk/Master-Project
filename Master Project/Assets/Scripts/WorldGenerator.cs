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
    [SerializeField] int XLength = 16;
    [SerializeField] int YLength = 16;
    [SerializeField] int Zlength = 16;

    [SerializeField] MeshFilter meshFilter;
    void Start()
    {
        //GenerateWorld();
        GenerateMesh();
        UpdateMesh();
    }

    void Update()
    {

    }

    private void GenerateWorld()
    {
        world = new Blocks[XLength, YLength, Zlength];
        for (int x = 0; x < XLength; x++)
        {
            for (int y = 0; y < YLength; y++)
            {
                for (int z = 0; z < Zlength; z++)
                {
                    if (y < YLength / 2)
                    {
                        world[x, y, z] = Blocks.Dirt;
                    }
                    else
                    {
                        world[x, y, z] = Blocks.Air;
                    }
                }
            }
        }
    }

    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    private void GenerateMesh()
    {
        mesh = new Mesh();
        vertices = new Vector3[]
        {
            new Vector3(0,0,0),
            new Vector3(0,1,0),
            new Vector3(1,1,0),
            new Vector3(1,0,0)
        };
        triangles = new int[]
        {
            0,1,2,
            2,3,0
        };

    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;
    }

    private void OnDrawGizmos()
    {
        if (world == null) return;
        for (int x = 0; x < XLength; x++)
        {
            for (int y = 0; y < YLength; y++)
            {
                for (int z = 0; z < Zlength; z++)
                {
                    Gizmos.color = world[x, y, z] switch
                    {
                        Blocks.Air => Color.cyan,
                        Blocks.Grass => Color.green,
                        Blocks.Dirt => new Color(0.545f, 0.271f, 0.075f),
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
