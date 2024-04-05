using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using SheetCodes;

public class TileableComputePerlinNoise
{
    private const int THREADS = 8;

    private readonly ComputeShader computeShader;
    private readonly int size;
    private readonly int roughSize;
    private readonly int sizeFactor;
    private readonly int heightMapBlendRadius;
    private readonly BiomeSettings[] biomes;
    private readonly int createNoiseMapKernel;
    private readonly int createTextureMapKernel;
    private readonly int smoothHeightMapKernel;
    private readonly int threadSize;
    private readonly int roughThreadSize;
    private readonly float chunkVisualSize;

    private readonly ComputeBuffer biomeLayersBuffer;
    private readonly ComputeBuffer biomeStepIndicesBuffer;

    private readonly ComputeBuffer biomeMapRoughBuffer;
    private readonly ComputeBuffer biomeColorBuffer;
    private readonly ComputeBuffer biomeScaleStrengthsBuffer;
    private readonly ComputeBuffer biomeClampStrengthsBuffer;
    private readonly ComputeBuffer biomeBlendStrengthsBuffer;
    private readonly ComputeBuffer biomeElevationStrengthsBuffer;

    private readonly ComputeBuffer biomeSeedsBuffer;
    private readonly ComputeBuffer biomeStepSizesBuffer;
    private readonly ComputeBuffer biomeStepRoughnessBuffer;

    private readonly ComputeBuffer heightMapRoughBuffer;
    private readonly ComputeBuffer heightLayersBuffer;
    private readonly ComputeBuffer heightStepIndicesBuffer;
    private readonly ComputeBuffer heightMinimumsBuffer;
    private readonly ComputeBuffer heightMaximumsBuffer;
    private readonly ComputeBuffer heightClampStrengthsBuffer;

    private readonly ComputeBuffer heightSeedsBuffer;
    private readonly ComputeBuffer heightStepSizesBuffer;
    private readonly ComputeBuffer heightStepRoughnessBuffer;

    private readonly FourDirectionalList<ChunkData> chunkDatas;

