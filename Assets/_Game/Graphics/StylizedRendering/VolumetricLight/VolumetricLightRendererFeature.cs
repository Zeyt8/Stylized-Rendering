using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class VolumetricRenderData : ContextItem
{
    public TextureHandle CopyColorHandle;
    public TextureHandle OcclusionTextureHandle;
    public TextureHandle ScatteringTextureHandle;

    public override void Reset()
    {
        CopyColorHandle = TextureHandle.nullHandle;
        OcclusionTextureHandle = TextureHandle.nullHandle;
        ScatteringTextureHandle = TextureHandle.nullHandle;
    }
}

public class VolumetricLightRendererFeature : ScriptableRendererFeature
{
    [SerializeField, Range(0.0f, 1.0f)]
    private float _intensity = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)]
    private float _blurWidth = 0.85f;
    [SerializeField] private Material _occludersMaterial;
    [SerializeField] private Material _scatteringMaterial;
    [SerializeField] private Material _combineMaterial;

    [SerializeField] private FullscreenSettings _settings = new();

    private VolumetricPass_CopyColor _volumetricPass_CopyColor;
    private VolumetricPass_Occlusion _volumetricPass_Occlusion;
    private VolumetricPass_Scattering _volumetricPass_Scattering;
    private VolumetricPass_Final _volumetricPass_Final;

    public override void Create()
    {
        _volumetricPass_CopyColor = new VolumetricPass_CopyColor(_settings);
        _volumetricPass_Occlusion = new VolumetricPass_Occlusion(_settings, _occludersMaterial);
        _volumetricPass_Scattering = new VolumetricPass_Scattering(_settings, _scatteringMaterial, _intensity, _blurWidth);
        _volumetricPass_Final = new VolumetricPass_Final(_settings, _combineMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_volumetricPass_CopyColor);
        renderer.EnqueuePass(_volumetricPass_Occlusion);
        renderer.EnqueuePass(_volumetricPass_Scattering);
        renderer.EnqueuePass(_volumetricPass_Final);
    }
}
