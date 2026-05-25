using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

public class AOBenchmark : MonoBehaviour
{
    public enum BenchmarkState { ShaderAO, PostProcessingAO, None }

    [Header("References")]
    [SerializeField] private Material voxelMaterial;
    [SerializeField] private GameObject unitySSAOVolume;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private World worldRef;

    [Header("Test Settings")]
    [SerializeField] private int framesToRecordPerState = 600;
    [SerializeField] private int framesToWarmUp = 600;
    [SerializeField] private float movementSpeed = 10f;

    private List<string> csvLines = new List<string>();
    private string filePath;
    private Vector3 startingPosition;
    private bool isMoving = false;

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        filePath = Path.Combine(Application.dataPath, "../AO_Dynamic_Benchmark_Results.csv");

        csvLines.Add("sep=,");
        csvLines.Add("State,FrameTime_ms,FPS,AllocatedMemory_MB");

        if (playerTransform != null)
        {
            startingPosition = playerTransform.position;
        }
        else
        {
            Debug.LogError("[BENCHMARK] Player Transform is not assigned! Movement will not work.");
        }

        if (worldRef == null)
        {
            Debug.LogError("[BENCHMARK] World Reference is not assigned! World reset will not work.");
        }

        StartCoroutine(RunBenchmarkRoutine());
    }

    private void Update()
    {
        if (isMoving && playerTransform != null)
        {
            playerTransform.Translate(movementSpeed * Time.unscaledDeltaTime * Vector3.forward);
        }
    }

    private IEnumerator RunBenchmarkRoutine()
    {
        Debug.Log("<color=cyan><b>[BENCHMARK]</b> Start...</color>");
        yield return new WaitForSeconds(5f);

        yield return StartCoroutine(MeasureState(BenchmarkState.ShaderAO, true, 1f, false));
        yield return StartCoroutine(MeasureState(BenchmarkState.PostProcessingAO, false, 0f, true));
        yield return StartCoroutine(MeasureState(BenchmarkState.None, false, 0f, false));

        try
        {
            File.WriteAllLines(filePath, csvLines);
            Debug.Log($"<color=green><b>[BENCHMARK]</b> Success! Data saved to: {filePath}</color>");
        }
        catch (IOException e)
        {
            Debug.LogError($"[BENCHMARK] Error saving CSV: {e.Message}");
        }
    }

    private IEnumerator MeasureState(BenchmarkState state, bool useKeywordsAO, float aoIntensity, bool ssaoActive)
    {
        if (playerTransform != null)
        {
            isMoving = false;
            playerTransform.position = startingPosition;
        }

        if (worldRef != null)
        {
            Debug.Log($"[BENCHMARK] Wiping all chunks from memory for state: {state}...");
            worldRef.RegenerateWorld();
        }

        if (useKeywordsAO) Shader.EnableKeyword("COMPUTE_AO_ON");
        else Shader.DisableKeyword("COMPUTE_AO_ON");

        if (voxelMaterial != null) voxelMaterial.SetFloat("_AOIntensity", aoIntensity);
        if (unitySSAOVolume != null) unitySSAOVolume.SetActive(ssaoActive);

        Debug.Log($"[BENCHMARK] Warming up pipeline and generating initial world area for state: {state}...");
        for (int i = 0; i < framesToWarmUp; i++)
        {
            yield return null;
        }

        Debug.Log($"<color=yellow>[BENCHMARK] Recording data for state: {state}</color>");
        isMoving = true;

        var culture = System.Globalization.CultureInfo.InvariantCulture;

        for (int i = 0; i < framesToRecordPerState; i++)
        {
            float frameTime = Time.unscaledDeltaTime * 1000f;
            float fps = 1f / Time.unscaledDeltaTime;

            long totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
            float memoryMB = totalAllocatedMemory / (1024f * 1024f);

            string line = string.Format(culture, "{0},{1:F2},{2:F1},{3:F2}", state, frameTime, fps, memoryMB);
            csvLines.Add(line);

            yield return null;
        }

        isMoving = false;
    }
}