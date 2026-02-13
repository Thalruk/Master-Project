using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct MeshJob : IJob
{
    [ReadOnly] public NativeArray<byte> paddedData;

    public int paddedSize;
    public int size;

    public NativeList<Vector3> vertices;
    public NativeList<int> triangles;
    public NativeList<Vector3> uvs;
    public NativeList<Vector3> normals;
    public NativeList<Color32> colors;

    public void Execute()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    byte blockID = paddedData[(x + 1) + (y + 1) * paddedSize + (z + 1) * paddedSize * paddedSize];

                    if (blockID == 0) continue;

                    if (GetVoxel(x, y + 1, z) == 0) AddFace(x, y, z, 0, 1, 0, blockID);  // Góra
                    if (GetVoxel(x, y - 1, z) == 0) AddFace(x, y, z, 0, -1, 0, blockID); // Dó³

                    if (GetVoxel(x + 1, y, z) == 0) AddFace(x, y, z, 1, 0, 0, blockID);  // Prawo
                    if (GetVoxel(x - 1, y, z) == 0) AddFace(x, y, z, -1, 0, 0, blockID); // Lewo

                    if (GetVoxel(x, y, z + 1) == 0) AddFace(x, y, z, 0, 0, 1, blockID);  // Przód
                    if (GetVoxel(x, y, z - 1) == 0) AddFace(x, y, z, 0, 0, -1, blockID); // Ty³
                }
            }
        }
    }

    private byte GetVoxel(int x, int y, int z)
    {
        return paddedData[(x + 1) + (y + 1) * paddedSize + (z + 1) * paddedSize * paddedSize];
    }

    private void AddFace(int x, int y, int z, int dx, int dy, int dz, byte blockID)
    {
        int vCount = vertices.Length;
        Vector3 pos = new Vector3(x, y, z);

        int ao0 = 0, ao1 = 0, ao2 = 0, ao3 = 0;

        if (dy == 1) // Góra
        {
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f) + pos);

            ao0 = VertexAO(GetVoxel(x - 1, y + 1, z), GetVoxel(x, y + 1, z - 1), GetVoxel(x - 1, y + 1, z - 1));
            ao1 = VertexAO(GetVoxel(x - 1, y + 1, z), GetVoxel(x, y + 1, z + 1), GetVoxel(x - 1, y + 1, z + 1));
            ao2 = VertexAO(GetVoxel(x + 1, y + 1, z), GetVoxel(x, y + 1, z + 1), GetVoxel(x + 1, y + 1, z + 1));
            ao3 = VertexAO(GetVoxel(x + 1, y + 1, z), GetVoxel(x, y + 1, z - 1), GetVoxel(x + 1, y + 1, z - 1));
        }
        else if (dy == -1) // Dó³
        {
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f) + pos);

            ao0 = VertexAO(GetVoxel(x - 1, y - 1, z), GetVoxel(x, y - 1, z + 1), GetVoxel(x - 1, y - 1, z + 1));
            ao1 = VertexAO(GetVoxel(x - 1, y - 1, z), GetVoxel(x, y - 1, z - 1), GetVoxel(x - 1, y - 1, z - 1));
            ao2 = VertexAO(GetVoxel(x + 1, y - 1, z), GetVoxel(x, y - 1, z - 1), GetVoxel(x + 1, y - 1, z - 1));
            ao3 = VertexAO(GetVoxel(x + 1, y - 1, z), GetVoxel(x, y - 1, z + 1), GetVoxel(x + 1, y - 1, z + 1));
        }
        else if (dz == 1) // Przód
        {
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f) + pos);

            ao0 = VertexAO(GetVoxel(x - 1, y, z + 1), GetVoxel(x, y + 1, z + 1), GetVoxel(x - 1, y + 1, z + 1));
            ao1 = VertexAO(GetVoxel(x - 1, y, z + 1), GetVoxel(x, y - 1, z + 1), GetVoxel(x - 1, y - 1, z + 1));
            ao2 = VertexAO(GetVoxel(x + 1, y, z + 1), GetVoxel(x, y - 1, z + 1), GetVoxel(x + 1, y - 1, z + 1));
            ao3 = VertexAO(GetVoxel(x + 1, y, z + 1), GetVoxel(x, y + 1, z + 1), GetVoxel(x + 1, y + 1, z + 1));
        }
        else if (dz == -1) // Ty³
        {
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f) + pos);

            ao0 = VertexAO(GetVoxel(x + 1, y, z - 1), GetVoxel(x, y + 1, z - 1), GetVoxel(x + 1, y + 1, z - 1));
            ao1 = VertexAO(GetVoxel(x + 1, y, z - 1), GetVoxel(x, y - 1, z - 1), GetVoxel(x + 1, y - 1, z - 1));
            ao2 = VertexAO(GetVoxel(x - 1, y, z - 1), GetVoxel(x, y - 1, z - 1), GetVoxel(x - 1, y - 1, z - 1));
            ao3 = VertexAO(GetVoxel(x - 1, y, z - 1), GetVoxel(x, y + 1, z - 1), GetVoxel(x - 1, y + 1, z - 1));
        }
        else if (dx == 1) // Prawo
        {
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f) + pos);

            ao0 = VertexAO(GetVoxel(x + 1, y + 1, z), GetVoxel(x + 1, y, z + 1), GetVoxel(x + 1, y + 1, z + 1));
            ao1 = VertexAO(GetVoxel(x + 1, y - 1, z), GetVoxel(x + 1, y, z + 1), GetVoxel(x + 1, y - 1, z + 1));
            ao2 = VertexAO(GetVoxel(x + 1, y - 1, z), GetVoxel(x + 1, y, z - 1), GetVoxel(x + 1, y - 1, z - 1));
            ao3 = VertexAO(GetVoxel(x + 1, y + 1, z), GetVoxel(x + 1, y, z - 1), GetVoxel(x + 1, y + 1, z - 1));
        }
        else if (dx == -1) // Lewo
        {
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f) + pos);

            ao0 = VertexAO(GetVoxel(x - 1, y + 1, z), GetVoxel(x - 1, y, z - 1), GetVoxel(x - 1, y + 1, z - 1));
            ao1 = VertexAO(GetVoxel(x - 1, y - 1, z), GetVoxel(x - 1, y, z - 1), GetVoxel(x - 1, y - 1, z - 1));
            ao2 = VertexAO(GetVoxel(x - 1, y - 1, z), GetVoxel(x - 1, y, z + 1), GetVoxel(x - 1, y - 1, z + 1));
            ao3 = VertexAO(GetVoxel(x - 1, y + 1, z), GetVoxel(x - 1, y, z + 1), GetVoxel(x - 1, y + 1, z + 1));
        }

        colors.Add(AOToColor(ao0));
        colors.Add(AOToColor(ao1));
        colors.Add(AOToColor(ao2));
        colors.Add(AOToColor(ao3));

        Vector3 normalDir = new Vector3(dx, dy, dz);
        normals.Add(normalDir);
        normals.Add(normalDir);
        normals.Add(normalDir);
        normals.Add(normalDir);


        if (ao0 + ao2 > ao1 + ao3)
        {
            triangles.Add(vCount);
            triangles.Add(vCount + 1);
            triangles.Add(vCount + 2);
            triangles.Add(vCount + 2);
            triangles.Add(vCount + 3);
            triangles.Add(vCount);
        }
        else
        {
            triangles.Add(vCount);
            triangles.Add(vCount + 1);
            triangles.Add(vCount + 3);
            triangles.Add(vCount + 1);
            triangles.Add(vCount + 2);
            triangles.Add(vCount + 3);
        }

        float texIdx = GetTextureIndex(blockID, dy);
        uvs.Add(new Vector3(0, 1, texIdx));
        uvs.Add(new Vector3(0, 0, texIdx));
        uvs.Add(new Vector3(1, 0, texIdx));
        uvs.Add(new Vector3(1, 1, texIdx));
    }

    private int VertexAO(int side1, int side2, int corner)
    {
        if (side1 != 0 && side2 != 0) return 3;
        return (side1 != 0 ? 1 : 0) + (side2 != 0 ? 1 : 0) + (corner != 0 ? 1 : 0);
    }

    private Color32 AOToColor(int ao)
    {
        float intensityFactor;
        switch (ao)
        {
            case 1: intensityFactor = 0.65f; break;
            case 2: intensityFactor = 0.40f; break;
            case 3: intensityFactor = 0.20f; break;
            default: intensityFactor = 1.0f; break;
        }

        byte intensity = (byte)(255 * intensityFactor);
        return new Color32(intensity, intensity, intensity, 255);
    }

    private float GetTextureIndex(byte blockID, int dy)
    {
        if (blockID == 1) // Grass
        {
            if (dy > 0) return 1;
            if (dy < 0) return 3;
            return 2;
        }
        if (blockID == 2) return 3; // Dirt
        if (blockID == 3) return 4; // Stone
        if (blockID == 4) return 5; // Island Debug
        if (blockID == 5) return 6; // coal
        if (blockID == 6) return 7; // iron
        return 0; // Error
    }
}