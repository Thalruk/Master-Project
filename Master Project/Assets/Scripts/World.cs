using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum BlockType
{
    Air,
    Grass,
    Dirt,
    Stone,
    Bedrock
}
public class World : MonoBehaviour
{
    [SerializeField] Vector3Int startSize = new Vector3Int(4, 2, 4); // Iloœæ chunków
    [SerializeField] int chunkSize = 16; // Sta³y rozmiar szeœcianu dla uproszczenia
    [SerializeField] GameObject chunkPrefab;

    // S£OWNIK: Pozwala znaleŸæ chunka w czasie zerowym po jego wspó³rzêdnych (np. [0,1,0])
    Dictionary<Vector3Int, Chunk> chunkMap = new Dictionary<Vector3Int, Chunk>();

    private void Start()
    {
        StartCoroutine(GenerateWorldRoutine());
    }

    public Chunk GetChunk(Vector3Int coord)
    {
        if (chunkMap.TryGetValue(coord, out Chunk chunk))
        {
            return chunk;
        }
        return null;
    }

    private IEnumerator GenerateWorldRoutine()
    {
        // KROK 1: Tworzenie obiektów i generowanie DANYCH (Compute Shader)
        // W tej pêtli NIE generujemy jeszcze meshy!

        for (int x = 0; x < startSize.x; x++)
        {
            for (int y = 0; y < startSize.y; y++)
            {
                for (int z = 0; z < startSize.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    Vector3 worldPos = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);

                    GameObject newChunkObj = Instantiate(chunkPrefab, worldPos, Quaternion.identity, transform);
                    Chunk newChunk = newChunkObj.GetComponent<Chunk>();

                    // Rejestrujemy w s³owniku
                    chunkMap.Add(coord, newChunk);

                    // Inicjalizujemy TYLKO dane (Shader)
                    newChunk.InitializeData(coord, chunkSize, this);
                }
            }
            // Robimy przerwê co "piêtro" generowania, ¿eby nie œci¹æ gry
            yield return null;
        }

        // KROK 2: Generowanie MESHY
        // Teraz, gdy wszystkie chunki maj¹ ju¿ dane w pamiêci, mo¿emy bezpiecznie budowaæ œciany

        foreach (var chunk in chunkMap.Values)
        {
            chunk.UpdateMesh();
            // Opcjonalnie: yield return null co X chunków, jeœli fps spada
        }

        Debug.Log($"Wygenerowano œwiat: {chunkMap.Count} chunków.");
    }
}