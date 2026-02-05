using System.Collections.Generic;
using UnityEngine;

public class ChunkPool : MonoBehaviour
{
    public Queue<Chunk> pool = new Queue<Chunk>();
    [SerializeField] GameObject chunkPrefab;

    public static ChunkPool Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    public Chunk GetChunk(Vector3Int chunkPos)
    {
        Chunk chunk;
        if (pool.Count > 0)
        {
            chunk = pool.Dequeue();
            chunk.transform.position = chunkPos;
            chunk.gameObject.SetActive(true);
            chunk.meshRenderer.enabled = true;
        }
        else
        {
            chunk = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform).GetComponent<Chunk>();
            chunk.gameObject.SetActive(true);
            chunk.meshRenderer.enabled = true;
        }
        return chunk;

    }

    public void ReturnChunk(Chunk chunk)
    {
        chunk.transform.position = transform.position;
        chunk.meshRenderer.enabled = false;
        if (chunk.meshFilter.sharedMesh != null)
        {
            chunk.meshFilter.sharedMesh.Clear();
        }
        chunk.gameObject.SetActive(false);
        pool.Enqueue(chunk);
    }
}
