using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

internal class OutlinePass_CopyColor : ScriptableRenderPass
{
    public OutlinePass_CopyColor(FullscreenSettings settings)
    {
        renderPassEvent = settings.RenderPassEvent;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.GetOrCreate<OutlineRenderData>();

        var targetDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        targetDesc.name = "_FullscreenCameraColor";
        targetDesc.clearBuffer = false;

        TextureHandle destination = renderGraph.CreateTexture(targetDesc);
        fullscreenData.ColorCopyTextureHandle = destination;

        renderGraph.AddCopyPass(resourceData.cameraColor, destination, passName: "OutlinePass Copy Color");
    }
}