using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BloomPass_Final : ScriptableRenderPass
{
    private readonly FullscreenSettings _settings;
    private readonly Material _blurMaterial;

    public BloomPass_Final(FullscreenSettings settings, Material blurMaterial)
    {
        renderPassEvent = settings.RenderPassEvent;
        _settings = settings;
        _blurMaterial = blurMaterial;
    }

    private static void ExecutePass(PassData passData, RasterGraphContext context)
    {
        passData.Material.SetTexture("_FullscreenColor", passData.Color);
        passData.Material.SetTexture("_BlurredColor", passData.Blur);

        Blitter.BlitTexture(context.cmd, passData.Color, new Vector4(1, 1, 0, 0), passData.Material, 0);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.Get<BloomRenderData>();

        using var builder = renderGraph.AddRasterRenderPass<PassData>("BloomPass Final", out var passData);

        if (!fullscreenData.ColorCopyTextureHandle.IsValid() || !fullscreenData.BlurredTextureHandle[^1].IsValid())
        {
            Debug.Log("Blur texture is invalid. Canceling Blur feature.");
            return;
        }

        passData.Material = _blurMaterial;
        passData.Color = fullscreenData.ColorCopyTextureHandle;
        passData.Blur = fullscreenData.BlurredTextureHandle[^1];

        builder.UseTexture(fullscreenData.ColorCopyTextureHandle);
        for (int i = 0; i < fullscreenData.BlurredTextureHandle.Length; i++)
        {
            builder.UseTexture(fullscreenData.BlurredTextureHandle[i]);
        }

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
    }
}
