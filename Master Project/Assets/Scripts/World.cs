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
    [SerializeField] GameObject chunkPrefab;

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
        Stopwatch dataTimer = new Stopwatch();
        Stopwatch meshTimer = new Stopwatch();

        dataTimer.Start();
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

                    chunkMap.Add(coord, newChunk);

                    newChunk.InitializeData(coord, chunkSize, this);
                }
            }
            //yield return null;
        }
        dataTimer.Stop();
        meshTimer.Start();
        foreach (var chunk in chunkMap.Values)
        {
            chunk.UpdateMesh();
            //yield return null;
        }
        meshTimer.Stop();

        long totalVoxelDataMemory = 0;
        long totalMeshMemory = 0;

        foreach (var chunk in chunkMap.Values)
        {
            if (chunk.VoxelData != null)
            {
                totalVoxelDataMemory += chunk.VoxelData.Length * sizeof(byte);
            }

            if (chunk.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
            {
                totalMeshMemory += Profiler.GetRuntimeMemorySizeLong(mf.sharedMesh);
            }
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
}