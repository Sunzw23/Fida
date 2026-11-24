#nullable enable
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>
/// One page is drawn with a single deferred <see cref="SpriteBatch"/>, so every drawing
/// helper shares its state. <see cref="Begin"/> is the only place that composes that page
/// batch, which keeps clipping and the manager's own draw in agreement.
/// </summary>
public static class UIBatch
{
    public static void Begin(SpriteBatch batch, RasterizerState rasterizer) =>
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, rasterizer);
}

/// <summary>
/// Clips drawing to a rectangle for the length of a scope. A <see cref="SpriteBatch"/>
/// cannot nest, so entering the scope closes the page batch, reopens it with the scissor
/// test and the clipped rectangle, and <see cref="Dispose"/> puts the page batch back.
/// Content is trimmed at the edge instead of being dropped when it only partly overlaps,
/// which is what a panned or zoomed map needs.
/// </summary>
public readonly struct UIBatchClip : IDisposable
{
    private static readonly ConditionalWeakTable<GraphicsDevice, ClipStates> Cache = new();

    private sealed class ClipStates
    {
        public readonly RasterizerState Scissored;
        public readonly RasterizerState Restored;
        public ClipStates()
        {
            Scissored = new RasterizerState
            {
                CullMode = CullMode.None, MultiSampleAntiAlias = true, ScissorTestEnable = true
            };
            Restored = new RasterizerState
            {
                CullMode = CullMode.None, MultiSampleAntiAlias = true, ScissorTestEnable = false
            };
        }
        public void Dispose() { Scissored.Dispose(); Restored.Dispose(); }
    }

    private readonly SpriteBatch _batch;
    private readonly ClipStates _states;
    private readonly Rectangle _restore;

    private UIBatchClip(SpriteBatch batch, ClipStates states, Rectangle restore)
    {
        _batch = batch; _states = states; _restore = restore;
    }

    public static UIBatchClip Enter(SpriteBatch batch, Rectangle bounds)
    {
        GraphicsDevice device = batch.GraphicsDevice;
        ClipStates states = Cache.GetValue(device, key =>
        {
            var created = new ClipStates();
            key.Disposing += (_, _) => created.Dispose();
            return created;
        });
        Rectangle restore = device.ScissorRectangle;
        Rectangle area = Rectangle.Intersect(bounds, device.Viewport.Bounds);
        batch.End();
        device.ScissorRectangle = area;
        UIBatch.Begin(batch, states.Scissored);
        return new UIBatchClip(batch, states, restore);
    }

    public void Dispose()
    {
        _batch.End();
        _batch.GraphicsDevice.ScissorRectangle = _restore;
        UIBatch.Begin(_batch, _states.Restored);
    }
}
