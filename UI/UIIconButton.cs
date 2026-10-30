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
            Pause,
            PreviousPage,
            NextPage
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

        public static Rectangle StandardBounds(Viewport viewport)
        {
#if FIDA_ANDROID
            Rectangle safe = UIMobileSafeArea.Get(viewport);
            return new Rectangle(safe.Left + 8, safe.Top + 4, 64, 64);
#else
            return UIDesktopLayout.MapRectangle(new Rectangle(32, 28, 68, 60), viewport);
#endif
        }

        public static void DrawBackButton(SpriteBatch spriteBatch, Texture2D pixel,
            Rectangle bounds, bool hovered = false, bool pressed = false)
        {
            UIPaper.Sheet(spriteBatch, bounds, hovered ? UITheme.AccentBlue : UIPaper.Face, pressed ? 0 : 3, 7);

            float half = Math.Max(10f, Math.Min(bounds.Width, bounds.Height) * 0.25f);
            Vector2 center = bounds.Center.ToVector2();
            Vector2 tip = new(center.X - half, center.Y);
            float thickness = Math.Max(2f, Math.Min(bounds.Width, bounds.Height) * 0.055f);
            DrawLine(spriteBatch, pixel, tip, new Vector2(center.X + half, center.Y),
                UIPaper.Ink, thickness);
            DrawLine(spriteBatch, pixel, tip,
                new Vector2(center.X - half * 0.15f, center.Y - half * 0.82f),
                UIPaper.Ink, thickness);
            DrawLine(spriteBatch, pixel, tip,
                new Vector2(center.X - half * 0.15f, center.Y + half * 0.82f),
                UIPaper.Ink, thickness);
        }

        public static void DrawCloseButton(SpriteBatch spriteBatch, Texture2D pixel,
            Rectangle bounds)
        {
            UIPaper.Sheet(spriteBatch, bounds, UIPaper.Face, 2, 6);
            float inset = Math.Min(bounds.Width, bounds.Height) * 0.28f;
            Vector2 topLeft = new(bounds.Left + inset, bounds.Top + inset);
            Vector2 bottomRight = new(bounds.Right - inset, bounds.Bottom - inset);
            DrawLine(spriteBatch, pixel, topLeft, bottomRight, UIPaper.Ink, 2f);
            DrawLine(spriteBatch, pixel, new Vector2(bottomRight.X, topLeft.Y),
                new Vector2(topLeft.X, bottomRight.Y), UIPaper.Ink, 2f);
        }

        public override bool Contains(Point point) =>
            new Rectangle(Bounds.X - HitPadding, Bounds.Y - HitPadding,
                Bounds.Width + HitPadding * 2, Bounds.Height + HitPadding * 2).Contains(point);

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_icon == IconKind.Back)
            {
                DrawBackButton(spriteBatch, Pixel, Bounds,
                    State == UIButtonState.Hovered, State == UIButtonState.Pressed);
                return;
            }

            Color accent = UIPaper.Ink;
            UIPaper.Sheet(spriteBatch, Bounds, State == UIButtonState.Hovered ? UITheme.Gold : UIPaper.Face,
                State == UIButtonState.Pressed ? 0 : 3, 6);

            float layoutScale = UIDesktopLayout.LayoutScale(spriteBatch.GraphicsDevice.Viewport);
            float pressOffset = State == UIButtonState.Pressed ? 2f * layoutScale : 0f;
            Vector2 center = Bounds.Center.ToVector2() + new Vector2(0f, pressOffset);
            if (_icon == IconKind.Pause) DrawPause(spriteBatch, center, accent);
            else DrawPageArrow(spriteBatch, center, accent, _icon == IconKind.NextPage);
        }

        private void DrawPageArrow(SpriteBatch spriteBatch, Vector2 center, Color color, bool right)
        {
            float direction = right ? 1f : -1f;
            float half = Math.Max(9f, Math.Min(Bounds.Width, Bounds.Height) * 0.2f);
            float thickness = Math.Max(2f, Math.Min(Bounds.Width, Bounds.Height) * 0.055f);
            Vector2 start = new(center.X - direction * half, center.Y);
            Vector2 tip = new(center.X + direction * half, center.Y);
            DrawLine(spriteBatch, Pixel, start, tip, color, thickness);
            DrawLine(spriteBatch, Pixel, tip,
                new Vector2(tip.X - direction * half * 0.65f, center.Y - half * 0.75f),
                color, thickness);
            DrawLine(spriteBatch, Pixel, tip,
                new Vector2(tip.X - direction * half * 0.65f, center.Y + half * 0.75f),
                color, thickness);
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

        private static void DrawFrame(SpriteBatch spriteBatch, Texture2D pixel,
            Rectangle bounds, Color color, int thickness)
        {
            spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), color);
        }

        private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel,
            Vector2 start, Vector2 end,
            Color color, float thickness)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length <= 0.01f) return;
            spriteBatch.Draw(pixel, start, null, color,
                (float)Math.Atan2(delta.Y, delta.X), new Vector2(0f, 0.5f),
                new Vector2(length, thickness), SpriteEffects.None, 0f);
        }
    }
}
