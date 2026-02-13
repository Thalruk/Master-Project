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
        if (profile.textures == null || profile.textures.Length == 0)
        {
            Debug.LogError("Brak tekstur w profilu!");
            return;
        }

        Texture2D firstTex = profile.textures[0];
        int width = firstTex.width;
        int height = firstTex.height;
        int depth = profile.textures.Length;
        TextureFormat format = firstTex.format;

        foreach (var tex in profile.textures)
        {
            if (tex.width != width || tex.height != height)
            {
                Debug.LogError($"Tekstura {tex.name} ma inny rozmiar! Wszystkie musz¹ byæ {width}x{height}.");
                return;
            }
        }

        Texture2DArray texArray = new Texture2DArray(width, height, depth, format, true);

        texArray.filterMode = profile.filterMode;
        texArray.wrapMode = profile.wrapMode;
        texArray.anisoLevel = profile.anisoLevel;

        for (int i = 0; i < profile.textures.Length; i++)
        {
            for (int mip = 0; mip < firstTex.mipmapCount; mip++)
            {
                Graphics.CopyTexture(profile.textures[i], 0, mip, texArray, i, mip);
            }
        }

        texArray.Apply(false, true);

        // Zapisywanie assetu
        string path = AssetDatabase.GetAssetPath(profile.targetArray);
        if (string.IsNullOrEmpty(path))
        {
            path = EditorUtility.SaveFilePanelInProject("Save Texture Array", "NewVoxelArray", "asset", "Zapisz TextureArray");
        }

        if (!string.IsNullOrEmpty(path))
        {
            if (profile.targetArray == null)
            {
                AssetDatabase.CreateAsset(texArray, path);
            }
            else
            {
                EditorUtility.CopySerialized(texArray, profile.targetArray);
                AssetDatabase.SaveAssets();
            }

            profile.targetArray = AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("Texture2DArray zaktualizowana pomyœlnie u¿ywaj¹c Graphics.CopyTexture!");
    }
}