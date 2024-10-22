using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class BloomPass_CopyColor : ScriptableRenderPass
{
    public BloomPass_CopyColor(FullscreenSettings settings)
    {
        renderPassEvent = settings.RenderPassEvent;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var fullscreenData = frameData.GetOrCreate<BloomRenderData>();

        var targetDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        targetDesc.name = "_FullscreenCameraColor";
        targetDesc.clearBuffer = false;

        TextureHandle destination = renderGraph.CreateTexture(targetDesc);
        fullscreenData.ColorCopyTextureHandle = destination;

        renderGraph.AddCopyPass(resourceData.cameraColor, destination, passName: "BloomPass Copy Color");
    }
}
