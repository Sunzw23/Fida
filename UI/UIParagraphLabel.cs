using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>Updatable status text that wraps while preserving the named UILabel state API.</summary>
public sealed class UIParagraphLabel : UILabel
{
    private string _text;
    private readonly Rectangle _area;
    private readonly SpriteFont _font;
    public UIParagraphLabel(int id, string text, Rectangle area, SpriteFont font)
        : base(id, text, area.Center.ToVector2(), font) { _text = text; _area = area; _font = font; }
    public override void Update(string text) { _text = text; base.Update(text); }
    public override Rectangle? GetScreenBounds(Viewport viewport) => IsVisible ? _area : null;
    public override void Draw(SpriteBatch batch, GameTime time)
    {
        if (IsVisible) UITextRenderer.DrawParagraph(batch, _font, _text, _area, UITheme.Ink);
    }
}
