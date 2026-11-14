#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>Backdrop, header and folio shared by every rules page.</summary>
public sealed class UIRulesSurface : UIControl
{
    private readonly ResourceManager _resources;
    private readonly int _page;
    private readonly int _total;

    public UIRulesSurface(ResourceManager resources, int page, int total) : base(-60)
    {
        _resources = resources;
        _page = page;
        _total = total;
    }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var layout = new UIRulesLayout(batch.GraphicsDevice.Viewport);
        var canvas = layout.Canvas;
        UIPaper.Background(batch, batch.GraphicsDevice.Viewport);
        UIPaper.Backdrop(batch, batch.GraphicsDevice.Viewport, time);
        UIPaper.Sheet(batch,
            canvas.Portrait ? canvas.Rect(30, 30, 840, 135) : canvas.Rect(45, 30, 1830, 135),
            UITheme.Panel, 3, 9);
        UIRulesText.Line(batch, _resources.LabelFont, "Rules", layout.Header, 65, layout.Scale, UITheme.Ink);
        UIRulesText.Line(batch, _resources.CaptionFont, $"{_page + 1} / {_total}", layout.PageLabel,
            26, layout.Scale, UITheme.InkMuted, centered: true);
    }
}
