using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BloomRenderData : ContextItem
{
    public TextureHandle ColorCopyTextureHandle;
    public TextureHandle[] BlurredTextureHandle = new TextureHandle[20];

    public override void Reset()
    {
        ColorCopyTextureHandle = TextureHandle.nullHandle;
        for (int i = 0; i < BlurredTextureHandle.Length; i++)
        {
            BlurredTextureHandle[i] = TextureHandle.nullHandle;
        }
    }
}

public class BloomRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Material[] _blurMaterials = new Material[2];
    [SerializeField] private Material _fullscreenMaterial;
    [SerializeField] private float _blurIntensity;

    public FullscreenSettings Settings = new();

    private BloomPass_CopyColor _bloomPass_CopyColor;
    private BloomPass_Render[] _bloomPass_Render = new BloomPass_Render[20];
    private BloomPass_Final _bloomPass_Final;

    public override void Create()
    {
        _bloomPass_CopyColor = new BloomPass_CopyColor(Settings);
        for (int i = 0; i < _bloomPass_Render.Length; i++)
        {
            _bloomPass_Render[i] = new BloomPass_Render(Settings, new Material(_blurMaterials[i % 2]), _blurIntensity, i);
        }
        _bloomPass_Final = new BloomPass_Final(Settings, _fullscreenMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_bloomPass_CopyColor);
        for (int i = 0; i < _bloomPass_Render.Length; i++)
        {
            renderer.EnqueuePass(_bloomPass_Render[i]);
        }
        renderer.EnqueuePass(_bloomPass_Final);
    }
}
