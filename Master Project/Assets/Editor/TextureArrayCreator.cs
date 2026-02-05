using UnityEditor;
using UnityEngine;

public class TextureArrayCreator : ScriptableWizard
{
    [MenuItem("Tools/Create Texture Array (Wizard)")]
    static void CreateWizard()
    {
        DisplayWizard<TextureArrayCreator>("Create Texture Array", "Create");
    }

    public Texture2D[] textures;

    void OnWizardCreate()
    {
        if (textures.Length == 0) return;

        Texture2D t = textures[0];

        Texture2DArray textureArray = new Texture2DArray(t.width, t.height, textures.Length, TextureFormat.RGBA32, true);

        textureArray.filterMode = FilterMode.Point;
        textureArray.wrapMode = TextureWrapMode.Repeat;
        textureArray.anisoLevel = 16;

        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i, 0);
        }

        textureArray.Apply(true, false);

        string path = "Assets/WorldTextures.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Zapisano Texture Array (z MipMapami!) w: " + path);
    }
}