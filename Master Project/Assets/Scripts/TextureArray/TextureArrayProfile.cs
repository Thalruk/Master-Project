using UnityEngine;

[CreateAssetMenu(fileName = "NewTextureArrayProfile", menuName = "Voxel/Texture Array Profile")]
public class TextureArrayProfile : ScriptableObject
{
    public Texture2D[] textures;

    public Texture2DArray targetArray;

    public FilterMode filterMode = FilterMode.Point;
    public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
    public int anisoLevel = 16;
}