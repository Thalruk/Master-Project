using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;
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
    [SerializeField] int chunksToGeneratePerFrame = 10;
    Queue<Vector3Int> chunksToGenerate = new Queue<Vector3Int>();
    Queue<Chunk> chunksToMesh = new Queue<Chunk>();
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

    private void Update()
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


    private IEnumerator GenerateWorldRoutine()
    {
        Stopwatch dataTimer = new Stopwatch();
        Stopwatch meshTimer = new Stopwatch();

        dataTimer.Start();
        for (int x = -startSize.x - 1; x <= startSize.x + 1; x++)
        {
            for (int y = -startSize.y - 1; y <= startSize.y + 1; y++)
            {
                for (int z = -startSize.z - 1; z <= startSize.z + 1; z++)
                {
                    float xTemp = (float)x / (startSize.x + 1);
                    float yTemp = (float)y / (startSize.y + 1);
                    float zTemp = (float)z / (startSize.z + 1);
                    if (xTemp * xTemp + yTemp * yTemp + zTemp * zTemp <= 1.0f)
                    {
                        Vector3Int coord = new Vector3Int(x, y, z);
                        chunksToGenerate.Enqueue(coord);
                    }
                }
            }
        }


        while (chunksToGenerate.Count > 0)
        {
            for (int i = 0; i < chunksToGeneratePerFrame && chunksToGenerate.Count > 0; i++)
            {
                Vector3Int coord = chunksToGenerate.Dequeue();
                Vector3Int worldPos = new Vector3Int(coord.x * chunkSize, coord.y * chunkSize, coord.z * chunkSize);
                Chunk newChunk = ChunkPool.Instance.GetChunk(worldPos);
                chunkMap.Add(coord, newChunk);
                newChunk.InitializeData(coord, chunkSize, this);
            }
            yield return null;
        }
        dataTimer.Stop();


        meshTimer.Start();
        int processedCount = 0;
        while (processedCount < chunkMap.Count)
        {
            int processedThisFrame = 0;
            while (chunksToMesh.Count > 0 && processedThisFrame < chunksToGeneratePerFrame)
            {
                Chunk chunk = chunksToMesh.Dequeue();

                float xT = (float)chunk.GridCoord.x / startSize.x;
                float yT = (float)chunk.GridCoord.y / startSize.y;
                float zT = (float)chunk.GridCoord.z / startSize.z;

                if (xT * xT + yT * yT + zT * zT <= 1.0f)
                {
                    chunk.UpdateMesh();
                }

                processedCount++;
                processedThisFrame++;
            }
            yield return null;
        }
        meshTimer.Stop();

        long totalVoxelDataMemory = 0;
        long totalMeshMemory = 0;
        foreach (var chunk in chunkMap.Values)
        {
            if (chunk.VoxelData != null) totalVoxelDataMemory += chunk.VoxelData.Length;
            if (chunk.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
                totalMeshMemory += Profiler.GetRuntimeMemorySizeLong(mf.sharedMesh);
        }
        double voxelMB = totalVoxelDataMemory / (1024.0 * 1024.0);
        double meshMB = totalMeshMemory / (1024.0 * 1024.0);

        UnityEngine.Debug.Log($"--- POPRAWIONY RAPORT PAMIÊCI ---");
        UnityEngine.Debug.Log($"VoxelData (C# Heap): {voxelMB:F2} MB");
        UnityEngine.Debug.Log($"Meshe (Unity Engine): {meshMB:F2} MB");
        UnityEngine.Debug.Log($"Suma: {voxelMB + meshMB:F2} MB");
        UnityEngine.Debug.Log($"Wygenerowano œwiat: {chunkMap.Count} chunków.");
        UnityEngine.Debug.Log($"--- RAPORT GENEROWANIA ---");
        UnityEngine.Debug.Log($"Dane (GPU + Transfer): {dataTimer.ElapsedMilliseconds} ms");
        UnityEngine.Debug.Log($"Meshe (CPU): {meshTimer.ElapsedMilliseconds} ms");
        UnityEngine.Debug.Log($"Ca³kowity czas: {dataTimer.ElapsedMilliseconds + meshTimer.ElapsedMilliseconds} ms");

        yield break;
    }

    public void OnDataReady(Chunk chunk)
    {
        chunksToMesh.Enqueue(chunk);
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
        chunksToMesh.Clear();
        StartCoroutine(GenerateWorldRoutine());
    }
}