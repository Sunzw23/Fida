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
    public bool Navigation { get; set; }
    public bool Outlined { get; set; }
    public float? TextLineHeight { get; set; }
    public UIEditorialButton(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font)
        : base(id, bounds, text, pixel, font) { }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        bool filled = (Primary || Active) && IsEnabled;
        Color back = filled ? Active ? UITheme.AccentBlue : Color.Lerp(UIPaper.Face, UITheme.AccentBlue, HoverAmount * .18f)
            : Color.Lerp(UITheme.PanelLight, UITheme.PanelHover, HoverAmount);
        if (!IsEnabled) back = UITheme.PanelDark;
        Color fore = filled ? UIPaper.Ink : IsEnabled ? UITheme.Ink : UITheme.InkMuted;
        UIPaper.Sheet(batch, Bounds, back * EntranceAmount, _isPressed ? 0 : 3, Navigation ? 5 : 9);
        if (Navigation && Active)
            batch.Draw(Pixel, new Rectangle(Bounds.Left, Bounds.Top + Bounds.Height / 4, 3, Bounds.Height / 2), UITheme.AccentBlue * EntranceAmount);
        if ((IsFocused || IsHovered) && !filled)
            batch.Draw(Pixel, new Rectangle(Bounds.Left, Bounds.Bottom - 3, Bounds.Width, 3), UITheme.AccentBlue * EntranceAmount);
        string safe = UIFontGuard.Sanitize(Font, Text);
        Vector2 size = Font.MeasureString(safe);
        float wanted = new UIEditorialLayout(batch.GraphicsDevice.Viewport).Scale * (TextLineHeight ?? (AlignLeft ? 52 : 44)) / Font.LineSpacing;
        float scale = UITextRenderer.FitScale(size, Math.Max(1, Bounds.Width - 40), Math.Max(1, Bounds.Height - 20), wanted);
        Vector2 pos = new(AlignLeft ? Bounds.Left + 24 : Bounds.Center.X - size.X * scale / 2,
            Bounds.Center.Y - size.Y * scale / 2 + PressAmount * 2);
        UITextRenderer.DrawString(batch, Font, safe, pos, fore * EntranceAmount, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        if (AlignLeft && !Navigation)
        {
            var tip = new Vector2(Bounds.Right - 25, Bounds.Center.Y);
            float thickness = Math.Max(1.5f, Bounds.Height / 35f);
            batch.Draw(Pixel, tip - new Vector2(6, 6), null, fore * EntranceAmount, MathHelper.PiOver4,
                Vector2.Zero, new Vector2(9, thickness), SpriteEffects.None, 0);
            batch.Draw(Pixel, tip - new Vector2(6, -6), null, fore * EntranceAmount, -MathHelper.PiOver4,
                Vector2.Zero, new Vector2(9, thickness), SpriteEffects.None, 0);
        }
    }
}
