
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using SheetCodes;

public class WorldChunk : MonoBehaviour
{
    private const bool USE_ADVANCED_GENERATION = true;
    private const int COMPUTE_THREADS_EDGE = 8;
    private const int COMPUTE_THREADS_CENTER = 4;
    private const int COMPUTE_THREADS_CHUNK = 8;

    [SerializeField] private MeshRenderer meshRenderer = default;
    [SerializeField] private MeshFilter meshFilter = default;
    [SerializeField] private MeshCollider meshCollider = default;

    private ComputeShader meshGenerationShader;
    private WorldChunkInternal worldChunkInternal;
    private Material meshMaterial;
    private int generateEdgesKernel;
    private int generateCenterKernel;
    private int generateChunkKernel;

    private int taskID;
    private int chunkSizeFactor;
    private Mesh[] detailLevelMeshes;

    public readonly EventVariable<WorldChunk, int> detailLevel;

    private WorldChunk()
    {
        detailLevel = new EventVariable<WorldChunk, int>(this, -1);
    }

    private void Awake()
    {
        detailLevel.onValueChange += OnValueChanged_DetailLevel;

        meshGenerationShader = ComputeShaderIdentifier.ChunkMesh.GetRecord().Shader;
        generateEdgesKernel = meshGenerationShader.FindKernel("GenerateEdges");
        generateCenterKernel = meshGenerationShader.FindKernel("GenerateCenter");
        generateChunkKernel = meshGenerationShader.FindKernel("GenerateChunk");
        meshMaterial = meshRenderer.material;
    }

    private void OnValueChanged_DetailLevel(int oldValue, int newValue)
    {
        if (taskID > 0)
        {
            TaskManager.instance.CancelTask(taskID);
            taskID = -1;
        }

        if (detailLevelMeshes[newValue] == null)
        {
            taskID = TaskManager.instance.AddTask(GenerateMeshTask, 2);
            return;
        }

        meshCollider.sharedMesh = detailLevelMeshes[newValue];
        meshFilter.mesh = detailLevelMeshes[newValue];
    }

