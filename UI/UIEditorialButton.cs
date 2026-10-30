using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>A normal UIButton with quieter styling; it retains the existing focus and click state machine.</summary>
public sealed class UIEditorialButton : UIButton
{
    public bool Primary { get; set; }
    public bool Active { get; set; }
    public bool AlignLeft { get; set; }
    public UIEditorialButton(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font)
        : base(id, bounds, text, pixel, font) { }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        bool filled = (Primary || Active) && IsEnabled;
        Color back = filled ? Color.Lerp(UITheme.Ink, UITheme.AccentBlue, HoverAmount * .32f)
            : Color.Lerp(Color.Transparent, UITheme.PanelHover, HoverAmount);
        Color fore = filled ? UITheme.BgTop : IsEnabled ? UITheme.Ink : UITheme.InkMuted;
        batch.Draw(Pixel, Bounds, back * EntranceAmount);
        if (IsFocused || IsHovered || _isPressed)
            batch.Draw(Pixel, new Rectangle(Bounds.Left, Bounds.Bottom - 3, Bounds.Width, 3), UITheme.AccentBlue * EntranceAmount);
        string safe = UIFontGuard.Sanitize(Font, Text);
        Vector2 size = Font.MeasureString(safe);
        float wanted = new UIEditorialLayout(batch.GraphicsDevice.Viewport).Scale * (AlignLeft ? 52 : 44) / Font.LineSpacing;
        float scale = UITextRenderer.FitScale(size, Math.Max(1, Bounds.Width - 40), Math.Max(1, Bounds.Height - 20), wanted);
        Vector2 pos = new(AlignLeft ? Bounds.Left + 24 : Bounds.Center.X - size.X * scale / 2,
            Bounds.Center.Y - size.Y * scale / 2);
        UITextRenderer.DrawString(batch, Font, safe, pos, fore * EntranceAmount, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
}
