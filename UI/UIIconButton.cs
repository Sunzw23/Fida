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
            Back
        }

        private readonly IconKind _icon;

        public UIIconButton(int id, Rectangle bounds, IconKind icon,
            Texture2D pixel, SpriteFont font)
            : base(id, bounds, "", pixel, font)
        {
            _icon = icon;
        }

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

            float pressOffset = State == UIButtonState.Pressed ? 2f : 0f;
            Vector2 center = Bounds.Center.ToVector2() + new Vector2(0f, pressOffset);
            if (_icon == IconKind.Back) DrawBack(spriteBatch, center, accent);
        }

        private void DrawBack(SpriteBatch spriteBatch, Vector2 center, Color color)
        {
            float half = Math.Max(10f, Math.Min(Bounds.Width, Bounds.Height) * 0.25f);
            Vector2 tip = new(center.X - half, center.Y);
            Vector2 tail = new(center.X + half, center.Y);
            DrawLine(spriteBatch, tip, tail, color, 4f);
            DrawLine(spriteBatch, tip,
                new Vector2(center.X - half * 0.18f, center.Y - half * 0.82f), color, 4f);
            DrawLine(spriteBatch, tip,
                new Vector2(center.X - half * 0.18f, center.Y + half * 0.82f), color, 4f);
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
