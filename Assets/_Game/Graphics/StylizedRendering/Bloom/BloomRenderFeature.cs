using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BloomRenderData : ContextItem
{
    public TextureHandle ColorCopyTextureHandle;
    public TextureHandle HorizontalBlurHandle;
    public TextureHandle BlurredTextureHandle;

    public override void Reset()
    {
        ColorCopyTextureHandle = TextureHandle.nullHandle;
        HorizontalBlurHandle = TextureHandle.nullHandle;
        BlurredTextureHandle = TextureHandle.nullHandle;
    }
}

public class BloomRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader _blurShader;
    [SerializeField] private Material _fullscreenMaterial;
    [Header("Bloom")]
    [SerializeField] private float _threshold;
    [SerializeField] private float _intensity;
    [SerializeField, Range(0, 0.5f)] private float _crosshatchThickness;
    [SerializeField] private float _crosshatchSpacing;
    [SerializeField, Range(0, 0.5f)] private float _crosshatchFade;
    [SerializeField] private float _rotationSpeed;
    [Header("Blur")]
    [SerializeField] private int _blurSize;
    [SerializeField] private float _blurSmoothness;
    [Header("Settings")]
    public FullscreenSettings Settings = new();

    private BloomPass_CopyColor _bloomPass_CopyColor;
    private BloomPass_Render _bloomPass_Render_H;
    private BloomPass_Render _bloomPass_Render_V;
    private BloomPass_Final _bloomPass_Final;

    public override void Create()
    {
        int kernelSize = 2 * _blurSize + 1;
        float sigma = kernelSize / (6 * Mathf.Max(0.001f, _blurSmoothness));
        _bloomPass_CopyColor = new BloomPass_CopyColor(Settings);
        _bloomPass_Render_H = new BloomPass_Render(Settings, new Material(_blurShader), _blurSize, sigma, 0);
        _bloomPass_Render_V = new BloomPass_Render(Settings, new Material(_blurShader), _blurSize, sigma, 1);
        _bloomPass_Final = new BloomPass_Final(Settings, _fullscreenMaterial, _threshold, _intensity, _crosshatchThickness, _crosshatchSpacing, _crosshatchFade, _rotationSpeed);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_bloomPass_CopyColor);
        renderer.EnqueuePass(_bloomPass_Render_H);
        renderer.EnqueuePass(_bloomPass_Render_V);
        renderer.EnqueuePass(_bloomPass_Final);
    }
}
