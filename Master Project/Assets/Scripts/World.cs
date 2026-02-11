using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public enum BlockType : byte
{
    Air,
    Grass,
    Dirt,
    Stone,
    Bedrock
}

public class World : MonoBehaviour
{
    [SerializeField] Vector3Int startSize = new Vector3Int(4, 2, 4);
    [SerializeField] int chunkSize = 16;

    Dictionary<Vector3Int, Chunk> chunkMap = new Dictionary<Vector3Int, Chunk>();

    [Header("Settings")]
    [SerializeField] bool ShowFrustumCullingInSceneView = false;
    bool isFrustumCullingChecked = false;

    [SerializeField] int chunksToGeneratePerFrame = 25;
    [SerializeField] int chunksToMeshPerFrame = 50;

    Queue<Vector3Int> chunksToGenerate = new Queue<Vector3Int>();

    HashSet<Chunk> chunksToMesh = new HashSet<Chunk>();

    private HashSet<Vector3Int> chunksInQueue = new HashSet<Vector3Int>();

    [Header("Infinite World Settings")]
    [SerializeField] Transform playerTransform;
    [SerializeField] int unloadMargin = 2;
    private Vector3Int lastPlayerChunk;

    public NativeArray<byte> EmptyVoxelData { get; private set; }

    private void Start()
    {
        lastPlayerChunk = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        EmptyVoxelData = new NativeArray<byte>(chunkSize * chunkSize * chunkSize, Allocator.Persistent);
        StartCoroutine(WorldWorkerRoutine());
    }

    private void Update()
    {
        if (playerTransform == null) return;

        Vector3Int currentPlayerChunk = new Vector3Int(
            Mathf.FloorToInt(playerTransform.position.x / chunkSize),
            Mathf.FloorToInt(playerTransform.position.y / chunkSize),
            Mathf.FloorToInt(playerTransform.position.z / chunkSize)
        );

        if (currentPlayerChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentPlayerChunk;
            UpdateVisibleWorld(currentPlayerChunk);
        }

        HandleFrustumCulling();
    }

    private void HandleFrustumCulling()
    {
        if (ShowFrustumCullingInSceneView == false && !isFrustumCullingChecked)
        {
            foreach (var chunk in chunkMap.Values)
            {
                chunk.meshRenderer.enabled = true;
            }
            isFrustumCullingChecked = true;
        }
#if UNITY_EDITOR
        if (ShowFrustumCullingInSceneView)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

            foreach (var chunk in chunkMap.Values)
            {
                if (chunk == null || chunk.meshRenderer == null) continue;
                chunk.meshRenderer.enabled = GeometryUtility.TestPlanesAABB(planes, chunk.meshRenderer.bounds);
            }
            isFrustumCullingChecked = false;
        }
#endif
    }

    private IEnumerator WorldWorkerRoutine()
    {
        while (true)
        {
            if (chunksToGenerate.Count > 0)
            {
                int generatedCount = 0;
                while (chunksToGenerate.Count > 0 && generatedCount < chunksToGeneratePerFrame)
                {
                    Vector3Int coord = chunksToGenerate.Dequeue();

                    if (chunksInQueue.Contains(coord) && !chunkMap.ContainsKey(coord))
                    {
                        Vector3Int worldPos = new Vector3Int(coord.x * chunkSize, coord.y * chunkSize, coord.z * chunkSize);
                        Chunk newChunk = ChunkPool.Instance.GetChunk(worldPos);
                        chunkMap.Add(coord, newChunk);
                        newChunk.InitializeData(coord, chunkSize, this);
                        generatedCount++;
                    }
                }
            }

            if (chunksToMesh.Count > 0)
            {
                int meshedCount = 0;
                List<Chunk> processedChunks = new List<Chunk>();

                foreach (var chunk in chunksToMesh)
                {
                    if (meshedCount >= chunksToMeshPerFrame) break;

                    if (chunk != null && chunk.IsReady && chunkMap.ContainsKey(chunk.GridCoord))
                    {
                        chunk.UpdateMesh();
                        processedChunks.Add(chunk);
                        meshedCount++;
                    }
                }

                foreach (var c in processedChunks) chunksToMesh.Remove(c);
            }

            yield return null;
        }
    }

    private void UpdateVisibleWorld(Vector3Int center)
    {
        List<Vector3Int> toRemove = new List<Vector3Int>();

        float marginX = startSize.x + unloadMargin;
        float marginY = startSize.y + unloadMargin;
        float marginZ = startSize.z + unloadMargin;

        foreach (var coord in chunkMap.Keys)
        {
            Vector3 rel = new Vector3(coord.x - center.x, coord.y - center.y, coord.z - center.z);
            float dist = (rel.x * rel.x) / (marginX * marginX) +
                         (rel.y * rel.y) / (marginY * marginY) +
                         (rel.z * rel.z) / (marginZ * marginZ);

            if (dist > 1.0f)
            {
                toRemove.Add(coord);
            }
        }

        foreach (var coord in toRemove)
        {
            if (chunkMap.ContainsKey(coord))
            {
                Chunk c = chunkMap[coord];
                ChunkPool.Instance.ReturnChunk(c);
                chunkMap.Remove(coord);
            }
            chunksInQueue.Remove(coord);
        }

        for (int x = -startSize.x; x <= startSize.x; x++)
        {
            for (int y = -startSize.y; y <= startSize.y; y++)
            {
                for (int z = -startSize.z; z <= startSize.z; z++)
                {
                    float dist = (float)(x * x) / (startSize.x * startSize.x) +
                                 (float)(y * y) / (startSize.y * startSize.y) +
                                 (float)(z * z) / (startSize.z * startSize.z);

                    if (dist <= 1.0f)
                    {
                        Vector3Int coord = new Vector3Int(x + center.x, y + center.y, z + center.z);
                        if (!chunkMap.ContainsKey(coord) && !chunksInQueue.Contains(coord))
                        {
                            chunksInQueue.Add(coord);
                            chunksToGenerate.Enqueue(coord);
                        }
                    }
                }
            }
        }
    }

    public void EnsureNeighborhoodReady(Vector3Int coord)
    {
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && y == 0 && z == 0) continue;
                    Chunk c = GetChunk(coord + new Vector3Int(x, y, z));
                    if (c != null) c.EnsureJobCompleted();
                }
    }

    public Chunk GetChunk(Vector3Int coord)
    {
        if (chunkMap.TryGetValue(coord, out Chunk chunk))
        {
            return chunk;
        }
        return null;
    }

    public void OnDataReady(Chunk chunk)
    {
        RequestMeshUpdate(chunk);

        Vector3Int[] neighbors = {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right,
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        foreach (var offset in neighbors)
        {
            Vector3Int neighborCoord = chunk.GridCoord + offset;
            Chunk neighbor = GetChunk(neighborCoord);
            if (neighbor != null && neighbor.IsReady)
            {
                RequestMeshUpdate(neighbor);
            }
        }
    }

    public void RequestMeshUpdate(Chunk chunk)
    {
        if (chunk != null && chunk.IsReady)
        {
            chunksToMesh.Add(chunk);
        }
    }

    public void RegenerateWorld()
    {
        StopAllCoroutines();
        foreach (var chunk in chunkMap.Values)
        {
            ChunkPool.Instance.ReturnChunk(chunk);
        }
        chunkMap.Clear();
        chunksToGenerate.Clear();
        chunksInQueue.Clear();
        chunksToMesh.Clear();
        StartCoroutine(WorldWorkerRoutine());
    }

    private void OnDestroy()
    {
        if (EmptyVoxelData.IsCreated) EmptyVoxelData.Dispose();
    }
}