    private void GenerateMeshTask()
    {
        int chunkSizeDisplayFactor = chunkSizeFactor - detailLevel.value;

        if (USE_ADVANCED_GENERATION && chunkSizeDisplayFactor < chunkSizeFactor)
        {
            int edgeSize = (1 << chunkSizeFactor) + 1;
            int centerSize = (1 << chunkSizeDisplayFactor) - 1;

            int totalEdgeVertices = (edgeSize - 1) * 4;
            int totalCenterVertices = centerSize * centerSize;

            int centerTriangles = (centerSize - 1) * (centerSize - 1) * 2;
            int edgeTriangles = (edgeSize + centerSize - 2) * 4;

            int totalVertices = totalEdgeVertices + totalCenterVertices;
            int totalIndices = (centerTriangles + edgeTriangles) * 3;

            int gapSize = 1 << (chunkSizeFactor - chunkSizeDisplayFactor);
            ComputeBuffer vertexPositionBuffer = new ComputeBuffer(totalVertices, Marshal.SizeOf<Vector3>());
            ComputeBuffer normalsBuffer = new ComputeBuffer(totalVertices, Marshal.SizeOf<Vector3>());
            ComputeBuffer indicesBuffer = new ComputeBuffer(totalIndices, Marshal.SizeOf<int>());

            meshGenerationShader.SetInt("gapSize", gapSize);
            meshGenerationShader.SetInt("totalEdgeTriangles", edgeTriangles);
            meshGenerationShader.SetInt("totalEdgeVertices", totalEdgeVertices);
            meshGenerationShader.SetInt("edgeSize", edgeSize);
            meshGenerationShader.SetInt("centerSize", centerSize);
            meshGenerationShader.SetBuffer(generateEdgesKernel, "normalMap", worldChunkInternal.chunkData.normalMapBuffer);
            meshGenerationShader.SetBuffer(generateEdgesKernel, "heightMap", worldChunkInternal.chunkData.heightMapBuffer);
            meshGenerationShader.SetBuffer(generateEdgesKernel, "vertexPositions", vertexPositionBuffer);
            meshGenerationShader.SetBuffer(generateEdgesKernel, "normals", normalsBuffer);
            meshGenerationShader.SetBuffer(generateEdgesKernel, "indices", indicesBuffer);

            meshGenerationShader.Dispatch(generateEdgesKernel, Mathf.CeilToInt((float)(edgeSize - 1) / COMPUTE_THREADS_EDGE), 1, 1);

            meshGenerationShader.SetBuffer(generateCenterKernel, "normalMap", worldChunkInternal.chunkData.normalMapBuffer);
            meshGenerationShader.SetBuffer(generateCenterKernel, "heightMap", worldChunkInternal.chunkData.heightMapBuffer);
            meshGenerationShader.SetBuffer(generateCenterKernel, "vertexPositions", vertexPositionBuffer);
            meshGenerationShader.SetBuffer(generateCenterKernel, "normals", normalsBuffer);
            meshGenerationShader.SetBuffer(generateCenterKernel, "indices", indicesBuffer);

            meshGenerationShader.Dispatch(generateCenterKernel, Mathf.CeilToInt((float)centerSize / COMPUTE_THREADS_CENTER), Mathf.CeilToInt((float)centerSize / COMPUTE_THREADS_CENTER), 1);

            Vector3[] vertices = new Vector3[totalVertices];
            Vector3[] normals = new Vector3[totalVertices];
            int[] indices = new int[totalIndices];

            vertexPositionBuffer.GetData(vertices);
            normalsBuffer.GetData(normals);
            indicesBuffer.GetData(indices);

            vertexPositionBuffer.Dispose();
            normalsBuffer.Dispose();
            indicesBuffer.Dispose();

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);
            mesh.RecalculateBounds();
            detailLevelMeshes[detailLevel.value] = mesh;
        }
        else
        {
            int chunkSize = (1 << chunkSizeDisplayFactor) + 1;
            int gapSize = 1 << (chunkSizeFactor - chunkSizeDisplayFactor);

            int totalVertices = chunkSize * chunkSize;
            int totalIndices = (chunkSize - 1) * (chunkSize - 1) * 4;

            ComputeBuffer vertexPositionBuffer = new ComputeBuffer(totalVertices, Marshal.SizeOf<Vector3>());
            ComputeBuffer normalsBuffer = new ComputeBuffer(totalVertices, Marshal.SizeOf<Vector3>());
            ComputeBuffer indicesBuffer = new ComputeBuffer(totalIndices, Marshal.SizeOf<int>());

            meshGenerationShader.SetInt("chunkSize", chunkSize);
            meshGenerationShader.SetInt("gapSize", gapSize);
            meshGenerationShader.SetBuffer(generateChunkKernel, "normalMap", worldChunkInternal.chunkData.normalMapBuffer);
            meshGenerationShader.SetBuffer(generateChunkKernel, "heightMap", worldChunkInternal.chunkData.heightMapBuffer);
            meshGenerationShader.SetBuffer(generateChunkKernel, "vertexPositions", vertexPositionBuffer);
            meshGenerationShader.SetBuffer(generateChunkKernel, "normals", normalsBuffer);
            meshGenerationShader.SetBuffer(generateChunkKernel, "indices", indicesBuffer);

            meshGenerationShader.Dispatch(generateChunkKernel, Mathf.CeilToInt((float)chunkSize / COMPUTE_THREADS_CHUNK), Mathf.CeilToInt((float)chunkSize / COMPUTE_THREADS_CHUNK), 1);

            Vector3[] vertices = new Vector3[totalVertices];
            Vector3[] normals = new Vector3[totalVertices];
            int[] indices = new int[totalIndices];

            vertexPositionBuffer.GetData(vertices);
            normalsBuffer.GetData(normals);
            indicesBuffer.GetData(indices);

            vertexPositionBuffer.Dispose();
            normalsBuffer.Dispose();
            indicesBuffer.Dispose();

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.SetIndices(indices, MeshTopology.Quads, 0);

            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);

            detailLevelMeshes[detailLevel.value] = mesh;
        }

        meshCollider.sharedMesh = detailLevelMeshes[detailLevel.value];
        meshFilter.mesh = detailLevelMeshes[detailLevel.value];
    }

    public void SetInternalWorldChunk(WorldChunkInternal worldChunkInternal)
    {
        this.worldChunkInternal = worldChunkInternal;
        meshMaterial.SetBuffer("heightMap", worldChunkInternal.chunkData.heightMapBuffer);
        meshMaterial.SetBuffer("biomeMap", worldChunkInternal.chunkData.biomeBuffer);
    }

    public void Initialize(int biomes, int chunkSizeFactor, int xPosition, int yPosition, float visualSize)
    {
        detailLevelMeshes = new Mesh[chunkSizeFactor];
        this.chunkSizeFactor = chunkSizeFactor;
        meshGenerationShader.SetInt("biomes", biomes);
        meshGenerationShader.SetInt("heightMapSize", (1 << chunkSizeFactor) + 1);

        float scale = visualSize / (1 << chunkSizeFactor);
        transform.localScale = new Vector3(scale, 1, scale);
        transform.localPosition = new Vector3(xPosition * visualSize, 0, yPosition * visualSize);
    }

    private void OnDestroy()
    {
        if (taskID >= 0)
            TaskManager.instance.CancelTask(taskID);

        for(int i = 0; i < detailLevelMeshes.Length; i++)
        {
            if (detailLevelMeshes[i] == null)
                continue;

            GameObject.Destroy(detailLevelMeshes[i]);
        }
        detailLevel.onValueChange -= OnValueChanged_DetailLevel;
    }
}