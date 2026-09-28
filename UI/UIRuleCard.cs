using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>Swiss editorial card used by the in-game field guide.</summary>
    public sealed class UIRuleCard : UIControl
    {
        private readonly Rectangle _bounds;
        private readonly string _tag;
        private readonly string _heading;
        private readonly string _detail;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        private readonly SpriteFont _titleFont;
        private readonly Color _accent;

        public UIRuleCard(int id, Rectangle bounds, string tag, string heading, string detail,
            Texture2D pixel, SpriteFont font, SpriteFont titleFont, Color accent)
            : base(id)
        {
            _bounds = bounds;
            _tag = tag;
            _heading = heading;
            _detail = detail;
            _pixel = pixel;
            _font = font;
            _titleFont = titleFont;
            _accent = accent;
        }

        public override Rectangle? GetScreenBounds(Viewport viewport) =>
            IsVisible ? _bounds : null;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsVisible) return;

            float scale = UIDesktopLayout.LayoutScale(spriteBatch.GraphicsDevice.Viewport);
            spriteBatch.Draw(_pixel, _bounds, UITheme.PanelLight);
            spriteBatch.Draw(_pixel,
                new Rectangle(_bounds.Left, _bounds.Top, Math.Max(4, (int)(5 * scale)), _bounds.Height),
                _accent);
            UIUtils.DrawRectangleOutlines(spriteBatch, _bounds, UITheme.Ink * 0.18f, 1, _pixel);

            float inset = 18f * scale;
            Vector2 tagPosition = new(_bounds.Left + inset, _bounds.Top + inset);
            DrawText(spriteBatch, _font, _tag, tagPosition,
                _accent, scale * 0.84f, _bounds.Width * 0.24f, _bounds.Height * 0.42f);

            float textLeft = _bounds.Left + Math.Max(62f * scale, _bounds.Width * 0.22f);
            float availableWidth = _bounds.Right - textLeft - 16f * scale;
            float headingY = _bounds.Top + ( _bounds.Height < 100 * scale ? 10f : 12f) * scale;
            DrawText(spriteBatch, _titleFont, _heading,
                new Vector2(textLeft, headingY), UITheme.Ink,
                scale, availableWidth, _bounds.Height * 0.42f);

            float detailY = _bounds.Top + _bounds.Height * 0.53f;
            DrawText(spriteBatch, _font, _detail,
                new Vector2(textLeft, detailY), UITheme.InkMuted,
                scale * 0.84f, availableWidth, _bounds.Height * 0.38f);
        }

        private static void DrawText(SpriteBatch spriteBatch, SpriteFont font, string text,
            Vector2 position, Color color, float preferredScale, float maxWidth, float maxHeight)
        {
            string displayText = UIFontGuard.Sanitize(font, text);
            Vector2 size = font.MeasureString(displayText);
            float fitScale = MathHelper.Min(maxWidth / MathHelper.Max(1f, size.X),
                maxHeight / MathHelper.Max(1f, size.Y));
            float drawScale = MathHelper.Min(preferredScale, fitScale);
            spriteBatch.DrawString(font, displayText, position, color,
                0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
        }
    }
}
