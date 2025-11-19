using System.Collections.Generic;
using System.Threading.Tasks;
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
    [SerializeField] Vector3Int startSize = new Vector3Int(3, 3, 3);
    [SerializeField] Vector3Int chunkDimensions = new Vector3Int(16, 16, 16);



    List<Chunk> chunks = new List<Chunk>();
    public GameObject chunkPrefab;

    private async void Awake()
    {
        await GenerateWorld();
    }

    private async Task GenerateWorld()
    {
        for (int x = -startSize.x / 2; x <= startSize.x / 2; x++)
        {
            for (int y = -startSize.y / 2; y <= startSize.y / 2; y++)
            {
                for (int z = -startSize.z / 2; z <= startSize.z / 2; z++)
                {
                    Chunk newChunk = Instantiate(chunkPrefab, new Vector3(chunkDimensions.x * x, chunkDimensions.y * y, chunkDimensions.z * z), Quaternion.identity, transform).GetComponent<Chunk>();
                    newChunk.Initialize(chunkDimensions);
                    chunks.Add(newChunk);
                }
                await Task.Delay(1);

            }
        }
    }
}
