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
    private readonly int _blurSize;
    private readonly float _sigma;
    private readonly int _pass;

    private readonly List<ShaderTagId> _shaderTagIds = new()
    {
        new("UniversalForwardOnly"),
        new("UniversalForward"),
        new("SRPDefaultUnlit"),
        new("LightweightForward"),
        new("GaussianBlur")
    };

    public BloomPass_Render(FullscreenSettings settings, Material blurMaterial, int blurSize, float sigma, int pass)
    {
        renderPassEvent = settings.RenderPassEvent;
        _layerMask = settings.LayerMask;

        _renderLayerMask = (uint)1 << settings.RenderLayerMask;

        _blurMaterial = blurMaterial;
        _blurSize = blurSize;
        _sigma = sigma;
        _pass = pass;
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
        data.BlurMaterial.SetFloat("_BlurSize", data.BlurSize);
        data.BlurMaterial.SetFloat("_Sigma", data.Sigma);
        data.BlurMaterial.SetVector("_Dir", data.PassIndex == 0 ? Vector2.right : Vector2.up);
        context.cmd.ClearRenderTarget(RTClearFlags.All, new Color(0, 0, 0, 1), 1, 0);
        context.cmd.DrawRendererList(data.RendererListHandle);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        string passName = $"BloomPass Render";
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.Get<BloomRenderData>();

        using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData);

        var targetDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        targetDesc.name = $"_BlurredColor";
        targetDesc.clearBuffer = false;
        targetDesc.depthBufferBits = DepthBits.None;

        TextureHandle intermediateTexture = renderGraph.CreateTexture(targetDesc);
        if (_pass == 0)
            fullscreenData.HorizontalBlurHandle = intermediateTexture;
        else
            fullscreenData.BlurredTextureHandle = intermediateTexture;

        passData.BlurMaterial = _blurMaterial;
        passData.Color = (_pass == 0 ? resourceData.cameraColor : fullscreenData.HorizontalBlurHandle);
        passData.BlurSize = _blurSize;
        passData.Sigma = _sigma;
        passData.PassIndex = _pass;

        InitRendererLists(frameData, ref passData, renderGraph);

        if (_pass == 1)
        {
            builder.UseTexture(fullscreenData.HorizontalBlurHandle);
        }
        builder.UseRendererList(passData.RendererListHandle);
        builder.SetRenderAttachment(intermediateTexture, 0);
        builder.SetRenderFunc<PassData>(ExecutePass);
    }

    internal class PassData
    {
        internal RendererListHandle RendererListHandle;
        internal Material BlurMaterial;
        internal int PassIndex;
        internal TextureHandle Color;
        internal int BlurSize;
        internal float Sigma;
    }
}
