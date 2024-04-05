using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Rendering/Global Fog")]
public class GlobalFog : MonoBehaviour
{
    [SerializeField] private Cubemap skyBox = default;
    [SerializeField] private float fogStartDistance = default;
    [SerializeField] private float fogEndDistance = default;
    [SerializeField] private float fogExponentFactor = default;

    private Camera fogCamera;
    private Shader globalFogShader;
    private Material globalFogMaterial;
    
    private void Check()
    {
        if (globalFogShader != null)
            return;

        fogCamera = GetComponent<Camera>();
        globalFogShader = Shader.Find("Hidden/WorldFog");
        globalFogMaterial = new Material(globalFogShader);
        globalFogMaterial.SetTexture("_SkyCubemap", skyBox);
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Check();

        globalFogMaterial.SetFloat("fogStartDistance", fogStartDistance);
        globalFogMaterial.SetFloat("fogTotalDistance", fogEndDistance - fogStartDistance);
        globalFogMaterial.SetFloat("fogExponentFactor", fogExponentFactor);

        Graphics.Blit(source, destination, globalFogMaterial);
    }
}
