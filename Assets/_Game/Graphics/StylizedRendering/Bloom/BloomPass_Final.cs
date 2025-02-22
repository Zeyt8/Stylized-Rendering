using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BloomPass_Final : ScriptableRenderPass
{
    private readonly FullscreenSettings _settings;
    private readonly Material _blurMaterial;

    private float _threshold;
    private float _intensity;
    private float _crosshatchThickness;
    private float _crosshatchSpacing;
    private float _crosshatchFade;
    private float _rotationSpeed;

    public BloomPass_Final(FullscreenSettings settings, Material blurMaterial, float threshold, float intensity, float crosshatchThickness, float crosshatchSpacing, float crosshatchFade, float rotationSpeed)
    {
        renderPassEvent = settings.RenderPassEvent;
        _settings = settings;
        _blurMaterial = blurMaterial;

        _threshold = threshold;
        _intensity = intensity;
        _crosshatchThickness = crosshatchThickness;
        _crosshatchSpacing = crosshatchSpacing;
        _crosshatchFade = crosshatchFade;
        _rotationSpeed = rotationSpeed;
    }

    private static void ExecutePass(PassData passData, RasterGraphContext context)
    {
        passData.Material.SetTexture("_FullscreenColor", passData.Color);
        passData.Material.SetTexture("_BlurredColor", passData.Blur);
        passData.Material.SetFloat("_Threshold", passData.Threshold);
        passData.Material.SetFloat("_Intensity", passData.Intensity);
        passData.Material.SetFloat("_CrosshatchThickness", passData.CrosshatchThickness);
        passData.Material.SetFloat("_CrosshatchSpacing", passData.CrosshatchSpacing);
        passData.Material.SetFloat("_CrosshatchFade", passData.CrosshatchFade);
        passData.Material.SetFloat("_RotationSpeed", passData.RotationSpeed);

        Blitter.BlitTexture(context.cmd, passData.Color, new Vector4(1, 1, 0, 0), passData.Material, 0);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.Get<BloomRenderData>();

        using var builder = renderGraph.AddRasterRenderPass<PassData>("BloomPass Final", out var passData);

        if (!fullscreenData.ColorCopyTextureHandle.IsValid() || !fullscreenData.BlurredTextureHandle.IsValid())
        {
            Debug.Log("Blur texture is invalid. Canceling Blur feature.");
            return;
        }

        passData.Material = _blurMaterial;
        passData.Color = fullscreenData.ColorCopyTextureHandle;
        passData.Blur = fullscreenData.BlurredTextureHandle;
        passData.Threshold = _threshold;
        passData.Intensity = _intensity;
        passData.CrosshatchThickness = _crosshatchThickness;
        passData.CrosshatchSpacing = _crosshatchSpacing;
        passData.CrosshatchFade = _crosshatchFade;
        passData.RotationSpeed = _rotationSpeed;

        builder.UseTexture(fullscreenData.ColorCopyTextureHandle);
        builder.UseTexture(fullscreenData.HorizontalBlurHandle);
        builder.UseTexture(fullscreenData.BlurredTextureHandle);

        builder.SetRenderAttachment(resourceData.cameraColor, index: 0);

        builder.SetRenderFunc((PassData passData, RasterGraphContext context) =>
        {
            ExecutePass(passData, context);
        });
    }

    internal class PassData
    {
        internal TextureHandle Color;
        internal TextureHandle Blur;
        internal Material Material;

        internal float Threshold;
        internal float Intensity;
        internal float CrosshatchThickness;
        internal float CrosshatchSpacing;
        internal float CrosshatchFade;
        internal float RotationSpeed;
    }
}