    public TileableComputePerlinNoise(BiomeSettings[] biomes, int sizeFactor, int heightMapBlendRadius, int seed, float chunkVisualSize)
    {
        this.computeShader = ComputeShaderIdentifier.PerlinNoise.GetRecord().Shader;
        this.sizeFactor = sizeFactor;
        this.size = (1 << sizeFactor) + 1;
        this.roughSize = size + heightMapBlendRadius * 2;
        this.biomes = biomes;
        this.heightMapBlendRadius = heightMapBlendRadius;

        this.chunkVisualSize = chunkVisualSize;
        threadSize = Mathf.CeilToInt(((float)size) / THREADS);
        roughThreadSize = Mathf.CeilToInt(((float)roughSize) / THREADS);

        createNoiseMapKernel = computeShader.FindKernel("GeneratePerlinNoiseHeights");
        createTextureMapKernel = computeShader.FindKernel("GeneratePerlinNoiseTexture");
        smoothHeightMapKernel = computeShader.FindKernel("SmoothHeightMap");

        System.Random random = new System.Random(seed);
        chunkDatas = new FourDirectionalList<ChunkData>(default);

        int[] biomeLayers = new int[biomes.Length];
        int[] biomeStepIndices = new int[biomes.Length];
        Color[] biomeColors = biomes.Select(i => i.color).ToArray();
        float[] biomeScaleStrength = biomes.Select(i => i.biomeScaleStrength).ToArray();
        float[] biomeClampStrength = biomes.Select(i => i.biomeClampStrength).ToArray();
        float[] biomeBlendStrength = biomes.Select(i => i.biomeBlendStrength).ToArray();
        float[] biomeElevationStrength = biomes.Select(i => i.biomeElevationStrength).ToArray();

        int[] heightLayers = new int[biomes.Length];
        int[] heightStepIndices = new int[biomes.Length];
        float[] heightMinimums = biomes.Select(i => i.minimumHeight).ToArray();
        float[] heightMaximums = biomes.Select(i => i.maximumHeight).ToArray();
        float[] heightClampStrength = biomes.Select(i => i.heightClampStrength).ToArray();

        float lowestHeight = float.MaxValue;
        float highestHeight = float.MinValue;

        int totalBiomeLayers = 0;
        int totalHeightLayers = 0;
        for (int i = 0; i < biomes.Length; i++)
        {
            biomeLayers[i] = biomes[i].biomeLayers.Length;
            biomeStepIndices[i] = totalBiomeLayers;
            totalBiomeLayers += biomes[i].biomeLayers.Length;

            heightLayers[i] = biomes[i].heightLayers.Length;
            heightStepIndices[i] = totalHeightLayers;
            totalHeightLayers += biomes[i].heightLayers.Length;

            lowestHeight = Mathf.Min(lowestHeight, biomes[i].minimumHeight);
            highestHeight = Mathf.Max(highestHeight, biomes[i].maximumHeight);

            if (biomeBlendStrength[i] == 0)
                biomeBlendStrength[i] = 0.00001f;
        }

        int[] biomeSeeds = new int[totalBiomeLayers];
        float[] biomeStepSizes = new float[totalBiomeLayers];
        float[] biomeStepRoughness = new float[totalBiomeLayers];

        int[] heightSeeds = new int[totalHeightLayers];
        float[] heightStepSizes = new float[totalHeightLayers];
        float[] heightStepRoughness = new float[totalHeightLayers];

        int biomeIndex = 0;
        int heightIndex = 0;
        for (int i = 0; i < biomes.Length; i++)
        {
            int biomeStartIndex = biomeIndex;
            int heightStartIndex = heightIndex;
            float biomeTotalRoughness = 0;
            float heightTotalRoughness = 0;

            for (int j = 0; j < biomes[i].biomeLayers.Length; j++)
            {
                biomeStepSizes[biomeIndex] = size * biomes[i].biomeLayers[j].sizeFactor;
                biomeSeeds[biomeIndex] = random.Next();
                biomeTotalRoughness += biomes[i].biomeLayers[j].roughness;
                biomeIndex++;
            }
            
            for (int j = 0; j < biomes[i].heightLayers.Length; j++)
            {
                heightStepSizes[heightIndex] = size * biomes[i].heightLayers[j].sizeFactor;
                heightSeeds[heightIndex] = random.Next();
                heightTotalRoughness += biomes[i].heightLayers[j].roughness;
                heightIndex++;
            }

            // Make sure the total roughness of an entire biome is set to 1
            biomeIndex = biomeStartIndex;
            for (int j = 0; j < biomes[i].biomeLayers.Length; j++)
            {
                biomeStepRoughness[biomeIndex] = biomes[i].biomeLayers[j].roughness / biomeTotalRoughness;
                biomeIndex++;
            }

            heightIndex = heightStartIndex;
            for (int j = 0; j < biomes[i].heightLayers.Length; j++)
            {
                heightStepRoughness[heightIndex] = biomes[i].heightLayers[j].roughness / heightTotalRoughness;
                heightIndex++;
            }
        }

        biomeLayersBuffer = new ComputeBuffer(biomeLayers.Length, Marshal.SizeOf<int>());
        biomeStepIndicesBuffer = new ComputeBuffer(biomeLayers.Length, Marshal.SizeOf<int>());

        biomeMapRoughBuffer = new ComputeBuffer(roughSize * roughSize * biomes.Length, Marshal.SizeOf<float>());
        biomeColorBuffer = new ComputeBuffer(biomeLayers.Length, Marshal.SizeOf<Color>());
        biomeScaleStrengthsBuffer = new ComputeBuffer(biomeLayers.Length, Marshal.SizeOf<float>());
        biomeClampStrengthsBuffer = new ComputeBuffer(biomeLayers.Length, Marshal.SizeOf<float>());
        biomeBlendStrengthsBuffer = new ComputeBuffer(biomeLayers.Length, Marshal.SizeOf<float>());
        biomeElevationStrengthsBuffer = new ComputeBuffer(biomeLayers.Length, Marshal.SizeOf<float>());

        biomeSeedsBuffer = new ComputeBuffer(totalBiomeLayers, Marshal.SizeOf<int>());
        biomeStepSizesBuffer = new ComputeBuffer(totalBiomeLayers, Marshal.SizeOf<float>());
        biomeStepRoughnessBuffer = new ComputeBuffer(totalBiomeLayers, Marshal.SizeOf<float>());

        heightLayersBuffer = new ComputeBuffer(heightLayers.Length, Marshal.SizeOf<int>());
        heightStepIndicesBuffer = new ComputeBuffer(heightLayers.Length, Marshal.SizeOf<int>());

        heightMapRoughBuffer = new ComputeBuffer(roughSize * roughSize, Marshal.SizeOf<float>());
        heightMinimumsBuffer = new ComputeBuffer(heightLayers.Length, Marshal.SizeOf<float>());
        heightMaximumsBuffer = new ComputeBuffer(heightLayers.Length, Marshal.SizeOf<float>());
        heightClampStrengthsBuffer = new ComputeBuffer(heightLayers.Length, Marshal.SizeOf<float>());
        
        heightSeedsBuffer = new ComputeBuffer(totalHeightLayers, Marshal.SizeOf<int>());
        heightStepSizesBuffer = new ComputeBuffer(totalHeightLayers, Marshal.SizeOf<float>());
        heightStepRoughnessBuffer = new ComputeBuffer(totalHeightLayers, Marshal.SizeOf<float>());

        int blendDiameter = (heightMapBlendRadius + 1) * 2 - 1;
        int smoothingTiles = (blendDiameter * blendDiameter + 1) / 2;

        float smoothFactor = 1f / smoothingTiles;
        computeShader.SetInt("smoothRadius", heightMapBlendRadius);
        computeShader.SetInt("roughSize", roughSize);
        computeShader.SetInt("size", size);
        computeShader.SetInt("biomes", biomes.Length);
        computeShader.SetFloat("smoothFactor", smoothFactor);
        computeShader.SetFloat("lowestHeight", lowestHeight);
        computeShader.SetFloat("highestHeight", highestHeight);

        computeShader.SetBuffer(createNoiseMapKernel, "biomeLayers", biomeLayersBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "biomeStepIndices", biomeStepIndicesBuffer);

        computeShader.SetBuffer(createNoiseMapKernel, "biomeScaleStrengths", biomeScaleStrengthsBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "biomeClampStrengths", biomeClampStrengthsBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "biomeBlendStrengths", biomeBlendStrengthsBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "biomeElevationStrengths", biomeElevationStrengthsBuffer);

        computeShader.SetBuffer(createNoiseMapKernel, "biomeSeeds", biomeSeedsBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "biomeStepSizes", biomeStepSizesBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "biomeStepRoughness", biomeStepRoughnessBuffer);

        computeShader.SetBuffer(createNoiseMapKernel, "heightLayers", heightLayersBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "heightStepIndices", heightStepIndicesBuffer);

        computeShader.SetBuffer(createNoiseMapKernel, "heightMinimums", heightMinimumsBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "heightMaximums", heightMaximumsBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "heightClampStrengths", heightClampStrengthsBuffer);
                                                       
        computeShader.SetBuffer(createNoiseMapKernel, "heightSeeds", heightSeedsBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "heightStepSizes", heightStepSizesBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "heightStepRoughness", heightStepRoughnessBuffer);

        computeShader.SetBuffer(createNoiseMapKernel, "roughBiomeMap", biomeMapRoughBuffer);
        computeShader.SetBuffer(createNoiseMapKernel, "roughHeightMap", heightMapRoughBuffer);

        computeShader.SetBuffer(createTextureMapKernel, "biomeColors", biomeColorBuffer);

        computeShader.SetBuffer(smoothHeightMapKernel, "roughBiomeMap", biomeMapRoughBuffer);
        computeShader.SetBuffer(smoothHeightMapKernel, "roughHeightMap", heightMapRoughBuffer);

        biomeLayersBuffer.SetData(biomeLayers);
        biomeStepIndicesBuffer.SetData(biomeStepIndices);

        biomeColorBuffer.SetData(biomeColors);
        biomeScaleStrengthsBuffer.SetData(biomeScaleStrength);
        biomeClampStrengthsBuffer.SetData(biomeClampStrength);
        biomeBlendStrengthsBuffer.SetData(biomeBlendStrength);
        biomeElevationStrengthsBuffer.SetData(biomeElevationStrength);

        biomeSeedsBuffer.SetData(biomeSeeds);
        biomeStepSizesBuffer.SetData(biomeStepSizes);
        biomeStepRoughnessBuffer.SetData(biomeStepRoughness);

        heightLayersBuffer.SetData(heightLayers);
        heightStepIndicesBuffer.SetData(heightStepIndices);

        heightMinimumsBuffer.SetData(heightMinimums);
        heightMaximumsBuffer.SetData(heightMaximums);
        heightClampStrengthsBuffer.SetData(heightClampStrength);

        heightSeedsBuffer.SetData(heightSeeds);
        heightStepSizesBuffer.SetData(heightStepSizes);
        heightStepRoughnessBuffer.SetData(heightStepRoughness);
    }

