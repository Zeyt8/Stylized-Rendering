using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlineRenderData : ContextItem
{
    public TextureHandle ColorCopyTextureHandle;
    public TextureHandle ObjectTextureHandle;

    public override void Reset()
    {
        ColorCopyTextureHandle = TextureHandle.nullHandle;
        ObjectTextureHandle = TextureHandle.nullHandle;
    }
}

public class OutlineRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Material overrideMaterial;
    [SerializeField] private Material fullscreenMaterial;
    [Header("Outline")]
    [SerializeField] private float _robertsCrossMultiplier;
    [SerializeField, Range(0, 0.005f)] private float _noiseStrength;
    [SerializeField, Range(0, 100)] private float _noiseScale;
    // Outline
    [Space]
    [SerializeField] private Color _outlineColor;
    // Depth
    [Space]
    [SerializeField, Range(0, 3)] private float _depthOutlineScale;
    [SerializeField, Range(0, 2)] private float _depthThreshold;
    [SerializeField] private float _steepAngleThreshold;
    [SerializeField] private float _steepAngleMultiplier;
    // Normal
    [Space]
    [SerializeField, Range(0, 3)] private float _normalOutlineScale;
    [SerializeField, Range(0, 2)] private float _normalThreshold;
    // Color
    [Space]
    [SerializeField, Range(0, 3)] private float _colorOutlineScale;
    [SerializeField, Range(0, 2)] private float _colorThresholdMin;
    [SerializeField, Range(0, 2)] private float _colorThresholdMax;
    [SerializeField] private float _dotsDensity;
    [SerializeField, Range(0, 1)] private float _dotsSize;
    [Header("Settings")]
    [SerializeField] private FullscreenSettings Settings = new();

    private OutlinePass_CopyColor _outlinePass_CopyColor;
    private OutlinePass_Render _outlinePass_Render;
    private OutlinePass_Final _outlinePass_Final;

    public override void Create()
    {
        _outlinePass_CopyColor = new OutlinePass_CopyColor(Settings);
        _outlinePass_Render = new OutlinePass_Render(Settings, overrideMaterial);
        _outlinePass_Final = new OutlinePass_Final(Settings, fullscreenMaterial,
            _robertsCrossMultiplier, _noiseStrength, _noiseScale, _outlineColor,
            _depthOutlineScale, _depthThreshold, _steepAngleThreshold, _steepAngleMultiplier,
            _normalOutlineScale, _normalThreshold,
            _colorOutlineScale, _colorThresholdMin, _colorThresholdMax, _dotsDensity, _dotsSize);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_outlinePass_CopyColor);
        renderer.EnqueuePass(_outlinePass_Render);
        renderer.EnqueuePass(_outlinePass_Final);
    }
}
