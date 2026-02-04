using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    // Publiczny dostêp do danych (read-only dla bezpieczeñstwa)
    public int[] VoxelData { get; private set; }

    public Vector3Int GridCoord { get; private set; } // Moja pozycja w siatce (np. 0,1,0)

    private int size;
    private World worldRef;
    private ComputeShader voxelShader; // Przypisz w prefabie!

    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;

    // Potrzebujemy referencji do shadera w kodzie. 
    // Najlepiej wczytaæ go z Resources albo przypisaæ w Inspectorze prefaba.
    // Tutaj zak³adam, ¿e przypiszesz go w Inspectorze w Unity.
    [SerializeField] ComputeShader assignedShader;

    // --- STRUKTURY DLA MESHA ---
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

    // --- KROK 1: Generowanie Danych ---
    public void InitializeData(Vector3Int coord, int chunkSize, World world)
    {
        this.GridCoord = coord;
        this.size = chunkSize;
        this.worldRef = world;
        this.voxelShader = assignedShader;

        int totalVoxels = size * size * size;
        VoxelData = new int[totalVoxels];

        // Obliczenia na GPU
        ComputeBuffer buffer = new ComputeBuffer(totalVoxels, 4);
        try
        {
            buffer.SetData(VoxelData);
            int kernel = voxelShader.FindKernel("CSMain");
            voxelShader.SetBuffer(kernel, "ResultBuffer", buffer);
            voxelShader.SetInt("ChunkSize", size);

            // Wa¿ne: Przekazujemy globaln¹ pozycjê, ¿eby noise siê zgadza³
            voxelShader.SetVector("ChunkOffset", transform.position);

            voxelShader.Dispatch(kernel, size / 8, size / 8, size / 8);
            buffer.GetData(VoxelData);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    // --- KROK 2: Generowanie Mesha ---
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

                    // Jeœli ja jestem pusty, nie generujê nic
                    if (!IsSolid(x, y, z)) continue;

                    Vector3Int pos = new Vector3Int(x, y, z);
                    int myBlockID = GetBlockLocal(x, y, z);

                    foreach (var face in faces)
                    {
                        // Sprawdzamy s¹siada w kierunku œcianki
                        Vector3Int neighborPos = pos + face.direction;

                        // Jeœli s¹siad NIE jest lity (jest powietrzem), to rysujemy œciankê
                        if (!IsSolid(neighborPos.x, neighborPos.y, neighborPos.z))
                        {
                            AddFace(pos, face, (BlockType)myBlockID);
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Obs³uga du¿ych meshy
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    // --- KLUCZOWA LOGIKA ---

    // Pobiera blok z lokalnej tablicy (bez sprawdzania granic, bo wiemy ¿e x,y,z s¹ ok)
    int GetBlockLocal(int x, int y, int z)
    {
        return VoxelData[x + (y * size) + (z * size * size)];
    }

    // Sprawdza czy blok jest lity (obs³uguje s¹siadów!)
    bool IsSolid(int x, int y, int z)
    {
        // 1. Jeœli jesteœmy WEWN¥TRZ tego chunka
        if (x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size)
        {
            return GetBlockLocal(x, y, z) != 0;
        }

        // 2. Jeœli jesteœmy POZA chunkiem -> musimy zapytaæ World o s¹siada

        // Obliczamy w któr¹ stronê wyszliœmy (np. x=-1 oznacza direction (-1,0,0))
        Vector3Int neighborDir = new Vector3Int(0, 0, 0);
        if (x < 0) neighborDir.x = -1;
        else if (x >= size) neighborDir.x = 1;

        if (y < 0) neighborDir.y = -1;
        else if (y >= size) neighborDir.y = 1;

        if (z < 0) neighborDir.z = -1;
        else if (z >= size) neighborDir.z = 1;

        // Pytamy World o chunka pod wspó³rzêdnymi [Ja + Kierunek]
        Chunk neighborChunk = worldRef.GetChunk(GridCoord + neighborDir);

        if (neighborChunk != null)
        {
            // Mamy s¹siada! Ale musimy przeliczyæ wspó³rzêdne na JEGO lokalne.
            // Np. jeœli ja szukam x=-1, to u s¹siada po lewej to jest x=15.
            // U¿ywamy modulo (z poprawk¹ na ujemne liczby w C#)
            int nx = (x + size) % size;
            int ny = (y + size) % size;
            int nz = (z + size) % size;

            // Pobieramy dane z tablicy s¹siada
            // UWAGA: Musisz zrobiæ VoxelData public w Chunk.cs
            int neighborBlock = neighborChunk.VoxelData[nx + (ny * size) + (nz * size * size)];
            return neighborBlock != 0;
        }

        // 3. Jeœli s¹siad NIE ISTNIEJE (Koniec œwiata)
        // Zwracamy false (powietrze), ¿eby zamkn¹æ œwiat œciankami zewnêtrznymi
        return false;
    }

    void AddFace(Vector3Int pos, FaceData face, BlockType type)
    {
        int v = vertices.Count;
        foreach (int i in face.vertIndices) vertices.Add(vertexPos[i] + pos);
        triangles.AddRange(new int[] { v, v + 1, v + 2, v + 2, v + 3, v });

        // Twoja funkcja UV (skrócona dla czytelnoœci)
        float idx = (type == BlockType.Grass) ? (face.direction.y > 0 ? 0 : (face.direction.y < 0 ? 2 : 1)) :
                    (type == BlockType.Dirt ? 2 : 3);
        uvs.Add(new Vector3(0, 1, idx)); uvs.Add(new Vector3(0, 0, idx));
        uvs.Add(new Vector3(1, 0, idx)); uvs.Add(new Vector3(1, 1, idx));
    }
}