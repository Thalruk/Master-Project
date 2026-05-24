using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

public class AOBenchmark : MonoBehaviour
{
    public enum BenchmarkState { None, CustomVertexAO, UnitySSAO }

    [Header("Referencje")]
    [SerializeField] private Material voxelMaterial;
    [SerializeField] private GameObject unitySSAOVolume;

    [Header("Ustawienia Testu")]
    [SerializeField] private int framesToRecordPerState = 500;
    [SerializeField] private int framesToWarmUp = 50;

    private List<string> csvLines = new List<string>();
    private string filePath;

    private void Start()
    {
        filePath = Path.Combine(Application.dataPath, "../AO_Benchmark_Results.csv");
        csvLines.Add("State,FrameTime_ms,FPS,AllocatedMemory_MB");
        StartCoroutine(RunBenchmarkRoutine());
    }

    private IEnumerator RunBenchmarkRoutine()
    {
        Debug.Log("<color=cyan><b>[BENCHMARK]</b> Uruchamianie testów porównawczych Ambient Occlusion...</color>");

        yield return StartCoroutine(MeasureState(BenchmarkState.None, 0f, false));

        yield return StartCoroutine(MeasureState(BenchmarkState.CustomVertexAO, 1f, false));

        yield return StartCoroutine(MeasureState(BenchmarkState.UnitySSAO, 0f, true));

        File.WriteAllLines(filePath, csvLines);
        Debug.Log($"<color=green><b>[BENCHMARK]</b> Testy zakonczone sukcesem! Dane zapisano w: {filePath}</color>");
    }

    private IEnumerator MeasureState(BenchmarkState state, float aoIntensity, bool ssaoActive)
    {
        // Konfiguracja stanu
        if (voxelMaterial != null) voxelMaterial.SetFloat("_AOIntensity", aoIntensity);
        if (unitySSAOVolume != null) unitySSAOVolume.SetActive(ssaoActive);

        Debug.Log($"[BENCHMARK] Rozgrzewanie fazy: {state}...");
        for (int i = 0; i < framesToWarmUp; i++) yield return null;

        Debug.Log($"<color=yellow>[BENCHMARK] Rozpoczynanie pomiaru dla: {state}</color>");

        for (int i = 0; i < framesToRecordPerState; i++)
        {
            float frameTime = Time.unscaledDeltaTime * 1000f; // Konwersja na ms
            float fps = 1f / Time.unscaledDeltaTime;

            // Pobranie zuzycia pamieci RAM zalokowanej przez Unity
            long totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
            float memoryMB = totalAllocatedMemory / (1024f * 1024f);

            // Dodanie wpisu: Stan, Czas klatki, FPS, Pamiec
            csvLines.Add($"{state},{frameTime.ToString("F2")},{fps.ToString("F1")},{memoryMB.ToString("F2")}");

            yield return null;
        }
    }
}