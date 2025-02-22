using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class VolumetricPass_Occlusion : ScriptableRenderPass
{
    private readonly Material _occludersMaterial;
    private readonly LayerMask _layerMask;
    private readonly uint _renderLayerMask;
    private readonly List<ShaderTagId> _shaderTagIds = new()
    {
        new("UniversalForwardOnly"),
        new("UniversalForward"),
        new("SRPDefaultUnlit"),
        new("LightweightForward")
    };

    public VolumetricPass_Occlusion(FullscreenSettings settings, Material occludersMaterial)
    {
        renderPassEvent = settings.RenderPassEvent;
        _layerMask = settings.LayerMask;

        _renderLayerMask = (uint)1 << settings.RenderLayerMask;
        _occludersMaterial = occludersMaterial;
    }

    private void InitRendererLists(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
    {
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        var sortFlags = cameraData.defaultOpaqueSortFlags;

        var filterSettings = new FilteringSettings(RenderQueueRange.opaque, _layerMask, _renderLayerMask);

        var drawSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIds, renderingData, cameraData, lightData, sortFlags);
        drawSettings.overrideMaterial = _occludersMaterial;

        var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

        passData.RendererListHandle = renderGraph.CreateRendererList(param);
    }

    private static void ExecutePass(PassData passData, RasterGraphContext context)
    {
        context.cmd.ClearRenderTarget(RTClearFlags.All, new Color(1, 1, 1, 1), 1, 0);
        context.cmd.DrawRendererList(passData.RendererListHandle);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var volumetricData = frameData.GetOrCreate<VolumetricRenderData>();

        using var builder = renderGraph.AddRasterRenderPass<PassData>("OcclusionPass", out var passData);

        var targetDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        targetDesc.name = "_OcclusionTexture";
        targetDesc.clearBuffer = false;
        targetDesc.depthBufferBits = DepthBits.None;

        TextureHandle destination = renderGraph.CreateTexture(targetDesc);
        volumetricData.OcclusionTextureHandle = destination;

        passData.OccludersMaterial = _occludersMaterial;
        passData.SourceTexture = resourceData.cameraColor;
        passData.DestinationTexture = destination;

        InitRendererLists(frameData, ref passData, renderGraph);

        builder.UseRendererList(passData.RendererListHandle);

        builder.UseTexture(resourceData.cameraColor);

        builder.SetRenderAttachment(destination, 0);
        builder.SetRenderFunc<PassData>(ExecutePass);
    }

    internal class PassData
    {
        internal RendererListHandle RendererListHandle;
        internal Material OccludersMaterial;
        internal TextureHandle SourceTexture;
        internal TextureHandle DestinationTexture;
    }
}
