
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode, ImageEffectAllowedInSceneView]
[AddComponentMenu("Image Effects/Rendering/Volumetric Fog")]
public class VolumetricFog : MonoBehaviour
{
    private const int POSITIONS_THREADS = 8;
    private const int NOISE_THREADS = 8;

    [SerializeField] private ComputeShader worleyNoiseComputeShader = default;

    [SerializeField] private int textureSize = default;
    [SerializeField] private WorleyNoiseLayer[] redPositionLayers = default;
    [SerializeField] private WorleyNoiseLayer[] greenPositionLayers = default;
    [SerializeField] private WorleyNoiseLayer[] bluePositionLayers = default;
    [SerializeField] private WorleyNoiseLayer[] alphaPositionLayers = default;

    [SerializeField] private Transform bounds = default;
    [SerializeField] private float sampleStepSize = default;
    
    [SerializeField] private Vector4 sampleScaleX = default;
    [SerializeField] private Vector4 sampleScaleY = default;
    [SerializeField] private Vector4 sampleScaleZ = default;

    [SerializeField] private Vector4 sampleThreshold = default;
    [SerializeField] private Vector4 sampleEdgeVerticalCutoff = default;
    [SerializeField] private Vector4 sampleEdgeHorizontalCutoff = default; 
    [SerializeField] private Vector4 sampleMultiplier = default;
    [SerializeField] private Vector4 sampleWeights = default;

    [SerializeField] private float maxFogDensity = default;

    private Material volumetricFogMaterial;
    private Material worleyNoiseDisplayMaterial;

    private int generateWorleyPositionsKernel = default;
    private int generateWorleyNoiseKernel = default;

    private RenderTexture worleyNoiseTexture;

    private void Check()
    {
        if (volumetricFogMaterial != null)
            return;

        Shader volumetricFogShader = Shader.Find("Hidden/VolumetricFog");
        volumetricFogMaterial = new Material(volumetricFogShader);

        Shader worleyNoiseDisplayShader = Shader.Find("Hidden/WorleyNoiseDisplay");
        worleyNoiseDisplayMaterial = new Material(worleyNoiseDisplayShader);

        SetMaterialValues();
        GenerateAllChannels();
    }

    [ContextMenu("Generate")]
    private void GenerateAllChannels()
    {
        if (worleyNoiseTexture != null)
            worleyNoiseTexture.Release();

        generateWorleyPositionsKernel = worleyNoiseComputeShader.FindKernel("GenerateWorleyPositions");
        generateWorleyNoiseKernel = worleyNoiseComputeShader.FindKernel("GenerateWorleyNoise");

        worleyNoiseComputeShader.SetInt("textureSize", textureSize);
        worleyNoiseComputeShader.SetFloat("maximumDistance", Mathf.Sqrt(2));

        int largestPointSize = 0;
        for (int i = 0; i < redPositionLayers.Length; i++)
            largestPointSize = Mathf.Max(redPositionLayers[i].positionsSize, largestPointSize);

        for (int i = 0; i < greenPositionLayers.Length; i++)
            largestPointSize = Mathf.Max(greenPositionLayers[i].positionsSize, largestPointSize);

        for (int i = 0; i < bluePositionLayers.Length; i++)
            largestPointSize = Mathf.Max(bluePositionLayers[i].positionsSize, largestPointSize);

        for (int i = 0; i < alphaPositionLayers.Length; i++)
            largestPointSize = Mathf.Max(alphaPositionLayers[i].positionsSize, largestPointSize);

        RenderTexture worleyPositionsTexture = new RenderTexture(largestPointSize, largestPointSize, 0);
        worleyPositionsTexture.volumeDepth = largestPointSize;
        worleyPositionsTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        worleyPositionsTexture.enableRandomWrite = true;
        worleyPositionsTexture.Create();

        worleyNoiseTexture = new RenderTexture(textureSize, textureSize, 0);
        worleyPositionsTexture.volumeDepth = textureSize;
        worleyPositionsTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        worleyPositionsTexture.filterMode = FilterMode.Bilinear;
        worleyPositionsTexture.wrapMode = TextureWrapMode.Repeat;
        worleyPositionsTexture.enableRandomWrite = true;
        worleyPositionsTexture.Create();

        worleyNoiseComputeShader.SetTexture(generateWorleyPositionsKernel, "randomPositions", worleyPositionsTexture);
        worleyNoiseComputeShader.SetTexture(generateWorleyNoiseKernel, "randomPositions", worleyPositionsTexture);

        worleyNoiseComputeShader.SetTexture(generateWorleyNoiseKernel, "worleyNoise", worleyNoiseTexture);

        GenerateWorleyChannel(0, redPositionLayers);
        GenerateWorleyChannel(1, greenPositionLayers);
        GenerateWorleyChannel(2, bluePositionLayers);
        GenerateWorleyChannel(3, alphaPositionLayers);

        worleyPositionsTexture.Release();

        volumetricFogMaterial.SetTexture("worleyNoise", worleyNoiseTexture);
    }

