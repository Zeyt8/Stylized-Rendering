using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class VolumetricPass_Scattering : ScriptableRenderPass
{
    private readonly Material _scatteringMaterial;
    private readonly LayerMask _layerMask;
    private readonly uint _renderLayerMask;
    private readonly List<ShaderTagId> _shaderTagIds = new()
    {
        new("UniversalForwardOnly"),
        new("UniversalForward"),
        new("SRPDefaultUnlit"),
        new("LightweightForward")
    };
    private readonly float _intensity;
    private readonly float _blurWidth;

    public VolumetricPass_Scattering(FullscreenSettings settings, Material occludersMaterial, float intensity, float blurWidth)
    {
        renderPassEvent = settings.RenderPassEvent;
        _layerMask = settings.LayerMask;

        _renderLayerMask = (uint)1 << settings.RenderLayerMask;
        _scatteringMaterial = occludersMaterial;
        _intensity = intensity;
        _blurWidth = blurWidth;
    }

    private void InitRendererLists(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
    {
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        var sortFlags = cameraData.defaultOpaqueSortFlags;

        var filterSettings = new FilteringSettings(RenderQueueRange.opaque, _layerMask, _renderLayerMask);

        var drawSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIds, renderingData, cameraData, lightData, sortFlags);
        drawSettings.overrideMaterial = _scatteringMaterial;

        var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

        passData.RendererListHandle = renderGraph.CreateRendererList(param);
    }

    private static void ExecutePass(PassData passData, RasterGraphContext context)
    {
        Vector3 sunDirectionWorldSpace = RenderSettings.sun.transform.forward;
        Vector3 cameraPositionWorldSpace = passData.CameraWS;
        Vector3 sunPositionWorldSpace = cameraPositionWorldSpace + sunDirectionWorldSpace;
        Vector3 sunPositionViewportSpace = passData.Camera.WorldToViewportPoint(sunPositionWorldSpace);

        passData.ScatteringMaterial.SetTexture("_OcclusionTex", passData.SourceTexture);
        passData.ScatteringMaterial.SetVector("_Center", new Vector4(sunPositionViewportSpace.x, sunPositionViewportSpace.y, 0, 0));
        passData.ScatteringMaterial.SetFloat("_Intensity", passData.Intensity);
        passData.ScatteringMaterial.SetFloat("_BlurWidth", passData.BlurWidth);

        context.cmd.ClearRenderTarget(RTClearFlags.All, new Color(0, 0, 0, 1), 1, 0);
        context.cmd.DrawRendererList(passData.RendererListHandle);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var volumetricData = frameData.GetOrCreate<VolumetricRenderData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        using var builder = renderGraph.AddRasterRenderPass<PassData>("ScatteringPass", out var passData);

        var targetDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        targetDesc.name = "_ScatteringTexture";
        targetDesc.clearBuffer = true;
        targetDesc.depthBufferBits = DepthBits.None;

        TextureHandle destination = renderGraph.CreateTexture(targetDesc);
        volumetricData.ScatteringTextureHandle = destination;

        passData.ScatteringMaterial = _scatteringMaterial;
        passData.SourceTexture = volumetricData.OcclusionTextureHandle;
        passData.Intensity = _intensity;
        passData.BlurWidth = _blurWidth;
        passData.CameraWS = cameraData.worldSpaceCameraPos;
        passData.Camera = cameraData.camera;

        InitRendererLists(frameData, ref passData, renderGraph);

        builder.UseRendererList(passData.RendererListHandle);

        builder.UseTexture(resourceData.cameraColor);

        builder.SetRenderAttachment(destination, 0);
        builder.SetRenderFunc<PassData>(ExecutePass);
    }

    internal class PassData
    {
        internal RendererListHandle RendererListHandle;
        internal Material ScatteringMaterial;
        internal TextureHandle SourceTexture;
        internal float Intensity;
        internal float BlurWidth;
        internal Vector3 CameraWS;
        internal Camera Camera;
    }
}
