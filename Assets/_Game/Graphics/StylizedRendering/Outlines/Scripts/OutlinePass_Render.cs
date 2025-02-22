using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlinePass_Render : ScriptableRenderPass
{
    private readonly Material _overrideMaterial;
    private readonly LayerMask _layerMask;
    private readonly uint _renderLayerMask;

    private readonly List<ShaderTagId> _shaderTagIds = new()
    {
        new("UniversalForwardOnly"),
        new("UniversalForward"),
        new("SRPDefaultUnlit"),
        new("LightweightForward")
    };

    public OutlinePass_Render(FullscreenSettings settings, Material overrideMaterial)
    {
        renderPassEvent = settings.RenderPassEvent;
        _layerMask = settings.LayerMask;

        _renderLayerMask = (uint)1 << settings.RenderLayerMask;

        _overrideMaterial = overrideMaterial;
    }

    private void InitRendererLists(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
    {
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        var sortFlags = cameraData.defaultOpaqueSortFlags;

        var filterSettings = new FilteringSettings(RenderQueueRange.opaque, _layerMask, _renderLayerMask);

        var drawSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIds, renderingData, cameraData, lightData, sortFlags);
        drawSettings.overrideMaterial = _overrideMaterial;

        var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

        passData.RendererListHandle = renderGraph.CreateRendererList(param);
    }

    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        context.cmd.ClearRenderTarget(RTClearFlags.All, new Color(0, 0, 0, 1), 1, 0);
        context.cmd.DrawRendererList(data.RendererListHandle);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        string passName = "OutlinePass Render";
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.Get<OutlineRenderData>();
        using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData);

        var targetDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        targetDesc.name = "_FullscreenObjects";
        targetDesc.clearBuffer = false;
        targetDesc.depthBufferBits = DepthBits.None;

        TextureHandle destination = renderGraph.CreateTexture(targetDesc);
        fullscreenData.ObjectTextureHandle = destination;

        InitRendererLists(frameData, ref passData, renderGraph);

        builder.UseRendererList(passData.RendererListHandle);

        builder.SetRenderAttachment(destination, 0);
        builder.SetRenderFunc<PassData>(ExecutePass);
    }

    internal class PassData
    {
        internal RendererListHandle RendererListHandle;
    }
}