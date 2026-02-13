using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct MeshJob : IJob
{
    [ReadOnly] public NativeArray<byte> centerData;
    [ReadOnly] public NativeArray<byte> up, down, left, right, front, back;
    public int size;

    public NativeList<Vector3> vertices;
    public NativeList<int> triangles;
    public NativeList<Vector3> uvs;
    public NativeList<Vector3> normals;
    public NativeList<Color32> colors;

    public void Execute()
    {
        int totalVoxels = size * size * size;

        for (int i = 0; i < totalVoxels; i++)
        {
            int x = i % size;
            int y = (i / size) % size;
            int z = i / (size * size);

            byte blockID = centerData[i];
            if (blockID == 0) continue;

            CheckFace(x, y, z, 0, 1, 0, blockID, up);    // Góra
            CheckFace(x, y, z, 0, -1, 0, blockID, down);  // Dó³
            CheckFace(x, y, z, 0, 0, 1, blockID, front);  // Przód
            CheckFace(x, y, z, 0, 0, -1, blockID, back);  // Ty³
            CheckFace(x, y, z, 1, 0, 0, blockID, right);  // Prawo
            CheckFace(x, y, z, -1, 0, 0, blockID, left);  // Lewo
        }
    }


    private void CheckFace(int x, int y, int z, int dx, int dy, int dz, byte blockID, NativeArray<byte> neighborChunk)
    {
        int nx = x + dx;
        int ny = y + dy;
        int nz = z + dz;

        bool isNeighborSolid = false;
        if (nx >= 0 && nx < size && ny >= 0 && ny < size && nz >= 0 && nz < size)
        {
            isNeighborSolid = centerData[nx + (ny * size) + (nz * size * size)] != 0;
        }
        else if (neighborChunk.Length > 0)
        {
            int localNX = (nx % size + size) % size;
            int localNY = (ny % size + size) % size;
            int localNZ = (nz % size + size) % size;
            isNeighborSolid = neighborChunk[localNX + (localNY * size) + (localNZ * size * size)] != 0;
        }
        else
        {
            isNeighborSolid = false;
        }

        if (!isNeighborSolid) AddFaceData(x, y, z, dx, dy, dz, blockID);
    }

    private void AddFaceData(int x, int y, int z, int dx, int dy, int dz, byte blockID)
    {
        int vCount = vertices.Length;
        Vector3 pos = new Vector3(x, y, z);

        if (dy == 1) // up
        {
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f) + pos);

            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y + 1, z), GetVoxel(x, y + 1, z - 1), GetVoxel(x - 1, y + 1, z - 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y + 1, z), GetVoxel(x, y + 1, z + 1), GetVoxel(x - 1, y + 1, z + 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y + 1, z), GetVoxel(x, y + 1, z + 1), GetVoxel(x + 1, y + 1, z + 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y + 1, z), GetVoxel(x, y + 1, z - 1), GetVoxel(x + 1, y + 1, z - 1))));
        }
        else if (dy == -1) // down
        {
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f) + pos);

            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y - 1, z), GetVoxel(x, y - 1, z + 1), GetVoxel(x - 1, y - 1, z + 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y - 1, z), GetVoxel(x, y - 1, z - 1), GetVoxel(x - 1, y - 1, z - 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y - 1, z), GetVoxel(x, y - 1, z - 1), GetVoxel(x + 1, y - 1, z - 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y - 1, z), GetVoxel(x, y - 1, z + 1), GetVoxel(x + 1, y - 1, z + 1))));
        }
        else if (dz == 1) // front
        {
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f) + pos);

            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y, z + 1), GetVoxel(x, y + 1, z + 1), GetVoxel(x - 1, y + 1, z + 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y, z + 1), GetVoxel(x, y - 1, z + 1), GetVoxel(x - 1, y - 1, z + 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y, z + 1), GetVoxel(x, y - 1, z + 1), GetVoxel(x + 1, y - 1, z + 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y, z + 1), GetVoxel(x, y + 1, z + 1), GetVoxel(x + 1, y + 1, z + 1))));
        }
        else if (dz == -1) // back
        {
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f) + pos);

            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y, z - 1), GetVoxel(x, y + 1, z - 1), GetVoxel(x + 1, y + 1, z - 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y, z - 1), GetVoxel(x, y - 1, z - 1), GetVoxel(x + 1, y - 1, z - 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y, z - 1), GetVoxel(x, y - 1, z - 1), GetVoxel(x - 1, y - 1, z - 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y, z - 1), GetVoxel(x, y + 1, z - 1), GetVoxel(x - 1, y + 1, z - 1))));
        }
        else if (dx == 1) // right
        {
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f) + pos);

            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y + 1, z), GetVoxel(x + 1, y, z + 1), GetVoxel(x + 1, y + 1, z + 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y - 1, z), GetVoxel(x + 1, y, z + 1), GetVoxel(x + 1, y - 1, z + 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y - 1, z), GetVoxel(x + 1, y, z - 1), GetVoxel(x + 1, y - 1, z - 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x + 1, y + 1, z), GetVoxel(x + 1, y, z - 1), GetVoxel(x + 1, y + 1, z - 1))));
        }
        else if (dx == -1) // left
        {
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f) + pos);
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f) + pos);

            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y + 1, z), GetVoxel(x - 1, y, z - 1), GetVoxel(x - 1, y + 1, z - 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y - 1, z), GetVoxel(x - 1, y, z - 1), GetVoxel(x - 1, y - 1, z - 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y - 1, z), GetVoxel(x - 1, y, z + 1), GetVoxel(x - 1, y - 1, z + 1))));
            colors.Add(AOToColor(VertexAO(GetVoxel(x - 1, y + 1, z), GetVoxel(x - 1, y, z + 1), GetVoxel(x - 1, y + 1, z + 1))));
        }

        Vector3 normalDir = new Vector3(dx, dy, dz);
        for (int i = 0; i < 4; i++) normals.Add(normalDir);

        triangles.Add(vCount);
        triangles.Add(vCount + 1);
        triangles.Add(vCount + 2);
        triangles.Add(vCount + 2);
        triangles.Add(vCount + 3);
        triangles.Add(vCount);

        float texIdx = GetTextureIndex(blockID, dy);
        uvs.Add(new Vector3(0, 1, texIdx));
        uvs.Add(new Vector3(0, 0, texIdx));
        uvs.Add(new Vector3(1, 0, texIdx));
        uvs.Add(new Vector3(1, 1, texIdx));
    }

    private int GetVoxel(int x, int y, int z)
    {
        bool xValid = x >= 0 && x < size;
        bool yValid = y >= 0 && y < size;
        bool zValid = z >= 0 && z < size;
        int index = -1;
        if (x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size)
        {
            index = x + (y * size) + (z * size * size);
            return centerData[index];
        }
        else if (x < 0 && yValid && zValid)
        {
            index = (x + size) + (y * size) + (z * size * size);
            return (left.Length > 0) ? left[index] : 0;
        }
        else if (x >= size && yValid && zValid)
        {
            index = (x - size) + (y * size) + (z * size * size);
            return (right.Length > 0) ? right[index] : 0;
        }
        else if (xValid && y < 0 && zValid)
        {
            index = x + ((y + size) * size) + (z * size * size);
            return (down.Length > 0) ? down[index] : 0;
        }
        else if (xValid && y >= size && zValid)
        {
            index = x + ((y - size) * size) + (z * size * size);
            return (up.Length > 0) ? up[index] : 0;
        }
        else if (xValid && yValid && z < 0)
        {
            index = x + (y * size) + ((z + size) * size * size);
            return (back.Length > 0) ? back[index] : 0;
        }
        else if (xValid && yValid && z >= size)
        {
            index = x + (y * size) + ((z - size) * size * size);
            return (front.Length > 0) ? front[index] : 0;
        }
        else
        {
            return 0;
        }
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
            case 1: intensityFactor = 0.5f; break;
            case 2: intensityFactor = 0.15f; break;
            case 3: intensityFactor = 0.0f; break;
            default: intensityFactor = 1.0f; break;
        }

        byte intensity = (byte)(255 * intensityFactor);
        return new Color32(intensity, intensity, intensity, 255);
    }
    private float GetTextureIndex(byte blockID, int dy)
    {
        if (blockID == 1) // Grass
        {
            if (dy > 0) return 1;      // Góra trawy
            if (dy < 0) return 3;      // Spód (ziemia)
            return 2;                  // Boki trawy
        }
        if (blockID == 2) return 3;    // Dirt
        if (blockID == 3) return 4;    // Stone
        if (blockID == 4) return 5;    // Island Debug Color
        return 0; // Magenta / Error
    }
}