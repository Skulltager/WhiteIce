using System;
using UnityEngine;

[Serializable]
public class BiomeSettings
{
    [field: SerializeField] public Color color { private set; get; } = default;
    [field: SerializeField] public float biomeBlendStrength { private set; get; } = default;
    [field: SerializeField] public float biomeClampStrength { private set; get; } = default;
    [field: SerializeField] public float biomeScaleStrength { private set; get; } = default;
    [field: SerializeField] public float biomeElevationStrength { private set; get; } = default;
    [field: SerializeField] public float heightClampStrength { private set; get; } = default;
    [field: SerializeField] public float minimumHeight { private set; get; } = default;
    [field: SerializeField] public float maximumHeight { private set; get; } = default; 
    [field: SerializeField] public LayerSettings[] biomeLayers { private set; get; } = default;
    [field: SerializeField] public LayerSettings[] heightLayers { private set; get; } = default;
    [field: SerializeField] public uint textureIndex { private set; get; } = default;
}