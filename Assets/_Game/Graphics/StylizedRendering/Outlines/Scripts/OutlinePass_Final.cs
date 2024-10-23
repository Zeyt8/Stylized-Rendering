using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlinePass_Final : ScriptableRenderPass
{
    private readonly FullscreenSettings _settings;

    private readonly Material _blitMaterial;

    public OutlinePass_Final(FullscreenSettings settings, Material blitMaterial)
    {
        renderPassEvent = settings.RenderPassEvent;
        _settings = settings;
        _blitMaterial = blitMaterial;
    }

    private static void ExecutePass(PassData passData, RasterGraphContext context)
    {
        passData.Material.SetTexture("_FullscreenObjects", passData.Objects);
        passData.Material.SetTexture("_FullscreenColor", passData.Color);
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
    }
}