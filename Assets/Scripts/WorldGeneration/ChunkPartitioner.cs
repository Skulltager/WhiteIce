using UnityEngine;

public class ChunkPartitioner : MonoBehaviour
{
    public readonly EventVariable<ChunkPartitioner, ChunkPoint> currentChunkPoint;
    [SerializeField] private float[] chunkViewDistances;

    public float[] chunkViewDistancesSquared { private set; get; }
    public float highestChunkViewDistance { private set; get; }
    public float highestChunkViewDistanceSquared { private set; get; }
    private float chunkVisualSize => WorldGenerator.instance.ChunkVisualSize;

    private ChunkPartitioner()
    {
        currentChunkPoint = new EventVariable<ChunkPartitioner, ChunkPoint>(this, default);
    }

    private void Awake()
    {
        chunkViewDistancesSquared = new float[chunkViewDistances.Length];
        highestChunkViewDistanceSquared = float.MinValue;
        highestChunkViewDistance = float.MinValue;
        for (int i = 0; i < chunkViewDistances.Length; i++)
        {
            chunkViewDistancesSquared[i] = chunkViewDistances[i] * chunkViewDistances[i];
            if (highestChunkViewDistance < chunkViewDistances[i])
            {
                highestChunkViewDistanceSquared = chunkViewDistancesSquared[i];
                highestChunkViewDistance = chunkViewDistances[i];
            }
        }
    }

    private void OnEnable()
    {
        WorldGenerator.instance.AddChunkPartitioner(this);
    }

    private void Update()
    {
        int xIndex = Mathf.RoundToInt(transform.position.x / chunkVisualSize - 0.5f);
        int yIndex = Mathf.RoundToInt(transform.position.z / chunkVisualSize - 0.5f);
        currentChunkPoint.value = new ChunkPoint(xIndex, yIndex);
    }

    private void OnDisable()
    {
        WorldGenerator.instance.RemoveChunkPartitioner(this);
    }
}