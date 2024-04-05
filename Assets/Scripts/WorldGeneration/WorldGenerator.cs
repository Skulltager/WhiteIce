
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using System;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator instance { private set; get; }

    //[SerializeField] private TextureDisplay textureDisplayPrefab = default;
    [SerializeField] private WorldChunk worldChunkPrefab = default;
    [SerializeField] private Material worldChunkMaterial = default;
    [SerializeField] private int seed = default;
    [SerializeField] private int chunkDetailLevels = default;
    [SerializeField] private float chunkVisualSize = default;
    [SerializeField] private int chunkSizeFactor = default;
    [SerializeField] private int heightMapBlendRadius = default;
    [SerializeField] private bool autoRegenerate = default;
    [SerializeField] private BiomeSettings[] biomeSettings = default;

    //private List<TextureDisplay> textureDisplays;
    private TileableComputePerlinNoise biomeGenerator;
    private EventList<ChunkPartitioner> chunkPartitioners;
    private FourDirectionalList<WorldChunkInternal> internalWorldChunks;

    private ComputeBuffer textureIndicesBuffer;

    private float chunkVisualSizeSquared;
    public float ChunkVisualSize => chunkVisualSize;
    public int chunkSize { private set; get; }

    private void Awake()
    {
        instance = this;
        chunkVisualSizeSquared = chunkVisualSize * chunkVisualSize;
        chunkPartitioners = new EventList<ChunkPartitioner>();
        internalWorldChunks = new FourDirectionalList<WorldChunkInternal>(null);

        Initialize();
    }

    private void Initialize()
    {
        biomeGenerator = new TileableComputePerlinNoise(biomeSettings, chunkSizeFactor, heightMapBlendRadius, seed, chunkVisualSize);
        textureIndicesBuffer = new ComputeBuffer(biomeSettings.Length, Marshal.SizeOf<int>());

        chunkSize = (1 << chunkSizeFactor) + 1;

        uint[] textureIndices = biomeSettings.Select(i => i.textureIndex).ToArray();
        textureIndicesBuffer.SetData(textureIndices);

        worldChunkMaterial.SetBuffer("biomeTextureIndices", textureIndicesBuffer);
        worldChunkMaterial.SetInt("biomes", biomeSettings.Length);
        worldChunkMaterial.SetInt("heightMapWidth", chunkSize);
        worldChunkMaterial.SetInt("biomeMapStride", chunkSize * chunkSize);
        chunkPartitioners.onAdd += OnAdd_ChunkPartitioner;
        chunkPartitioners.onRemove += OnRemove_ChunkPartitioner;
    }

    private void OnAdd_ChunkPartitioner(ChunkPartitioner item)
    {
        item.currentChunkPoint.onValueChangeSource += OnValueChanged_ChunkPartitioner_CurrentChunkPoint;

        int chunkRadius = Mathf.CeilToInt(item.highestChunkViewDistance / chunkVisualSize);

        for (int i = -chunkRadius; i <= chunkRadius; i++)
        {
            for (int j = -chunkRadius; j <= chunkRadius; j++)
            {
                float chunkDistanceSquared = i * i * chunkVisualSizeSquared + j * j * chunkVisualSizeSquared;

                if (chunkDistanceSquared > item.highestChunkViewDistanceSquared)
                    continue;

                int detailLevel = Array.FindIndex(item.chunkViewDistancesSquared, h => chunkDistanceSquared < h);
                int xIndex = item.currentChunkPoint.value.xIndex + i;
                int yIndex = item.currentChunkPoint.value.yIndex + j;

                WorldChunkInternal internalChunk;
                if (!internalWorldChunks.TryGetListItem(xIndex, yIndex, out internalChunk))
                {
                    internalChunk = new WorldChunkInternal(xIndex, yIndex, chunkDetailLevels);
                    internalWorldChunks.AddItemToList(internalChunk, xIndex, yIndex);
                }

                internalChunk.detailLevelsActivated[detailLevel].value++;
            }
        }
    }

    private void OnRemove_ChunkPartitioner(ChunkPartitioner item)
    {
        item.currentChunkPoint.onValueChangeSource -= OnValueChanged_ChunkPartitioner_CurrentChunkPoint;

        int chunkRadius = Mathf.CeilToInt(item.highestChunkViewDistance / chunkVisualSize);

        for (int i = -chunkRadius; i <= chunkRadius; i++)
        {
            for (int j = -chunkRadius; j <= chunkRadius; j++)
            {
                float chunkDistanceSquared = i * i * chunkVisualSizeSquared + j * j * chunkVisualSizeSquared;

                if (chunkDistanceSquared > item.highestChunkViewDistanceSquared)
                    continue;

                int detailLevel = Array.FindIndex(item.chunkViewDistancesSquared, h => chunkDistanceSquared < h);
                int xIndex = item.currentChunkPoint.value.xIndex + i;
                int yIndex = item.currentChunkPoint.value.yIndex + j;

                WorldChunkInternal internalChunk = internalWorldChunks.GetListItem(xIndex, yIndex);
                internalChunk.detailLevelsActivated[detailLevel].value--;
            }
        }
    }

    private void OnValueChanged_ChunkPartitioner_CurrentChunkPoint(ChunkPartitioner source, ChunkPoint oldValue, ChunkPoint newValue)
    {
        int chunkRadius = Mathf.CeilToInt(source.highestChunkViewDistance / chunkVisualSize);

        for(int i = -chunkRadius; i <= chunkRadius; i++)
        {
            for(int j = -chunkRadius; j <= chunkRadius; j++)
            {
                float chunkDistanceSquared = i * i * chunkVisualSizeSquared + j * j * chunkVisualSizeSquared;

                if (chunkDistanceSquared > source.highestChunkViewDistanceSquared)
                    continue;

                int detailLevel = Array.FindIndex(source.chunkViewDistancesSquared, h => chunkDistanceSquared < h);
                int xIndex = oldValue.xIndex + i;
                int yIndex = oldValue.yIndex + j;

                WorldChunkInternal internalChunk = internalWorldChunks.GetListItem(xIndex, yIndex);
                internalChunk.detailLevelsActivated[detailLevel].value--;

                detailLevel = Array.FindIndex(source.chunkViewDistancesSquared, h => chunkDistanceSquared < h);
                xIndex = newValue.xIndex + i;
                yIndex = newValue.yIndex + j;

                if (!internalWorldChunks.TryGetListItem(xIndex, yIndex, out internalChunk))
                {
                    internalChunk = new WorldChunkInternal(xIndex, yIndex, chunkDetailLevels);
                    internalWorldChunks.AddItemToList(internalChunk, xIndex, yIndex);
                }

                internalChunk.detailLevelsActivated[detailLevel].value++;
            }
        }
    }

    public WorldChunkInternal GetWorldChunkInternal(int xIndex, int yIndex)
    {
        WorldChunkInternal worldChunkInternal;
        if (!internalWorldChunks.TryGetListItem(xIndex, yIndex, out worldChunkInternal))
        {
            worldChunkInternal = new WorldChunkInternal(xIndex, yIndex, chunkDetailLevels);
            internalWorldChunks.AddItemToList(worldChunkInternal, xIndex, yIndex);
        }

        return worldChunkInternal;
    }

    public void AddChunkPartitioner(ChunkPartitioner chunkPartitioner)
    {
        chunkPartitioners.Add(chunkPartitioner);
    }

    public void RemoveChunkPartitioner(ChunkPartitioner chunkPartitioner)
    {
        chunkPartitioners.Remove(chunkPartitioner);
    }

    private void OnDestroy()
    {
        biomeGenerator.Cleanup();
        textureIndicesBuffer.Dispose();
    }

    public ChunkData GetChunkData(WorldChunkInternal internalChunk)
    {
        return biomeGenerator.GetChunkData(internalChunk.xIndex, internalChunk.yIndex);
    }

    public WorldChunk CreateWorldChunk(WorldChunkInternal internalChunk, int mipMapLevel)
    {
        WorldChunk worldChunk = GameObject.Instantiate(worldChunkPrefab, transform);
        worldChunk.SetInternalWorldChunk(internalChunk);
        biomeGenerator.GenerateWorldChunk(worldChunk, internalChunk.xIndex, internalChunk.yIndex);
        worldChunk.detailLevel.value = mipMapLevel;
        return worldChunk;
    }
}