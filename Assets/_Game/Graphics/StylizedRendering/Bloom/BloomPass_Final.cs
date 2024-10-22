using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BloomPass_Final : ScriptableRenderPass
{
    private readonly FullscreenSettings settings;
    private readonly Material blurMaterial;

    public BloomPass_Final(FullscreenSettings settings, Material blurMaterial)
    {
        renderPassEvent = settings.RenderPassEvent;
        this.settings = settings;
        this.blurMaterial = blurMaterial;
    }

    private static void ExecutePass(PassData passData, RasterGraphContext context)
    {
        passData.material.SetTexture("_FullscreenColor", passData.color);
        passData.material.SetTexture("_BlurredColor", passData.blur);

        Blitter.BlitTexture(context.cmd, passData.color, new Vector4(1, 1, 0, 0), passData.material, 0);
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

        passData.material = blurMaterial;
        passData.color = fullscreenData.ColorCopyTextureHandle;
        passData.blur = fullscreenData.BlurredTextureHandle;

        builder.UseTexture(fullscreenData.ColorCopyTextureHandle);
        builder.UseTexture(fullscreenData.BlurredTextureHandle);

        builder.SetRenderAttachment(resourceData.cameraColor, index: 0);

        builder.SetRenderFunc((PassData passData, RasterGraphContext context) =>
        {
            ExecutePass(passData, context);
        });
    }

    internal class PassData
    {
        internal TextureHandle color;
        internal TextureHandle blur;
        internal Material material;
    }
}
