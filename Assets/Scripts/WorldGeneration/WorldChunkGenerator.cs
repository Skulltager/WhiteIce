
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldChunkGenerator
{
    private readonly WorldChunk worldChunkPrefab;
    private readonly TileableComputePerlinNoise tileablePerlinNoise;
    private readonly Queue<ChunkPoint> chunksToGenerate;
    private readonly Queue<ChunkPoint> chunksToInstantiate;
    private readonly FourDirectionalList<WorldChunkData> worldChunkDatas;
    private readonly int mipMapCount;

    private ManualResetEvent generateChunkResetEvent;
    private bool stopThreading;

    public WorldChunkGenerator(WorldChunk prefab, TileableComputePerlinNoise tileablePerlinNoise, int mipMapCount)
    {
        this.worldChunkPrefab = prefab;
        this.tileablePerlinNoise = tileablePerlinNoise;
        this.mipMapCount = mipMapCount;
        chunksToGenerate = new Queue<ChunkPoint>();
        chunksToInstantiate = new Queue<ChunkPoint>();
        generateChunkResetEvent = new ManualResetEvent(false);
        Thread generateChunksThread = new Thread(GenerateChunks);
        generateChunksThread.Start();
    }

    public void GenerateChunk(int xIndex, int yIndex)
    {
        ChunkPoint chunkPoint = new ChunkPoint(xIndex, yIndex);

        lock (chunksToGenerate)
            chunksToGenerate.Enqueue(chunkPoint);

        generateChunkResetEvent.Set();
    }

    public void Update()
    {
        while(chunksToInstantiate.Count > 0)
        {
            ChunkPoint chunkPoint;
            lock (chunksToInstantiate)
                chunkPoint = chunksToInstantiate.Dequeue();

            WorldChunkData worldChunkData = worldChunkDatas.GetListItem(chunkPoint.xIndex, chunkPoint.yIndex);
            
            WorldChunk worldChunk = GameObject.Instantiate(worldChunkPrefab);
            tileablePerlinNoise.GenerateWorldChunk(worldChunk, chunkPoint.xIndex, chunkPoint.yIndex);
        }
    }

    private void GenerateChunks()
    {
        while(true)
        {
            if (chunksToGenerate.Count == 0)
            {
                generateChunkResetEvent.Reset();
                generateChunkResetEvent.WaitOne();
            }

            if (stopThreading)
                break;

            ChunkPoint chunkPoint;

            lock (chunksToGenerate)
                chunkPoint = chunksToGenerate.Dequeue();

            ChunkData chunkData = tileablePerlinNoise.GetChunkData(chunkPoint.xIndex, chunkPoint.yIndex);

            lock (worldChunkDatas)
            {
                lock (chunksToInstantiate)
                {
                    chunksToInstantiate.Enqueue(chunkPoint);
                    worldChunkDatas.AddItemToList(new WorldChunkData(chunkData.heightMapBuffer, chunkData.biomeBuffer, mipMapCount), chunkPoint.xIndex, chunkPoint.yIndex);
                }
            }
        }
    }

    public void Cleanup()
    {
        stopThreading = true;
        generateChunkResetEvent.Set();
    }
}