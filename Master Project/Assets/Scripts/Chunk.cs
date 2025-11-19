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
                                AddFace(pos, face);
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
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

        // --- KONFIGURACJA (Mo¿esz to wynieœæ do zmiennych publicznych) ---
        float caveThreshold = 0.5f;      // Jak du¿e s¹ jaskinie (im mniej, tym wiêcej dziur)
        float islandThreshold = 0.65f;   // Jak gêste s¹ wyspy (im wiêcej, tym mniejsze wyspy)
        int islandMinHeight = 20;        // Od jakiej wysokoœci zaczynaj¹ siê wyspy

        // 1. BEDROCK (Zawsze na dnie, niezniszczalny)
        if (globalY <= -64)
        {
            return BlockType.Bedrock;
        }

        // 2. TEREN PODSTAWOWY (Heightmap 2D)
        // To twoja stara logika - tworzy pod³ogê
        float terrainHeight = Mathf.PerlinNoise(globalX * 0.05f, globalZ * 0.05f) * 15f; // Zwiêkszy³em amplitudê do 15
        int groundHeight = Mathf.FloorToInt(terrainHeight);

        BlockType blockType = BlockType.Air;

        // Sprawdzamy, czy jesteœmy w "gruncie"
        if (globalY <= groundHeight)
        {
            // Standardowe warstwy
            if (globalY == groundHeight) blockType = BlockType.Grass;
            else if (globalY > groundHeight - 4) blockType = BlockType.Dirt;
            else blockType = BlockType.Stone;
        }

        // 3. LATAJ¥CE WYSPY (Szum 3D - Dodawanie)
        // Sprawdzamy tylko wysoko nad ziemi¹, ¿eby oszczêdziæ obliczenia
        if (globalY > islandMinHeight)
        {
            // U¿ywamy innej skali (0.03f) ¿eby wyspy by³y wiêksze i bardziej "chmurzaste"
            // Odejmujemy trochê od Y w szumie, ¿eby wyspy by³y rzadsze im wy¿ej
            float islandNoise = Perlin3D(globalX * 0.03f, globalY * 0.05f, globalZ * 0.03f);

            if (islandNoise > islandThreshold)
            {
                // Jeœli trafiliœmy na wyspê, nadpisujemy powietrze
                // Prosta logika: Wierzch wyspy to trawa/ziemia, œrodek to kamieñ

                // Sprytny trik: jeœli szum jest blisko granicy (0.65 - 0.70), to znaczy ¿e jesteœmy na krawêdzi wyspy -> Ziemia/Trawa
                // Jeœli szum jest mocny (> 0.70), to znaczy ¿e jesteœmy g³êboko w wyspie -> Kamieñ

                if (islandNoise < islandThreshold + 0.05f)
                    blockType = BlockType.Dirt; // Lub Grass, jeœli chcesz zielone ca³e wyspy
                else
                    blockType = BlockType.Stone;

                // Opcjonalnie: Jeœli blok nad nami jest pusty (Air), zamieñ ten blok na Grass.
                // (Wymaga³oby to sprawdzenia s¹siada, tutaj upraszczamy)
                if (blockType == BlockType.Dirt) blockType = BlockType.Grass;
            }
        }

        // Jeœli po krokach 2 i 3 nadal mamy Air, to koñczymy (szkoda liczyæ jaskinie w powietrzu)
        if (blockType == BlockType.Air) return BlockType.Air;

        // 4. JASKINIE (Szum 3D - Odejmowanie)
        // Wycinamy dziury w Kamieniu, Ziemi i Wyspach (ale nie w Bedrocku!)
        if (blockType != BlockType.Bedrock)
        {
            // Skala 0.05f daje fajne, krête korytarze.
            // Dodajemy offset do Y, ¿eby jaskinie nie pokrywa³y siê idealnie z kszta³tem terenu
            float caveNoise = Perlin3D(globalX * 0.05f, (globalY + 100) * 0.05f, globalZ * 0.05f);

            if (caveNoise > caveThreshold)
            {
                return BlockType.Air; // Jaskinia "zjada" blok
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


    void AddFace(Vector3Int pos, Face face)
    {
        int vertIndex = vertices.Count;

        // Dodajemy wierzcho³ki
        foreach (var i in face.vertexIndices)
        {
            vertices.Add(vertexPos[i] + pos);
        }

        // Dodajemy trójk¹ty
        triangles.AddRange(new int[] { vertIndex, vertIndex + 1, vertIndex + 2, vertIndex + 2, vertIndex + 3, vertIndex });

    }

    [Header("Debug Settings")]
    [SerializeField] bool showGizmos = false;

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Rysuj ¿ó³t¹ ramkê ca³ego chunka
        Gizmos.color = Color.yellow;
        Vector3 chunkCenter = transform.position + new Vector3(dimensions.x / 2f, dimensions.y / 2f, dimensions.z / 2f);
        Gizmos.DrawWireCube(chunkCenter, dimensions);

        if (blocks == null) return;

        // Tablica kierunków do sprawdzania s¹siadów
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
