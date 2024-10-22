using System;
using UnityEngine.Rendering.Universal;
using UnityEngine;

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