    public RenderTexture GetPerlinNoiseTexture(int xIndex, int yIndex)
    {
        ChunkData chunkData = GetChunkData(xIndex, yIndex);
        RenderTexture texture = new RenderTexture(size, size, 1, RenderTextureFormat.ARGBFloat);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.enableRandomWrite = true;
        texture.Create();
        computeShader.SetTexture(createTextureMapKernel, "heightTexture", texture);
        computeShader.SetBuffer(createTextureMapKernel, "heightMap", chunkData.heightMapBuffer);
        computeShader.SetBuffer(createTextureMapKernel, "biomeMap", chunkData.biomeBuffer);
        computeShader.Dispatch(createTextureMapKernel, threadSize, threadSize, 1);
        return texture;
    }

    public void GenerateWorldChunk(WorldChunk worldChunk, int xIndex, int yIndex)
    {
        worldChunk.Initialize(biomes.Length, sizeFactor, xIndex, yIndex, chunkVisualSize);
    }

    public ChunkData GetChunkData(int xIndex, int yIndex)
    {
        ChunkData chunkData;
        if (!chunkDatas.TryGetListItem(xIndex, yIndex, out chunkData))
        {
            int heightMapSize = size * size;
            ComputeBuffer heightMapBuffer = new ComputeBuffer(heightMapSize, Marshal.SizeOf<float>());
            ComputeBuffer biomeMapBuffer = new ComputeBuffer(heightMapSize * biomes.Length, Marshal.SizeOf<float>());
            computeShader.SetInt("xLocation", xIndex * (size - 1) - heightMapBlendRadius);
            computeShader.SetInt("yLocation", yIndex * (size - 1) - heightMapBlendRadius);
            float[] heightMap = new float[heightMapSize];
            float[] biomeMap = new float[heightMapSize * biomes.Length];

            computeShader.Dispatch(createNoiseMapKernel, roughThreadSize, roughThreadSize, 1);

            float[] roughHeightMap = new float[roughSize * roughSize];
            float[] roughBiomeMap = new float[roughSize * roughSize * biomes.Length];
            heightMapRoughBuffer.GetData(roughHeightMap);
            biomeMapRoughBuffer.GetData(roughBiomeMap);

            computeShader.SetBuffer(smoothHeightMapKernel, "heightMap", heightMapBuffer);
            computeShader.SetBuffer(smoothHeightMapKernel, "biomeMap", biomeMapBuffer);

            computeShader.Dispatch(smoothHeightMapKernel, threadSize, threadSize, 1);
            heightMapBuffer.GetData(heightMap);
            biomeMapBuffer.GetData(biomeMap);
            chunkData = new ChunkData(biomeMapBuffer, heightMapBuffer);
            chunkDatas.AddItemToList(chunkData , xIndex, yIndex);
        }

        return chunkData;
    }

    public void Cleanup()
    {
        biomeLayersBuffer.Release();
        biomeStepIndicesBuffer.Release();

        biomeMapRoughBuffer.Release();
        biomeColorBuffer.Release();
        biomeScaleStrengthsBuffer.Release();
        biomeClampStrengthsBuffer.Release();
        biomeBlendStrengthsBuffer.Release();
        biomeElevationStrengthsBuffer.Release(); 

        biomeSeedsBuffer.Release();
        biomeStepSizesBuffer.Release();
        biomeStepRoughnessBuffer.Release();
        
        heightLayersBuffer.Release();
        heightStepIndicesBuffer.Release();

        heightMapRoughBuffer.Release();
        heightMinimumsBuffer.Release();
        heightMaximumsBuffer.Release();
        heightClampStrengthsBuffer.Release();

        heightSeedsBuffer.Release();
        heightStepSizesBuffer.Release();
        heightStepRoughnessBuffer.Release();

        foreach (ChunkData chunkData in chunkDatas)
        {
            chunkData.biomeBuffer.Release();
            chunkData.heightMapBuffer.Release();
            chunkData.normalMapBuffer.Release();
        }
    }
}
