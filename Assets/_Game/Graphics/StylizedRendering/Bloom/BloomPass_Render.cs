using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BloomPass_Render : ScriptableRenderPass
{
    private readonly Material _blurMaterial;
    private readonly LayerMask _layerMask;
    private readonly uint _renderLayerMask;
    private readonly int _pass;
    private readonly float _blurIntensity;

    private readonly List<ShaderTagId> _shaderTagIds = new()
    {
        new("UniversalForwardOnly"),
        new("UniversalForward"),
        new("SRPDefaultUnlit"),
        new("LightweightForward"),
        new("GaussianBlur")
    };

    public BloomPass_Render(FullscreenSettings settings, Material blurMaterial, float blurIntensity, int pass)
    {
        renderPassEvent = settings.RenderPassEvent;
        _layerMask = settings.LayerMask;

        _renderLayerMask = (uint)1 << settings.RenderLayerMask;

        _blurMaterial = blurMaterial;
        _pass = pass;
        _blurIntensity = blurIntensity;
    }

    private void InitRendererLists(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
    {
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        var sortFlags = cameraData.defaultOpaqueSortFlags;

        var filterSettings = new FilteringSettings(RenderQueueRange.opaque, _layerMask, _renderLayerMask);

        var drawSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIds, renderingData, cameraData, lightData, sortFlags);
        drawSettings.overrideMaterial = _blurMaterial;

        var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

        passData.RendererListHandle = renderGraph.CreateRendererList(param);
    }

    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        data.BlurMaterial.SetTexture("_MainTex", data.Color);
        data.BlurMaterial.SetFloat("_BlurSize", data.BlurIntensity);
        context.cmd.ClearRenderTarget(RTClearFlags.All, new Color(0, 0, 0, 1), 1, 0);
        context.cmd.DrawRendererList(data.RendererListHandle);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        string passName = $"BloomPass Render {_pass}";
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.Get<BloomRenderData>();
        using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData);

        var targetDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        targetDesc.name = $"_BlurredColor {_pass}";
        targetDesc.clearBuffer = false;
        targetDesc.depthBufferBits = DepthBits.None;

        TextureHandle destination = renderGraph.CreateTexture(targetDesc);
        fullscreenData.BlurredTextureHandle[_pass] = destination;

        passData.BlurMaterial = _blurMaterial;
        passData.Color = (_pass != 0 ? fullscreenData.BlurredTextureHandle[_pass - 1] : resourceData.cameraColor);
        passData.BlurIntensity = _blurIntensity;

        InitRendererLists(frameData, ref passData, renderGraph);

        builder.UseRendererList(passData.RendererListHandle);

        if (_pass != 0)
        {
            builder.UseTexture(fullscreenData.BlurredTextureHandle[_pass - 1]);
        }

        builder.SetRenderAttachment(destination, 0);
        builder.SetRenderFunc<PassData>(ExecutePass);
    }

    internal class PassData
    {
        internal RendererListHandle RendererListHandle;
        internal Material BlurMaterial;
        internal TextureHandle Color;
        internal float BlurIntensity;
    }
}
