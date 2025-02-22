using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlinePass_Final : ScriptableRenderPass
{
    private readonly FullscreenSettings _settings;

    private readonly Material _blitMaterial;

    private float _robertsCrossMultiplier;
    private float _noiseStrength;
    private float _noiseScale;
    private Color _outlineColor;
    private float _depthOutlineScale;
    private float _depthThreshold;
    private float _steepAngleThreshold;
    private float _steepAngleMultiplier;
    private float _normalOutlineScale;
    private float _normalThreshold;
    private float _colorOutlineScale;
    private float _colorThresholdMin;
    private float _colorThresholdMax;
    private float _dotsDensity;
    private float _dotsSize;

    public OutlinePass_Final(FullscreenSettings settings, Material blitMaterial,
        float robertsCrossMultiplier, float noiseStrength, float noiseScale, Color outlineColor,
        float depthOutlineScale, float depthThreshold, float steepAngleThreshold, float steepAngleMultiplier,
        float normalOutlineScale, float normalThreshold,
        float colorOutlineScale, float colorThresholdMin, float colorThresholdMax, float dotsDensity, float dotsSize)
    {
        renderPassEvent = settings.RenderPassEvent;
        _settings = settings;
        _blitMaterial = blitMaterial;

        _robertsCrossMultiplier = robertsCrossMultiplier;
        _noiseStrength = noiseStrength;
        _noiseScale = noiseScale;
        _outlineColor = outlineColor;
        _depthOutlineScale = depthOutlineScale;
        _depthThreshold = depthThreshold;
        _steepAngleThreshold = steepAngleThreshold;
        _steepAngleMultiplier = steepAngleMultiplier;
        _normalOutlineScale = normalOutlineScale;
        _normalThreshold = normalThreshold;
        _colorOutlineScale = colorOutlineScale;
        _colorThresholdMin = colorThresholdMin;
        _colorThresholdMax = colorThresholdMax;
        _dotsDensity = dotsDensity;
        _dotsSize = dotsSize;
    }

    private static void ExecutePass(PassData passData, RasterGraphContext context)
    {
        passData.Material.SetTexture("_FullscreenObjects", passData.Objects);
        passData.Material.SetTexture("_FullscreenColor", passData.Color);
        passData.Material.SetFloat("_RobertsCrossMultiplier", passData.RobertsCrossMultiplier);
        passData.Material.SetFloat("_NoiseStrength", passData.NoiseStrength);
        passData.Material.SetFloat("_NoiseScale", passData.NoiseScale);
        passData.Material.SetColor("_OutlineColor", passData.OutlineColor);
        passData.Material.SetFloat("_DepthOutlineScale", passData.DepthOutlineScale);
        passData.Material.SetFloat("_DepthThreshold", passData.DepthThreshold);
        passData.Material.SetFloat("_SteepAngleThreshold", passData.SteepAngleThreshold);
        passData.Material.SetFloat("_SteepAngleMultiplier", passData.SteepAngleMultiplier);
        passData.Material.SetFloat("_NormalOutlineScale", passData.NormalOutlineScale);
        passData.Material.SetFloat("_NormalThreshold", passData.NormalThreshold);
        passData.Material.SetFloat("_ColorOutlineScale", passData.ColorOutlineScale);
        passData.Material.SetFloat("_ColorThresholdMin", passData.ColorThresholdMin);
        passData.Material.SetFloat("_ColorThresholdMax", passData.ColorThresholdMax);
        passData.Material.SetFloat("_DotsDensity", passData.DotsDensity);
        passData.Material.SetFloat("_DotsSize", passData.DotsSize);
        Blitter.BlitTexture(context.cmd, passData.Objects, new Vector4(1, 1, 0, 0), passData.Material, 0);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.Get<OutlineRenderData>();
        var cameraData = frameData.Get<UniversalCameraData>();
        using var builder = renderGraph.AddRasterRenderPass<PassData>("OutlinePass Final", out var passData);

        if (!fullscreenData.ColorCopyTextureHandle.IsValid() || !fullscreenData.ObjectTextureHandle.IsValid())
        {
            Debug.Log("Either the color copy or object texture was invalid. Canceling Fullscreen feature");
            return;
        }

        passData.Material = _blitMaterial;
        passData.Objects = fullscreenData.ObjectTextureHandle;
        passData.Color = fullscreenData.ColorCopyTextureHandle;
        passData.RobertsCrossMultiplier = _robertsCrossMultiplier;
        passData.NoiseStrength = _noiseStrength;
        passData.NoiseScale = _noiseScale;
        passData.OutlineColor = _outlineColor;
        passData.DepthOutlineScale = _depthOutlineScale;
        passData.DepthThreshold = _depthThreshold;
        passData.SteepAngleThreshold = _steepAngleThreshold;
        passData.SteepAngleMultiplier = _steepAngleMultiplier;
        passData.NormalOutlineScale = _normalOutlineScale;
        passData.NormalThreshold = _normalThreshold;
        passData.ColorOutlineScale = _colorOutlineScale;
        passData.ColorThresholdMin = _colorThresholdMin;
        passData.ColorThresholdMax = _colorThresholdMax;
        passData.DotsDensity = _dotsDensity;
        passData.DotsSize = _dotsSize;

        builder.UseTexture(fullscreenData.ObjectTextureHandle);
        builder.UseTexture(fullscreenData.ColorCopyTextureHandle);

        builder.SetRenderAttachment(resourceData.cameraColor, index: 0);

        builder.SetRenderFunc((PassData passData, RasterGraphContext context) =>
        {
            ExecutePass(passData, context);
        });
    }

    internal class PassData
    {
        internal TextureHandle Objects;
        internal TextureHandle Color;
        internal Material Material;

        internal float RobertsCrossMultiplier;
        internal float NoiseStrength;
        internal float NoiseScale;
        internal Color OutlineColor;
        internal float DepthOutlineScale;
        internal float DepthThreshold;
        internal float SteepAngleThreshold;
        internal float SteepAngleMultiplier;
        internal float NormalOutlineScale;
        internal float NormalThreshold;
        internal float ColorOutlineScale;
        internal float ColorThresholdMin;
        internal float ColorThresholdMax;
        internal float DotsDensity;
        internal float DotsSize;
    }
}