    private void GenerateWorleyChannel(int channel, WorleyNoiseLayer[] positionLayers)
    {
        float totalWeight = 0;
        for (int i = 0; i < positionLayers.Length; i++)
            totalWeight += positionLayers[i].weight;

        for (int i = 0; i < positionLayers.Length; i++)
        {
            WorleyNoiseLayer positionLayer = positionLayers[i];

            System.Random random = new System.Random(positionLayer.seed);
            worleyNoiseComputeShader.SetInt("channel", channel);
            worleyNoiseComputeShader.SetInt("seedX", random.Next());
            worleyNoiseComputeShader.SetInt("seedY", random.Next());
            worleyNoiseComputeShader.SetInt("seedZ", random.Next());
            worleyNoiseComputeShader.SetInt("positionsSize", positionLayer.positionsSize);
            worleyNoiseComputeShader.SetFloat("pixelsPerPosition", ((float)textureSize / positionLayer.positionsSize));
            worleyNoiseComputeShader.SetFloat("weight", positionLayer.weight / totalWeight);

            int threads = Mathf.CeilToInt((float)positionLayer.positionsSize / POSITIONS_THREADS);
            worleyNoiseComputeShader.Dispatch(generateWorleyPositionsKernel, threads, threads, threads);

            threads = Mathf.CeilToInt((float)textureSize / NOISE_THREADS);
            worleyNoiseComputeShader.Dispatch(generateWorleyNoiseKernel, threads, threads, threads);
        }
    }

    public void RegenerateChannel(ColorChannel colorChannel)
    {
        switch (colorChannel)
        {
            case ColorChannel.Red:
                RegenerateChannel(0, redPositionLayers);
                break;
            case ColorChannel.Green:
                RegenerateChannel(1, redPositionLayers);
                break;
            case ColorChannel.Blue:
                RegenerateChannel(2, redPositionLayers);
                break;
            case ColorChannel.Alpha:
                RegenerateChannel(3, redPositionLayers);
                break;
        }
    }

    private void RegenerateChannel(int channel, WorleyNoiseLayer[] positionLayers)
    {
        int largestPointSize = 0;

        for (int i = 0; i < positionLayers.Length; i++)
            largestPointSize = Mathf.Max(positionLayers[i].positionsSize, largestPointSize);

        RenderTexture worleyPositionsTexture = new RenderTexture(largestPointSize, largestPointSize, 0);
        worleyPositionsTexture.volumeDepth = largestPointSize;
        worleyPositionsTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        worleyPositionsTexture.enableRandomWrite = true;
        worleyPositionsTexture.Create();

        worleyNoiseComputeShader.SetTexture(generateWorleyPositionsKernel, "randomPositions", worleyPositionsTexture);
        worleyNoiseComputeShader.SetTexture(generateWorleyNoiseKernel, "randomPositions", worleyPositionsTexture);

        GenerateWorleyChannel(channel, positionLayers);

        worleyPositionsTexture.Release();
    }

    public void SetMaterialValues()
    {
        volumetricFogMaterial.SetFloat("sampleStepSize", sampleStepSize);

        volumetricFogMaterial.SetVector("sampleScaleX", sampleScaleX);
        volumetricFogMaterial.SetVector("sampleScaleY", sampleScaleY);
        volumetricFogMaterial.SetVector("sampleScaleZ", sampleScaleZ);
        volumetricFogMaterial.SetVector("sampleThreshold", sampleThreshold);
        volumetricFogMaterial.SetVector("sampleEdgeVerticalCutoff", sampleEdgeVerticalCutoff);
        volumetricFogMaterial.SetVector("sampleEdgeHorizontalCutoff", sampleEdgeHorizontalCutoff);
        volumetricFogMaterial.SetVector("sampleMultiplier", sampleMultiplier);

        volumetricFogMaterial.SetFloat("maxFogDensity", maxFogDensity);
    }

    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Check();

        Vector3 bottomLeftBounds = bounds.transform.position - bounds.transform.lossyScale / 2;
        Vector3 topRightBounds = bounds.transform.position + bounds.transform.lossyScale / 2;

        volumetricFogMaterial.SetVector("boundsMin", bottomLeftBounds);
        volumetricFogMaterial.SetVector("boundsMax", topRightBounds);

        Graphics.Blit(source, destination, volumetricFogMaterial);
    }
}