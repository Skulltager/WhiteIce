
using UnityEngine;

public class TextureDisplay : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer = default;

    private Material material;

    private void Awake()
    {
        material = meshRenderer.material;
    }

    public void Initialize(Texture texture)
    {
        material.SetTexture("_MainTex", texture);
    }

    private void OnDestroy()
    {
        Texture texture = material.GetTexture("_MainTex");
        if (texture is RenderTexture renderTexture)
            renderTexture.Release();
    }
}