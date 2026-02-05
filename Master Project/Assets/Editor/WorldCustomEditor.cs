using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(World))]
public class WorldCustomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        World world = (World)target;

        GUILayout.Space(10);
        if (GUILayout.Button("REGENERATE WORLD"))
        {
            if (Application.isPlaying)
            {
                world.RegenerateWorld();
            }
            else
            {
                Debug.LogWarning("Musisz byæ w Play Mode, ¿eby wygenerowaæ œwiat (GPU Readback tego wymaga)");
            }
        }
    }
}