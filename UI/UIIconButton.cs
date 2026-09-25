using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// Compact button for universal navigation actions. It retains UIButton's
    /// input, focus and event behavior while replacing verbose copy with a
    /// resolution-independent code-drawn icon.
    /// </summary>
    public sealed class UIIconButton : UIButton
    {
        public enum IconKind
        {
            Back,
            Pause
        }

        private readonly IconKind _icon;

        // The drawn glyph stays compact while the touch target meets a more
        // forgiving mobile size. UIButtonBase.Update uses Contains as well.
        public int HitPadding { get; set; } =
#if FIDA_ANDROID
            10;
#else
            0;
#endif

        public UIIconButton(int id, Rectangle bounds, IconKind icon,
            Texture2D pixel, SpriteFont font)
            : base(id, bounds, "", pixel, font)
        {
            _icon = icon;
        }

        public override bool Contains(Point point) =>
            new Rectangle(Bounds.X - HitPadding, Bounds.Y - HitPadding,
                Bounds.Width + HitPadding * 2, Bounds.Height + HitPadding * 2).Contains(point);

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            Color accent = State is UIButtonState.Pressed or UIButtonState.Selected
                ? UITheme.AccentGreen
                : State == UIButtonState.Hovered ? UITheme.GoldBright : UITheme.AccentBlue;
            spriteBatch.Draw(Pixel, Bounds, GetBackColor());

            if (State == UIButtonState.Pressed)
                spriteBatch.Draw(Pixel, Bounds, UITheme.AccentViolet * 0.24f);

            float pulse = State == UIButtonState.Normal
                ? 0.48f
                : 0.72f + 0.18f * (float)Math.Sin(
                    gameTime.TotalGameTime.TotalSeconds * 7.0);
            UIUtils.DrawGlowFrame(spriteBatch, Bounds, Pixel, accent,
                Math.Max(1, GetBorderThickness()), pulse);

#if FIDA_ANDROID
            float layoutScale = 1f;
#else
            float layoutScale = UIDesktopLayout.Scale(spriteBatch.GraphicsDevice.Viewport);
#endif
            float pressOffset = State == UIButtonState.Pressed ? 2f * layoutScale : 0f;
            Vector2 center = Bounds.Center.ToVector2() + new Vector2(0f, pressOffset);
            if (_icon == IconKind.Back) DrawBack(spriteBatch, center, accent, layoutScale);
            else if (_icon == IconKind.Pause) DrawPause(spriteBatch, center, accent);
        }

        private void DrawPause(SpriteBatch spriteBatch, Vector2 center, Color color)
        {
            int barWidth = Math.Max(5, Bounds.Width / 11);
            int barHeight = Math.Max(20, Bounds.Height / 3);
            int gap = Math.Max(7, Bounds.Width / 10);
            spriteBatch.Draw(Pixel, new Rectangle((int)center.X - gap - barWidth,
                (int)center.Y - barHeight / 2, barWidth, barHeight), color);
            spriteBatch.Draw(Pixel, new Rectangle((int)center.X + gap,
                (int)center.Y - barHeight / 2, barWidth, barHeight), color);
        }

        private void DrawBack(SpriteBatch spriteBatch, Vector2 center, Color color, float scale)
        {
            float half = Math.Max(10f, Math.Min(Bounds.Width, Bounds.Height) * 0.25f);
            float thickness = Math.Max(1f, 4f * scale);
            Vector2 tip = new(center.X - half, center.Y);
            Vector2 tail = new(center.X + half, center.Y);
            DrawLine(spriteBatch, tip, tail, color, thickness);
            DrawLine(spriteBatch, tip,
                new Vector2(center.X - half * 0.18f, center.Y - half * 0.82f), color, thickness);
            DrawLine(spriteBatch, tip,
                new Vector2(center.X - half * 0.18f, center.Y + half * 0.82f), color, thickness);
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end,
            Color color, float thickness)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length <= 0.01f) return;
            spriteBatch.Draw(Pixel, start, null, color,
                (float)Math.Atan2(delta.Y, delta.X), new Vector2(0f, 0.5f),
                new Vector2(length, thickness), SpriteEffects.None, 0f);
        }
    }
}
