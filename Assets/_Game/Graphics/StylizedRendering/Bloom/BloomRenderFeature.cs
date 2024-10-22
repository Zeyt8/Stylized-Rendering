using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BloomRenderData : ContextItem
{
    public TextureHandle ColorCopyTextureHandle;
    public TextureHandle BlurredTextureHandle;

    public override void Reset()
    {
        ColorCopyTextureHandle = TextureHandle.nullHandle;
        BlurredTextureHandle = TextureHandle.nullHandle;
    }
}

public class BloomRenderFeature : ScriptableRendererFeature
{
    public Material blurMaterial;
    public Material fullscreenMaterial;

    public FullscreenSettings Settings = new();

    private BloomPass_CopyColor bloomPass_CopyColor;
    private BloomPass_Render bloomPass_Render;
    private BloomPass_Final bloomPass_Final;

    public override void Create()
    {
        bloomPass_CopyColor = new BloomPass_CopyColor(Settings);
        bloomPass_Render = new BloomPass_Render(Settings, blurMaterial);
        bloomPass_Final = new BloomPass_Final(Settings, fullscreenMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(bloomPass_CopyColor);
        renderer.EnqueuePass(bloomPass_Render);
        renderer.EnqueuePass(bloomPass_Final);
    }
}
