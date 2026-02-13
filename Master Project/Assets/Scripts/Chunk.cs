using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class Chunk : MonoBehaviour
{
    public NativeArray<byte> VoxelData;
    public Vector3Int GridCoord { get; private set; }

    private int size;
    private World worldRef;

    public ComputeShader dataShader;

    private ComputeShader voxelShader;
    private ComputeBuffer _voxelBuffer;

    [SerializeField] public MeshFilter meshFilter;
    [SerializeField] public MeshRenderer meshRenderer;

    public bool IsReady { get; private set; } = false;

    private static uint[] _clearArray;

    private JobHandle meshJobHandle;
    private bool isMeshJobActive = false;
    public bool IsMeshJobActive => isMeshJobActive;
    private NativeList<Vector3> jobVertices;
    private NativeList<int> jobTriangles;
    private NativeList<Vector3> jobUvs;
    private NativeList<Vector3> jobNormals;
    NativeList<Color32> jobColors;

    public void InitializeData(Vector3Int coord, int chunkSize, World world)
    {
        EnsureJobCompleted();

        GridCoord = coord;
        size = chunkSize;
        worldRef = world;
        IsReady = false;

        meshRenderer.enabled = false;

        int totalVoxels = size * size * size;
        int bufferSize = totalVoxels / 4;

        if (!VoxelData.IsCreated || VoxelData.Length != totalVoxels)
        {
            if (VoxelData.IsCreated) VoxelData.Dispose();
            VoxelData = new NativeArray<byte>(totalVoxels, Allocator.Persistent);
        }

        NativeArray<byte>.Copy(worldRef.EmptyVoxelData, VoxelData, totalVoxels);

        if (!jobVertices.IsCreated)
        {
            jobVertices = new NativeList<Vector3>(Allocator.Persistent);
            jobTriangles = new NativeList<int>(Allocator.Persistent);
            jobUvs = new NativeList<Vector3>(Allocator.Persistent);
            jobNormals = new NativeList<Vector3>(Allocator.Persistent);
            jobColors = new NativeList<Color32>(Allocator.Persistent);
        }
        else
        {
            jobVertices.Clear();
            jobTriangles.Clear();
            jobUvs.Clear();
            jobNormals.Clear();
            jobColors.Clear();
        }

        if (_clearArray == null || _clearArray.Length != bufferSize)
        {
            _clearArray = new uint[bufferSize];
        }

        if (_voxelBuffer == null || _voxelBuffer.count != bufferSize)
        {
            if (_voxelBuffer != null) _voxelBuffer.Release();
            _voxelBuffer = new ComputeBuffer(bufferSize, 4);
        }

        _voxelBuffer.SetData(_clearArray);

        voxelShader = dataShader;
        int kernel = voxelShader.FindKernel("GenerateVoxelData");
        voxelShader.SetBuffer(kernel, "ResultBuffer", _voxelBuffer);
        voxelShader.SetInt("ChunkSize", size);
        voxelShader.SetVector("ChunkOffset", transform.position);

        voxelShader.Dispatch(kernel, size / 8, size / 8, size / 8);

        AsyncGPUReadback.Request(_voxelBuffer, (AsyncGPUReadbackRequest request) =>
        {
            if (!VoxelData.IsCreated || coord != GridCoord) return;

            if (request.hasError)
            {
                Debug.LogWarning($"GPU Readback error at {coord}, retrying...");
                InitializeData(coord, size, worldRef);
                return;
            }

            var data = request.GetData<byte>();
            if (data.Length == VoxelData.Length)
            {
                data.CopyTo(VoxelData);
                IsReady = true;
                worldRef.OnDataReady(this);
            }
        });
    }

    public void UpdateMesh()
    {
        if (isMeshJobActive)
        {
            meshJobHandle.Complete();
            ApplyMeshData();
            isMeshJobActive = false;
        }

        jobVertices.Clear();
        jobTriangles.Clear();
        jobUvs.Clear();
        jobNormals.Clear();
        jobColors.Clear();

        int paddedSize = size + 2;
        int paddedLength = paddedSize * paddedSize * paddedSize;

        NativeArray<byte> paddedData = new NativeArray<byte>(paddedLength, Allocator.TempJob);

        BuildPaddedData(paddedData, paddedSize);

        MeshJob meshJob = new MeshJob
        {
            paddedData = paddedData,
            size = size,
            paddedSize = paddedSize,
            vertices = jobVertices,
            triangles = jobTriangles,
            uvs = jobUvs,
            normals = jobNormals,
            colors = jobColors
        };

        meshJobHandle = meshJob.Schedule();

        paddedData.Dispose(meshJobHandle);

        isMeshJobActive = true;
    }


    private void BuildPaddedData(NativeArray<byte> paddedData, int paddedSize)
    {
        Chunk lastCachedChunk = null;
        Vector3Int lastCachedChunkCoord = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

        for (int x = -1; x <= size; x++)
        {
            for (int y = -1; y <= size; y++)
            {
                for (int z = -1; z <= size; z++)
                {
                    int padIndex = (x + 1) + (y + 1) * paddedSize + (z + 1) * paddedSize * paddedSize;
                    byte voxel = 0;

                    if (x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size)
                    {
                        voxel = VoxelData[x + y * size + z * size * size];
                    }
                    else
                    {
                        Vector3Int globalPos = (GridCoord * size) + new Vector3Int(x, y, z);

                        int cx = Mathf.FloorToInt((float)globalPos.x / size);
                        int cy = Mathf.FloorToInt((float)globalPos.y / size);
                        int cz = Mathf.FloorToInt((float)globalPos.z / size);
                        Vector3Int targetChunkCoord = new Vector3Int(cx, cy, cz);

                        if (targetChunkCoord != lastCachedChunkCoord)
                        {
                            lastCachedChunk = worldRef.GetChunk(targetChunkCoord);
                            lastCachedChunkCoord = targetChunkCoord;
                        }

                        if (lastCachedChunk != null && lastCachedChunk.IsReady)
                        {
                            int lx = ((globalPos.x % size) + size) % size;
                            int ly = ((globalPos.y % size) + size) % size;
                            int lz = ((globalPos.z % size) + size) % size;
                            voxel = lastCachedChunk.VoxelData[lx + ly * size + lz * size * size];
                        }
                        else
                        {
                            voxel = 0;
                        }
                    }

                    paddedData[padIndex] = voxel;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (isMeshJobActive && meshJobHandle.IsCompleted)
        {
            meshJobHandle.Complete();
            ApplyMeshData();
            isMeshJobActive = false;
            meshRenderer.enabled = true;
        }
    }

    private void ApplyMeshData()
    {
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            meshFilter.sharedMesh = mesh;
        }

        mesh.Clear();

        if (jobVertices.Length > 0)
        {
            mesh.SetVertices(jobVertices.AsArray());
            mesh.SetIndices(jobTriangles.AsArray(), MeshTopology.Triangles, 0);
            mesh.SetUVs(0, jobUvs.AsArray());
            mesh.SetNormals(jobNormals.AsArray());
            mesh.bounds = new Bounds(new Vector3(size / 2f, size / 2f, size / 2f), new Vector3(size, size, size));
            mesh.SetColors(jobColors.AsArray());
        }
    }

    private byte GetVoxelFromWorldSlow(Vector3Int globalPos)
    {
        Vector3Int chunkCoord = new Vector3Int(
            Mathf.FloorToInt(globalPos.x / (float)size),
            Mathf.FloorToInt(globalPos.y / (float)size),
            Mathf.FloorToInt(globalPos.z / (float)size)
        );

        Chunk chunk = worldRef.GetChunk(chunkCoord);
        if (chunk == null || !chunk.IsReady) return 0;
        int lx = ((globalPos.x % size) + size) % size;
        int ly = ((globalPos.y % size) + size) % size;
        int lz = ((globalPos.z % size) + size) % size;

        return chunk.VoxelData[lx + ly * size + lz * size * size];
    }

    public void EnsureJobCompleted()
    {
        if (isMeshJobActive)
        {
            meshJobHandle.Complete();
            isMeshJobActive = false;
        }
    }

    private void OnDestroy()
    {
        EnsureJobCompleted();
        if (_voxelBuffer != null) { _voxelBuffer.Release(); _voxelBuffer = null; }
        if (VoxelData.IsCreated) VoxelData.Dispose();

        if (jobVertices.IsCreated) jobVertices.Dispose();
        if (jobTriangles.IsCreated) jobTriangles.Dispose();
        if (jobUvs.IsCreated) jobUvs.Dispose();
        if (jobNormals.IsCreated) jobNormals.Dispose();
        if (jobColors.IsCreated) jobColors.Dispose();
    }
}