using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlineRenderData : ContextItem
{
    public TextureHandle ColorCopyTextureHandle;
    public TextureHandle ObjectTextureHandle;

    public override void Reset()
    {
        ColorCopyTextureHandle = TextureHandle.nullHandle;
        ObjectTextureHandle = TextureHandle.nullHandle;
    }
}

public class OutlineRenderFeature : ScriptableRendererFeature
{
    public Material overrideMaterial;

    public Material fullscreenMaterial;

    public FullscreenSettings Settings = new();

    private OutlinePass_CopyColor outlinePass_CopyColor;
    private OutlinePass_Render outlinePass_Render;
    private OutlinePass_Final outlinePass_Final;

    public override void Create()
    {
        outlinePass_CopyColor = new OutlinePass_CopyColor(Settings);
        outlinePass_Render = new OutlinePass_Render(Settings, overrideMaterial);
        outlinePass_Final = new OutlinePass_Final(Settings, fullscreenMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(outlinePass_CopyColor);
        renderer.EnqueuePass(outlinePass_Render);
        renderer.EnqueuePass(outlinePass_Final);
    }
}
