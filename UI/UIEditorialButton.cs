using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>A normal UIButton with quieter styling; it retains the existing focus and click state machine.</summary>
public sealed class UIEditorialButton : UIButton
{
    public bool Primary { get; set; }
    public bool MainMenuStyle { get; set; }
    public bool Active { get; set; }
    public bool AlignLeft { get; set; }
    public bool Navigation { get; set; }
    public bool Outlined { get; set; }
    public bool TrailingChevron { get; set; }
    public float? TextLineHeight { get; set; }
    public UIEditorialButton(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font)
        : base(id, bounds, text, pixel, font) { }

    private void DrawMainMenu(SpriteBatch batch)
    {
        bool highlighted = IsEnabled && (Primary || IsHovered || IsFocused || _isPressed);
        float opacity = EntranceAmount;
        Rectangle shadow = Bounds;
        shadow.Offset(0, Math.Max(2, Bounds.Height / 12));
        UIPaper.MenuPaper(batch, shadow, Color.Black * (.55f * opacity), lit:false);
        UIPaper.MenuPaper(batch, Bounds, (highlighted ? new Color(255, 250, 237) : new Color(34, 68, 91)) * opacity);
        int inset = Math.Max(2, Bounds.Height / 12);
        batch.Draw(Pixel, new Rectangle(Bounds.Left, Bounds.Top + inset, inset, Math.Max(1, Bounds.Height - inset * 2)),
            (highlighted ? new Color(105, 223, 229) : new Color(42, 80, 102)) * opacity);
        Color ink = (highlighted ? new Color(14, 23, 37) : new Color(232, 238, 242)) * opacity;
        string safe = UIFontGuard.Sanitize(Font, Text);
        Vector2 size = Font.MeasureString(safe);
        float wanted = Bounds.Height * .54f / Font.LineSpacing;
        float scale = UITextRenderer.FitScale(size, Math.Max(1, Bounds.Width - Bounds.Height * 1.5f), Math.Max(1, Bounds.Height - 12), wanted);
        float offset = PressAmount * 2;
        UITextRenderer.DrawString(batch, Font, safe, new Vector2(Bounds.Left + Math.Max(16, Bounds.Height / 2),
            Bounds.Center.Y - size.Y * scale / 2 + offset), ink, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        float arm = Math.Max(4, Bounds.Height * .12f);
        float thickness = Math.Max(1.5f, Bounds.Height / 32f);
        Vector2 tip = new(Bounds.Right - Math.Max(18, Bounds.Height / 2), Bounds.Center.Y + offset);
        batch.Draw(Pixel, tip - new Vector2(arm, arm), null, ink, MathHelper.PiOver4, Vector2.Zero,
            new Vector2(arm * 1.414f, thickness), SpriteEffects.None, 0);
        batch.Draw(Pixel, tip - new Vector2(arm, -arm), null, ink, -MathHelper.PiOver4, Vector2.Zero,
            new Vector2(arm * 1.414f, thickness), SpriteEffects.None, 0);
    }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        if (MainMenuStyle) { DrawMainMenu(batch); return; }
        bool filled = (Primary || Active) && IsEnabled;
        Color back = filled ? Active ? UITheme.AccentBlue : UIPaper.Face : UITheme.PanelLight;
        if (!IsEnabled) back = UITheme.PanelDark;
        Color fore = filled ? UIPaper.Ink : IsEnabled ? UITheme.Ink : UITheme.InkMuted;
        if (AlignLeft && Primary && IsEnabled)
            UIPaper.Sheet(batch, new Rectangle(Bounds.Left - 5, Bounds.Top + 4, 13, Math.Max(1, Bounds.Height - 8)),
                UITheme.AccentBlue * EntranceAmount, 1, 3, shape: UIPaperShape.Ticket);
        DrawPaperButton(batch, back, Navigation ? 5 : 9, AlignLeft || TrailingChevron ? UIPaperShape.Tag : UIPaperShape.Cut);
        if (Navigation && Active)
            batch.Draw(Pixel, new Rectangle(Bounds.Left, Bounds.Top + Bounds.Height / 4, 3, Bounds.Height / 2), UITheme.AccentBlue * EntranceAmount);
        string safe = UIFontGuard.Sanitize(Font, Text);
        Vector2 size = Font.MeasureString(safe);
        float wanted = new UIEditorialLayout(batch.GraphicsDevice.Viewport).Scale * (TextLineHeight ?? (AlignLeft ? 52 : 44)) / Font.LineSpacing;
        float scale = UITextRenderer.FitScale(size, Math.Max(1, Bounds.Width - 40), Math.Max(1, Bounds.Height - 20), wanted);
        Vector2 pos = new(AlignLeft ? Bounds.Left + 24 : Bounds.Center.X - size.X * scale / 2,
            Bounds.Center.Y - size.Y * scale / 2 + PressAmount * 2);
        UITextRenderer.DrawString(batch, Font, safe, pos, fore * EntranceAmount, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        if ((AlignLeft && !Navigation) || TrailingChevron)
        {
            var tip = new Vector2(Bounds.Right - 25, Bounds.Center.Y + PressAmount * 2);
            float thickness = Math.Max(1.5f, Bounds.Height / 35f);
            batch.Draw(Pixel, tip - new Vector2(6, 6), null, fore * EntranceAmount, MathHelper.PiOver4,
                Vector2.Zero, new Vector2(9, thickness), SpriteEffects.None, 0);
            batch.Draw(Pixel, tip - new Vector2(6, -6), null, fore * EntranceAmount, -MathHelper.PiOver4,
                Vector2.Zero, new Vector2(9, thickness), SpriteEffects.None, 0);
        }
    }
}

