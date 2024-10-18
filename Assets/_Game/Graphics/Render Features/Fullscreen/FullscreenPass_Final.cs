using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class FullscreenPass_Final : ScriptableRenderPass
{
    private readonly FullscreenSettings settings;

    private readonly Material blitMaterial;

    public FullscreenPass_Final(FullscreenSettings settings, Material blitMaterial)
    {
        renderPassEvent = settings.RenderPassEvent;
        this.settings = settings;
        this.blitMaterial = blitMaterial;
    }

    private static void ExecutePass(PassData passData, RasterGraphContext context)
    {
        passData.material.SetTexture("_FullscreenObjects", passData.objects);
        passData.material.SetTexture("_FullscreenColor", passData.color);
        Blitter.BlitTexture(context.cmd, passData.objects, new Vector4(1, 1, 0, 0), passData.material, 0);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.Get<FullscreenRenderData>();
        var cameraData = frameData.Get<UniversalCameraData>();
        using var builder = renderGraph.AddRasterRenderPass<PassData>("FullscreenPass Final", out var passData);

        if (!fullscreenData.ColorCopyTextureHandle.IsValid() || !fullscreenData.ObjectTextureHandle.IsValid())
        {
            Debug.Log("Either the color copy or object texture was invalid. Canceling Fullscreen feature");
            return;
        }

        passData.material = blitMaterial;
        passData.objects = fullscreenData.ObjectTextureHandle;
        passData.color = fullscreenData.ColorCopyTextureHandle;

        builder.UseTexture(fullscreenData.ObjectTextureHandle);
        builder.UseTexture(fullscreenData.ColorCopyTextureHandle);

        builder.SetRenderAttachment(resourceData.cameraColor, index: 0);

        builder.SetRenderFunc((PassData passData, RasterGraphContext context) =>
        {
            //UpdateMaterial(passData);
            ExecutePass(passData, context);
        });
    }

    internal class PassData
    {
        internal TextureHandle objects;
        internal TextureHandle color;
        internal Material material;
    }
}