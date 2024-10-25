using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class VolumetricPass_Final : ScriptableRenderPass
{
    private readonly FullscreenSettings _settings;
    private readonly Material _combineMaterial;

    public VolumetricPass_Final(FullscreenSettings settings, Material combineMaterial)
    {
        renderPassEvent = settings.RenderPassEvent;
        _settings = settings;
        _combineMaterial = combineMaterial;
    }

    private static void ExecutePass(PassData passData, RasterGraphContext context)
    {
        passData.Material.SetTexture("_OcclusionMap", passData.Occlusion);
        passData.Material.SetTexture("_ScatterMap", passData.Scatter);
        passData.Material.SetTexture("_MainTex", passData.Color);

        Blitter.BlitTexture(context.cmd, passData.Color, new Vector4(1, 1, 0, 0), passData.Material, 0);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.Get<VolumetricRenderData>();

        using var builder = renderGraph.AddRasterRenderPass<PassData>("VolumetricPass Final", out var passData);

        if (!fullscreenData.OcclusionTextureHandle.IsValid())
        {
            Debug.Log("Volumetric texture is invalid. Canceling Volumetric feature.");
            return;
        }

        passData.Material = _combineMaterial;
        passData.Occlusion = fullscreenData.OcclusionTextureHandle;
        passData.Color = fullscreenData.CopyColorHandle;
        passData.Scatter = fullscreenData.ScatteringTextureHandle;

        builder.UseTexture(fullscreenData.OcclusionTextureHandle);
        builder.UseTexture(fullscreenData.ScatteringTextureHandle);
        builder.UseTexture(fullscreenData.CopyColorHandle);

        builder.SetRenderAttachment(resourceData.cameraColor, index: 0);

        builder.SetRenderFunc((PassData passData, RasterGraphContext context) =>
        {
            ExecutePass(passData, context);
        });
    }

    internal class PassData
    {
        internal TextureHandle Color;
        internal TextureHandle Occlusion;
        internal TextureHandle Scatter;
        internal Material Material;
        internal Color LightColor;
    }
}
