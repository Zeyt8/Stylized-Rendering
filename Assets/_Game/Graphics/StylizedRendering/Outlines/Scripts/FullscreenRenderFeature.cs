using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class FullscreenRenderData : ContextItem
{
    public TextureHandle ColorCopyTextureHandle;
    public TextureHandle ObjectTextureHandle;

    public override void Reset()
    {
        ColorCopyTextureHandle = TextureHandle.nullHandle;
        ObjectTextureHandle = TextureHandle.nullHandle;
    }
}

[Serializable]
public class FullscreenSettings
{
    /// <summary>
    /// When to render the outlines.
    /// </summary>
    public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

    /// <summary>
    /// The layer mask of the objects to include in the outlines.
    /// </summary>
    public LayerMask LayerMask = 0;

    /// <summary>
    /// The render layer mask of the objects to include in the outlines.
    /// </summary>
    public int RenderLayerMask = 1;
}

public class FullscreenRenderFeature : ScriptableRendererFeature
{
    public Material overrideMaterial;

    public Material fullscreenMaterial;

    public FullscreenSettings Settings = new();

    private FullscreenPass_CopyColor fullscreenPass_CopyColor;
    private FullscreenPass_Render fullscreenPass_Render;
    private FullscreenPass_Final fullscreenPass_Final;

    public override void Create()
    {
        fullscreenPass_CopyColor = new FullscreenPass_CopyColor(Settings);
        fullscreenPass_Render = new FullscreenPass_Render(Settings, overrideMaterial);
        fullscreenPass_Final = new FullscreenPass_Final(Settings, fullscreenMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(fullscreenPass_CopyColor);
        renderer.EnqueuePass(fullscreenPass_Render);
        renderer.EnqueuePass(fullscreenPass_Final);
    }
}
