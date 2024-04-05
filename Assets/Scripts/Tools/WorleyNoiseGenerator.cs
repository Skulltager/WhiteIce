
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class WorleyNoiseGenerator : MonoBehaviour
{ 
//{
//    private const int POSITIONS_THREADS = 8;
//    private const int NOISE_THREADS = 8;

//    private const string FILE_PATH = "Assets/Textures/3dTest.asset";
//    [SerializeField] private ComputeShader worleyNoiseComputeShader = default;

//    [SerializeField] private int textureSize = default;
//    [SerializeField] private WorleyNoiseLayer[] redPositionLayers = default;
//    [SerializeField] private WorleyNoiseLayer[] greenPositionLayers = default;
//    [SerializeField] private WorleyNoiseLayer[] bluePositionLayers = default;
//    [SerializeField] private WorleyNoiseLayer[] alphaPositionLayers = default;

//    [SerializeField] private int seed = default;

//    private int generateWorleyPositionsKernel = default;
//    private int generateWorleyNoiseKernel = default;

//    private readonly EventVariable<WorleyNoiseGenerator, RenderTexture> worleyPositionsTexture;

//    private WorleyNoiseGenerator()
//    {
//        worleyPositionsTexture = new EventVariable<WorleyNoiseGenerator, RenderTexture>(t)
//    }

//    private void Awake()
//    {
        
//    }

//    [ContextMenu("Generate")]
//    private void Generate()
//    {
//        generateWorleyPositionsKernel = worleyNoiseComputeShader.FindKernel("GenerateWorleyPositions");
//        generateWorleyNoiseKernel = worleyNoiseComputeShader.FindKernel("GenerateWorleyNoise");

//        ComputeBuffer pixelDataBuffer = new ComputeBuffer(textureSize * textureSize * textureSize, Marshal.SizeOf<uint>());
//        uint[] pixelData = new uint[textureSize * textureSize * textureSize];
//        pixelDataBuffer.SetData(pixelData);

//        System.Random random = new System.Random(seed);
//        worleyNoiseComputeShader.SetInt("textureSize", textureSize);
//        worleyNoiseComputeShader.SetFloat("maximumDistance", Mathf.Sqrt(2));

//        int largestPointSize = 0;
//        for (int i = 0; i < redPositionLayers.Length; i++)
//            largestPointSize = Mathf.Max(redPositionLayers[i].positionsSize, largestPointSize);

//        for (int i = 0; i < greenPositionLayers.Length; i++)
//            largestPointSize = Mathf.Max(greenPositionLayers[i].positionsSize, largestPointSize);

//        for (int i = 0; i < bluePositionLayers.Length; i++)
//            largestPointSize = Mathf.Max(bluePositionLayers[i].positionsSize, largestPointSize);

//        for (int i = 0; i < alphaPositionLayers.Length; i++)
//            largestPointSize = Mathf.Max(alphaPositionLayers[i].positionsSize, largestPointSize);

//        RenderTexture worleyPositionsTexture = new RenderTexture(largestPointSize, largestPointSize, 0);
//        worleyPositionsTexture.volumeDepth = largestPointSize;
//        worleyPositionsTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
//        worleyPositionsTexture.enableRandomWrite = true;
//        worleyPositionsTexture.Create();

//        worleyNoiseComputeShader.SetTexture(generateWorleyPositionsKernel, "randomPositions", worleyPositionsTexture);

//        worleyNoiseComputeShader.SetBuffer(generateWorleyNoiseKernel, "worleyTextureData", pixelDataBuffer);
//        worleyNoiseComputeShader.SetTexture(generateWorleyNoiseKernel, "randomPositions", worleyPositionsTexture);

//        pixelDataBuffer.GetData(pixelData);
        
//        Texture3D tex = new Texture3D(textureSize, textureSize, textureSize, TextureFormat.RGBA32, false);

//        tex.filterMode = FilterMode.Trilinear;
//        tex.wrapMode = TextureWrapMode.Repeat;
//        tex.SetPixelData(pixelData, 0, 0);
//        tex.Apply();

//        AssetDatabase.DeleteAsset(FILE_PATH);
//        AssetDatabase.CreateAsset(tex, FILE_PATH);
//        AssetDatabase.ImportAsset(FILE_PATH);

//        worleyPositionsTexture.Release();
//        pixelDataBuffer.Release();
//    }

//    public void Save()
//    {

//    }

//    public void GenerateNewTexture()
//    {
//        if (worleyPositionsTexture != null)
//            worleyPositionsTexture.Release();

//        int largestPointSize = 0;
//        for (int i = 0; i < redPositionLayers.Length; i++)
//            largestPointSize = Mathf.Max(redPositionLayers[i].positionsSize, largestPointSize);

//        for (int i = 0; i < greenPositionLayers.Length; i++)
//            largestPointSize = Mathf.Max(greenPositionLayers[i].positionsSize, largestPointSize);

//        for (int i = 0; i < bluePositionLayers.Length; i++)
//            largestPointSize = Mathf.Max(bluePositionLayers[i].positionsSize, largestPointSize);

//        for (int i = 0; i < alphaPositionLayers.Length; i++)
//            largestPointSize = Mathf.Max(alphaPositionLayers[i].positionsSize, largestPointSize);

//        worleyPositionsTexture = new RenderTexture(largestPointSize, largestPointSize, 0);
        
//        GenerateWorleyChannel(ColorChannel.Red);
//        GenerateWorleyChannel(ColorChannel.Green);
//        GenerateWorleyChannel(ColorChannel.Blue);
//        GenerateWorleyChannel(ColorChannel.Alpha);
//    }

//    public void GenerateWorleyChannel(ColorChannel colorChannel)
//    {
//        switch (colorChannel)
//        {
//            case ColorChannel.Red:
//                GenerateWorleyChannel(0, redPositionLayers);
//                break;
//            case ColorChannel.Green:
//                GenerateWorleyChannel(1, greenPositionLayers);
//                break;
//            case ColorChannel.Blue:
//                GenerateWorleyChannel(2, bluePositionLayers);
//                break;
//            case ColorChannel.Alpha:
//                GenerateWorleyChannel(3, alphaPositionLayers);
//                break;
//        }
//    }

//    private void GenerateWorleyChannel(int channel, WorleyNoiseLayer[] positionLayers)
//    {
//        float totalWeight = 0;
//        for (int i = 0; i < positionLayers.Length; i++)
//            totalWeight += positionLayers[i].weight;

//        for (int i = 0; i < positionLayers.Length; i++)
//        {
//            WorleyNoiseLayer positionLayer = positionLayers[i];
//            int threads = Mathf.CeilToInt((float)positionLayer.positionsSize / POSITIONS_THREADS);

//            System.Random random = new System.Random(positionLayer.seed);
//            worleyNoiseComputeShader.SetInt("channel", channel);
//            worleyNoiseComputeShader.SetInt("seedX", random.Next());
//            worleyNoiseComputeShader.SetInt("seedY", random.Next());
//            worleyNoiseComputeShader.SetInt("seedZ", random.Next());
//            worleyNoiseComputeShader.SetInt("positionsSize", positionLayer.positionsSize);
//            worleyNoiseComputeShader.SetFloat("pixelsPerPosition", ((float)textureSize / positionLayer.positionsSize));
//            worleyNoiseComputeShader.SetFloat("weight", positionLayer.weight / totalWeight);

//            worleyNoiseComputeShader.Dispatch(generateWorleyPositionsKernel, threads, threads, threads);

//            threads = Mathf.CeilToInt((float)textureSize / NOISE_THREADS);
//            worleyNoiseComputeShader.Dispatch(generateWorleyNoiseKernel, threads, threads, threads);
//        }
//    }
}