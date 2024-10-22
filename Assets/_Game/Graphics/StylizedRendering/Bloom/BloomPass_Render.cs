using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BloomPass_Render : ScriptableRenderPass
{
    private readonly Material blurMaterial;
    private readonly LayerMask layerMask;
    private readonly uint renderLayerMask;

    private readonly List<ShaderTagId> shaderTagIds = new()
    {
        new("UniversalForwardOnly"),
        new("UniversalForward"),
        new("SRPDefaultUnlit"),
        new("LightweightForward")
    };

    public BloomPass_Render(FullscreenSettings settings, Material blurMaterial)
    {
        renderPassEvent = settings.RenderPassEvent;
        layerMask = settings.LayerMask;

        renderLayerMask = (uint)1 << settings.RenderLayerMask;

        this.blurMaterial = blurMaterial;
    }

    private void InitRendererLists(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
    {
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        var sortFlags = cameraData.defaultOpaqueSortFlags;

        var filterSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask, renderLayerMask);

        var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTagIds, renderingData, cameraData, lightData, sortFlags);
        drawSettings.overrideMaterial = blurMaterial;

        var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

        passData.rendererListHandle = renderGraph.CreateRendererList(param);
    }

    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        data.blurMaterial.SetTexture("_MainTex", data.color);
        context.cmd.ClearRenderTarget(RTClearFlags.All, new Color(0, 0, 0, 1), 1, 0);
        context.cmd.DrawRendererList(data.rendererListHandle);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        string passName = "BloomPass Render";
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.Get<BloomRenderData>();
        using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData);

        var targetDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        targetDesc.name = "_BlurredColor";
        targetDesc.clearBuffer = false;
        targetDesc.depthBufferBits = DepthBits.None;

        TextureHandle destination = renderGraph.CreateTexture(targetDesc);
        fullscreenData.BlurredTextureHandle = destination;

        passData.blurMaterial = blurMaterial;
        passData.color = resourceData.cameraColor;

        InitRendererLists(frameData, ref passData, renderGraph);

        builder.UseRendererList(passData.rendererListHandle);

        builder.SetRenderAttachment(destination, 0);
        builder.SetRenderFunc<PassData>(ExecutePass);
    }

    internal class PassData
    {
        internal RendererListHandle rendererListHandle;
        internal Material blurMaterial;
        internal TextureHandle color;
    }
}
