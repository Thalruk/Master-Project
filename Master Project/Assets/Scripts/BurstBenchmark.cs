using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BurstBenchmark : MonoBehaviour
{
    [Header("Ustawienia testu")]
    public int chunkSize = 16;
    public int chunksToGenerate = 100;

    void Start()
    {
        RunBurstTest();
    }

    void RunBurstTest()
    {
        int paddedSize = chunkSize + 2;
        int paddedLength = paddedSize * paddedSize * paddedSize;

        NativeArray<byte> dummyData = new NativeArray<byte>(paddedLength, Allocator.Persistent);
        for (int i = 0; i < paddedLength; i++) dummyData[i] = 1; // Wype³niamy sztucznym "kamieniem"

        NativeList<Vector3> vertices = new NativeList<Vector3>(Allocator.Persistent);
        NativeList<int> triangles = new NativeList<int>(Allocator.Persistent);
        NativeList<Vector3> uvs = new NativeList<Vector3>(Allocator.Persistent);
        NativeList<Vector3> normals = new NativeList<Vector3>(Allocator.Persistent);
        NativeList<Color32> colors = new NativeList<Color32>(Allocator.Persistent);

        MeshJob warmup = new MeshJob { paddedData = dummyData, size = chunkSize, paddedSize = paddedSize, vertices = vertices, triangles = triangles, uvs = uvs, normals = normals, colors = colors };
        warmup.Schedule().Complete();
        ClearLists(vertices, triangles, uvs, normals, colors);

        // TEST W£AŒCIWY
        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < chunksToGenerate; i++)
        {
            MeshJob job = new MeshJob
            {
                paddedData = dummyData,
                size = chunkSize,
                paddedSize = paddedSize,
                vertices = vertices,
                triangles = triangles,
                uvs = uvs,
                normals = normals,
                colors = colors
            };
            job.Schedule().Complete();
            ClearLists(vertices, triangles, uvs, normals, colors);
        }

        sw.Stop();
        Debug.Log($"<color=orange><b>[BURST BENCHMARK]</b> Czas budowania {chunksToGenerate} chunków: <b>{sw.ElapsedMilliseconds} ms</b></color>");

        dummyData.Dispose();
        vertices.Dispose();
        triangles.Dispose();
        uvs.Dispose();
        normals.Dispose();
        colors.Dispose();
    }

    private void ClearLists(NativeList<Vector3> v, NativeList<int> t, NativeList<Vector3> u, NativeList<Vector3> n, NativeList<Color32> c)
    {
        v.Clear();
        t.Clear();
        u.Clear();
        n.Clear();
        c.Clear();
    }
}