#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>
/// Full-screen paper pass. The page is already rendered into one texture, so this
/// replaces the plain blit: it composites the incoming page with the outgoing one during
/// a crossfade and adds procedural paper tooth. No grain textures or per-size bakes.
/// </summary>
internal sealed class PaperPass : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly ResourceManager _resources;
    private Effect? _effect;
    private readonly VertexPositionTexture[] _quad =
    {
        new(new Vector3(-1, 1, 0), new Vector2(0, 0)),
        new(new Vector3(1, 1, 0), new Vector2(1, 0)),
        new(new Vector3(-1, -1, 0), new Vector2(0, 1)),
        new(new Vector3(1, -1, 0), new Vector2(1, 1))
    };

    public PaperPass(GraphicsDevice device, ResourceManager resources)
    {
        _device = device;
        _resources = resources;
    }

    /// <summary>
    /// The pass is created during Initialize(), before LoadContent() has run, so the
    /// effect is resolved on first use instead of in the constructor.
    /// </summary>
    private Effect ActiveEffect()
    {
        if (_effect != null) return _effect;
        var effect = _resources.PaperGrainEffect
            ?? throw new InvalidOperationException("paper_grain.fx was not loaded; the UI cannot render without its paper pass.");
        effect.CurrentTechnique = effect.Techniques["Paper"]
            ?? throw new InvalidOperationException("paper_grain.fx is missing its Paper technique.");
        _effect = effect;
        return effect;
    }

    /// <summary>Grain intensity, exposed so settings can soften or disable the tooth.</summary>
    public float GrainAmount { get; set; } = 1f;

    public void Draw(Texture2D page, Texture2D? previous, float fade)
    {
        var effect = ActiveEffect();
        var viewport = _device.Viewport;
        if (effect.Parameters["Scene"] is { } scene) scene.SetValue(page);
        if (effect.Parameters["Previous"] is { } earlier) earlier.SetValue(previous ?? page);
        if (effect.Parameters["Fade"] is { } fadeParameter) fadeParameter.SetValue(MathHelper.Clamp(fade, 0, 1));
        if (effect.Parameters["GrainAmount"] is { } grainParameter) grainParameter.SetValue(GrainAmount);
        if (effect.Parameters["RenderSize"] is { } sizeParameter)
            sizeParameter.SetValue(new Vector2(viewport.Width, viewport.Height));
        // Keep the tooth the same size relative to the UI at any resolution.
        if (effect.Parameters["Zoom"] is { } zoomParameter)
            zoomParameter.SetValue(MathF.Max(1f, viewport.Height / 720f));

        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.None;
        _device.RasterizerState = RasterizerState.CullNone;
        _device.SamplerStates[0] = SamplerState.LinearClamp;
        _device.SamplerStates[1] = SamplerState.LinearClamp;
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _quad, 0, 2);
        }
    }
    public void Dispose() { }
}

