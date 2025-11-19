using UnityEditor;
using UnityEngine;

public class TextureArrayCreator : ScriptableWizard
{
    [MenuItem("Assets/Create/Texture Array")]
    static void CreateWizard()
    {
        DisplayWizard<TextureArrayCreator>("Create Texture Array", "Create");
    }


    public Texture2D[] textures; // Tu wrzucisz swoje obrazki

    void OnWizardCreate()
    {
        if (textures.Length == 0) return;

        Texture2D t = textures[0];
        Texture2DArray textureArray = new Texture2DArray(t.width, t.height, textures.Length, t.format, true);

        textureArray.filterMode = FilterMode.Point;
        textureArray.wrapMode = TextureWrapMode.Repeat;

        for (int i = 0; i < textures.Length; i++)
        {
            Graphics.CopyTexture(textures[i], 0, 0, textureArray, i, 0);
        }

        string path = "Assets/WorldTextures.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Zapisano plik tekstur: " + path);
    }
}