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
    [SerializeField] private Material overrideMaterial;
    [SerializeField] private Material fullscreenMaterial;
    [SerializeField] private FullscreenSettings Settings = new();

    private OutlinePass_CopyColor _outlinePass_CopyColor;
    private OutlinePass_Render _outlinePass_Render;
    private OutlinePass_Final _outlinePass_Final;

    public override void Create()
    {
        _outlinePass_CopyColor = new OutlinePass_CopyColor(Settings);
        _outlinePass_Render = new OutlinePass_Render(Settings, overrideMaterial);
        _outlinePass_Final = new OutlinePass_Final(Settings, fullscreenMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_outlinePass_CopyColor);
        renderer.EnqueuePass(_outlinePass_Render);
        renderer.EnqueuePass(_outlinePass_Final);
    }
}
