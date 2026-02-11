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

    private NativeList<Vector3> jobVertices;
    private NativeList<int> jobTriangles;
    private NativeList<Vector3> jobUvs;
    private NativeList<Vector3> jobNormals;
    NativeList<Color32> jobColors;

    public void InitializeData(Vector3Int coord, int chunkSize, World world)
    {
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
            if (request.hasError || !VoxelData.IsCreated || coord != GridCoord) return;

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

        MeshJob meshJob = new MeshJob
        {
            centerData = VoxelData,
            up = GetNeighborData(Vector3Int.up),
            down = GetNeighborData(Vector3Int.down),
            left = GetNeighborData(Vector3Int.left),
            right = GetNeighborData(Vector3Int.right),
            front = GetNeighborData(new Vector3Int(0, 0, 1)),
            back = GetNeighborData(new Vector3Int(0, 0, -1)),
            size = size,
            vertices = jobVertices,
            triangles = jobTriangles,
            uvs = jobUvs,
            normals = jobNormals,
            colors = jobColors
        };

        meshJobHandle = meshJob.Schedule();
        isMeshJobActive = true;
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
            mesh.RecalculateBounds();
            mesh.SetColors(jobColors.AsArray());
        }
    }

    private NativeArray<byte> GetNeighborData(Vector3Int offset)
    {
        Chunk neighbor = worldRef.GetChunk(GridCoord + offset);
        if (neighbor == null || !neighbor.IsReady)
            return worldRef.EmptyVoxelData;

        return neighbor.VoxelData;
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