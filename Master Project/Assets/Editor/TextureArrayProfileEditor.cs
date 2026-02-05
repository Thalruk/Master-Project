using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextureArrayProfile))]
public class TextureArrayProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TextureArrayProfile profile = (TextureArrayProfile)target;

        if (GUILayout.Button("Update / Create Texture Array"))
        {
            UpdateTextureArray(profile);
        }
    }

    void UpdateTextureArray(TextureArrayProfile profile)
    {
        if (profile.textures == null || profile.textures.Length == 0) return;

        Texture2D firstTex = profile.textures[0];
        int width = firstTex.width;
        int height = firstTex.height;
        int depth = profile.textures.Length;

        bool needsRecreation = profile.targetArray == null ||
                               profile.targetArray.width != width ||
                               profile.targetArray.height != height ||
                               profile.targetArray.depth != depth;

        if (needsRecreation)
        {
            profile.targetArray = new Texture2DArray(width, height, depth, TextureFormat.RGBA32, true);
        }

        profile.targetArray.filterMode = profile.filterMode;
        profile.targetArray.wrapMode = profile.wrapMode;
        profile.targetArray.anisoLevel = profile.anisoLevel;

        for (int i = 0; i < profile.textures.Length; i++)
        {
            profile.targetArray.SetPixels(profile.textures[i].GetPixels(), i, 0);
        }

        profile.targetArray.Apply(true, false);

        if (needsRecreation)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Texture Array", "WorldTextures", "asset", "Save your texture array");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(profile.targetArray, path);
            }
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("Texture2DArray updated successfully!");
    }
}