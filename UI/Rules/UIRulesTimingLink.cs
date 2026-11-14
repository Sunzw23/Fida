#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>
/// A single skill name on the timing page. It keeps UIButton's focus and click state machine
/// but draws only the word, so the stage table stays a piece of typography.
/// </summary>
public sealed class UIRulesTimingLink : UIButton
{
    private readonly ResourceManager _resources;
    private readonly bool _seenOnly;

    public UIRulesTimingLink(int id, Rectangle bounds, string text, ResourceManager resources, bool seenOnly)
        : base(id, bounds, text, resources.Pixel, resources.CaptionFont)
    {
        _resources = resources;
        _seenOnly = seenOnly;
    }

    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        var layout = new UIRulesLayout(batch.GraphicsDevice.Viewport);
        bool active = IsHovered || IsFocused;
        Color color = active ? UITheme.AccentBlue : _seenOnly ? UIPaper.MutedInk : UIPaper.Ink;
        UIRulesText.Line(batch, _resources.CaptionFont, Text, Bounds, 22, layout.Scale, color);
        if (active)
            UIRulesPaint.Hairline(batch, _resources.Pixel,
                new Rectangle(Bounds.Left, Bounds.Bottom - 2, Bounds.Width, 1), .55f);
    }
}
