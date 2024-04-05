using System;
using UnityEngine;
using SheetCodes;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class WorldChunkInternal
{
    private const int THREADS = 8;

    public readonly int xIndex;
    public readonly int yIndex;
    public readonly EventVariable<WorldChunkInternal, int> highestDetailLevel;
    public readonly EventVariable<WorldChunkInternal, int>[] detailLevelsActivated;
    public readonly EventVariable<WorldChunkInternal, WorldChunkState> targetWorldChunkState;
    public readonly EventVariable<WorldChunkInternal, WorldChunkState> currentWorldChunkState;

    private ComputeShader normalMapComputeShader;
    private int generateNormalMapKernel;
    private WorldChunk worldChunk;
    private int currentTaskID;
    private int chunkSize => WorldGenerator.instance.chunkSize;

    public ChunkData chunkData { private set; get; }
    public WorldChunkInternal topLeftChunk { private set; get; }
    public WorldChunkInternal topChunk { private set; get; }
    public WorldChunkInternal topRightChunk { private set; get; }
    public WorldChunkInternal leftChunk { private set; get; }
    public WorldChunkInternal rightChunk { private set; get; }
    public WorldChunkInternal bottomLeftChunk { private set; get; }
    public WorldChunkInternal bottomChunk { private set; get; }
    public WorldChunkInternal bottomRightChunk { private set; get; }

    private WorldChunkState Check_TargetWorldChunkState(WorldChunkState value)
    {
        if (value == WorldChunkState.SurroundingChunksGenerated || value == WorldChunkState.Visualize)
            return value;

        if (value < currentWorldChunkState.value)
            return currentWorldChunkState.value >= WorldChunkState.SurroundingChunksGenerated ? WorldChunkState.SurroundingChunksGenerated : value;

        return value;
    }

    public WorldChunkInternal(int xIndex, int yIndex, int detailLevels)
    {
        normalMapComputeShader = ComputeShaderIdentifier.NormalMap.GetRecord().Shader;
        generateNormalMapKernel = normalMapComputeShader.FindKernel("GenerateNormals");

        this.xIndex = xIndex;
        this.yIndex = yIndex;
        this.highestDetailLevel = new EventVariable<WorldChunkInternal, int>(this, -1);
        detailLevelsActivated = new EventVariable<WorldChunkInternal, int>[detailLevels];
        for (int i = 0; i < detailLevels; i++)
        {
            detailLevelsActivated[i] = new EventVariable<WorldChunkInternal, int>(this, 0);
            detailLevelsActivated[i].onValueChange += OnValueChanged_DetailLevelActivated;
        }

        currentWorldChunkState = new EventVariable<WorldChunkInternal, WorldChunkState>(this, WorldChunkState.NotGenerated);
        targetWorldChunkState = new ControlledEventVariable<WorldChunkInternal, WorldChunkState>(this, WorldChunkState.NotGenerated, Check_TargetWorldChunkState);

        currentTaskID = -1;
        this.targetWorldChunkState.onValueChange += OnValueChanged_TargetWorldChunkState;
        this.highestDetailLevel.onValueChange += OnValueChanged_HighestDetailLevel;
    }

    private void OnValueChanged_DetailLevelActivated(int oldValue, int newValue)
    {
        int lowestIndex = -1;
        for(int i = 0; i < detailLevelsActivated.Length; i++)
        {
            if (detailLevelsActivated[i].value == 0)
                continue;

            lowestIndex = i;
            break;
        }
        highestDetailLevel.value = lowestIndex;
    }

    private void OnValueChanged_HighestDetailLevel(int oldValue, int newValue)
    {
        if (newValue >= 0)
        {
            targetWorldChunkState.value = WorldChunkState.Visualize;

            if (worldChunk != null)
                worldChunk.detailLevel.value = newValue;
        }
        else
            targetWorldChunkState.value = WorldChunkState.HeightsGenerated;
    }

    private void OnValueChanged_TargetWorldChunkState(WorldChunkState oldValue, WorldChunkState newValue)
    {
        //Already a task sceduled
        if (oldValue < newValue && currentTaskID != -1)
            return;

        if (oldValue > newValue)
        { 
            TaskManager.instance.CancelTask(currentTaskID);
            currentTaskID = -1;

            if (worldChunk != null)
            {
                GameObject.Destroy(worldChunk.gameObject);
                worldChunk = null;
            }

            if (currentWorldChunkState.value == WorldChunkState.Visualize && newValue < WorldChunkState.Visualize)
                currentWorldChunkState.value = WorldChunkState.SurroundingChunksGenerated;
        }

        if (newValue > currentWorldChunkState.value)
        {
            switch (currentWorldChunkState.value)
            {
                case WorldChunkState.NotGenerated:
                    AddTaskGenerateHeights();
                    break;
                case WorldChunkState.HeightsGenerated:
                    AddTaskGenerateSurroundingHeights();
                    break;
                case WorldChunkState.SurroundingChunksGenerated:
                    AddTaskGenerateNormals();
                    break;
                case WorldChunkState.NormalsGenerated:
                    AddTaskVisualizeChunk();
                    break;
            }
        }
    }

    private void SetGenerateHeights()
    {
        targetWorldChunkState.value = targetWorldChunkState.value > WorldChunkState.HeightsGenerated ? targetWorldChunkState.value : WorldChunkState.HeightsGenerated;
    }

    private void AddTaskGenerateHeights()
    {
        currentTaskID = TaskManager.instance.AddTask(GenerateHeights, 1);
    }

    private void AddTaskGenerateSurroundingHeights()
    {
        // Since this task has a lower priority than generating heights, after the task is called all other neighbouring heights will have been generated.

        bottomLeftChunk = WorldGenerator.instance.GetWorldChunkInternal(xIndex - 1, yIndex - 1);
        bottomChunk = WorldGenerator.instance.GetWorldChunkInternal(xIndex, yIndex - 1);
        bottomRightChunk = WorldGenerator.instance.GetWorldChunkInternal(xIndex + 1, yIndex - 1);

        leftChunk = WorldGenerator.instance.GetWorldChunkInternal(xIndex - 1, yIndex);
        rightChunk = WorldGenerator.instance.GetWorldChunkInternal(xIndex + 1, yIndex);

        topLeftChunk = WorldGenerator.instance.GetWorldChunkInternal(xIndex - 1, yIndex + 1);
        topChunk = WorldGenerator.instance.GetWorldChunkInternal(xIndex, yIndex + 1);
        topRightChunk = WorldGenerator.instance.GetWorldChunkInternal(xIndex + 1, yIndex + 1);

        bottomLeftChunk.MarkGenerateHeights();
        bottomChunk.MarkGenerateHeights();
        bottomRightChunk.MarkGenerateHeights();

        leftChunk.MarkGenerateHeights();
        rightChunk.MarkGenerateHeights();

        topLeftChunk.MarkGenerateHeights();
        topChunk.MarkGenerateHeights();
        topRightChunk.MarkGenerateHeights();

        currentTaskID = TaskManager.instance.AddTask(GenerateSurroundingHeights, 0);
    }

    private void AddTaskGenerateNormals()
    {
        currentTaskID = TaskManager.instance.AddTask(GenerateNormals, 2);
    }

    private void AddTaskVisualizeChunk()
    {
        currentTaskID = TaskManager.instance.AddTask(VisualizeChunk, 3);
    }

    private void FillEdgeHeights(ChunkEdgeDirection chunkDirection, float[] heights)
    {
        switch (chunkDirection)
        {
            case ChunkEdgeDirection.BottomLeft:
                chunkData.heightMapBuffer.GetData(heights, 0, chunkSize * (chunkSize - 1) - 2, 1);
                break;
            case ChunkEdgeDirection.Bottom:
                chunkData.heightMapBuffer.GetData(heights, 1, chunkSize * (chunkSize - 2), chunkSize);
                break;
            case ChunkEdgeDirection.BottomRight:
                chunkData.heightMapBuffer.GetData(heights, 1 + chunkSize, chunkSize * (chunkSize - 2) + 1, 1);
                break;
            case ChunkEdgeDirection.Right:
                float[] tempBuffer = new float[chunkSize * chunkSize];
                chunkData.heightMapBuffer.GetData(tempBuffer);

                for (int i = 0; i < chunkSize; i++)
                    heights[2 + chunkSize + i] = tempBuffer[chunkSize * i + 1];
                break;
            case ChunkEdgeDirection.TopRight:
                chunkData.heightMapBuffer.GetData(heights, 2 + chunkSize * 2, chunkSize + 1, 1);
                break;
            case ChunkEdgeDirection.Top:
                chunkData.heightMapBuffer.GetData(heights, 3 + chunkSize * 2, chunkSize, chunkSize);
                Array.Reverse(heights, 3 + chunkSize * 2, chunkSize);
                break;
            case ChunkEdgeDirection.TopLeft:
                chunkData.heightMapBuffer.GetData(heights, 3 + chunkSize * 3, chunkSize * 2 - 2, 1);
                break;
            case ChunkEdgeDirection.Left:
                tempBuffer = new float[chunkSize * chunkSize];
                chunkData.heightMapBuffer.GetData(tempBuffer);

                for (int i = 0; i < chunkSize; i++)
                    heights[4 + chunkSize * 3 + i] = tempBuffer[chunkSize * (chunkSize - i) - 2];
                break;
        }
    }

    private void MarkGenerateHeights()
    {
        if (targetWorldChunkState.value < WorldChunkState.HeightsGenerated)
            targetWorldChunkState.value = WorldChunkState.HeightsGenerated;
    }

    private void GenerateHeights()
    {
        chunkData = WorldGenerator.instance.GetChunkData(this);
        currentTaskID = -1;

        currentWorldChunkState.value = WorldChunkState.HeightsGenerated;
        if (currentWorldChunkState.value < targetWorldChunkState.value)
            AddTaskGenerateSurroundingHeights();
    }

    private void GenerateSurroundingHeights()
    {
        currentWorldChunkState.value = WorldChunkState.SurroundingChunksGenerated;
        if (currentWorldChunkState.value < targetWorldChunkState.value)
            AddTaskGenerateNormals();
    }

    private void GenerateNormals()
    {
        float[] edgeHeights = new float[(chunkSize + 1) * 4];
        bottomLeftChunk.FillEdgeHeights(ChunkEdgeDirection.BottomLeft, edgeHeights);
        bottomChunk.FillEdgeHeights(ChunkEdgeDirection.Bottom, edgeHeights);
        bottomRightChunk.FillEdgeHeights(ChunkEdgeDirection.BottomRight, edgeHeights);
        rightChunk.FillEdgeHeights(ChunkEdgeDirection.Right, edgeHeights);
        topRightChunk.FillEdgeHeights(ChunkEdgeDirection.TopRight, edgeHeights);
        topChunk.FillEdgeHeights(ChunkEdgeDirection.Top, edgeHeights);
        topLeftChunk.FillEdgeHeights(ChunkEdgeDirection.TopLeft, edgeHeights);
        leftChunk.FillEdgeHeights(ChunkEdgeDirection.Left, edgeHeights);
        ComputeBuffer edgeHeightsBuffer = new ComputeBuffer(edgeHeights.Length, Marshal.SizeOf<float>());
        edgeHeightsBuffer.SetData(edgeHeights);

        normalMapComputeShader.SetInt("size", chunkSize);
        normalMapComputeShader.SetBuffer(generateNormalMapKernel, "edgeHeights", edgeHeightsBuffer);
        normalMapComputeShader.SetBuffer(generateNormalMapKernel, "heightMap", chunkData.heightMapBuffer);
        normalMapComputeShader.SetBuffer(generateNormalMapKernel, "normalMap", chunkData.normalMapBuffer);
        normalMapComputeShader.Dispatch(generateNormalMapKernel, Mathf.CeilToInt((float)chunkSize / THREADS), Mathf.CeilToInt((float)chunkSize / THREADS), 1);
        edgeHeightsBuffer.Release();

        currentWorldChunkState.value = WorldChunkState.NormalsGenerated;
        if (currentWorldChunkState.value < targetWorldChunkState.value)
            AddTaskVisualizeChunk();
    }

    private void VisualizeChunk()
    {
        worldChunk = WorldGenerator.instance.CreateWorldChunk(this, highestDetailLevel.value);
        currentTaskID = -1;

        currentWorldChunkState.value = WorldChunkState.Visualize;
    }
}