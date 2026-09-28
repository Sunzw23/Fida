using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Resources;

namespace Fida.UI
{
    internal class UIUtils
    {
        /// <summary>
        /// 绘制矩形边框
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="pixel"></param>
        public static void DrawRectangleOutlines(SpriteBatch sb, Rectangle rect, Color color, int width, Texture2D pixel)
        {
            if (rect.Width <= 0 || rect.Height <= 0 || width <= 0) return;
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, width), color); // Top
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - width, rect.Width, width), color); // Bottom
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Top, width, rect.Height), color); // Left
            sb.Draw(pixel, new Rectangle(rect.Right - width, rect.Top, width, rect.Height), color); // Right
        }

        /// <summary>Flat focus frame shared by controls and dialogs.</summary>
        public static void DrawGlowFrame(SpriteBatch sb, Rectangle rect, Texture2D pixel,
            Color color, int coreWidth = 2, float intensity = 1f)
        {
            DrawRectangleOutlines(sb, rect, color, coreWidth, pixel);
        }

        /// <summary>
        /// Draws the shared modern signal frame used by cards and modal panels. Keeping
        /// it here prevents every new screen from inventing another border style.
        /// </summary>
        public static void DrawEngravedFrame(SpriteBatch sb, Rectangle rect, Texture2D pixel,
            Color outerColor, Color innerColor, int outerWidth = 3, int inset = 8,
            int cornerLength = 20, bool drawShadow = true)
        {
            if (rect.Width <= inset * 2 || rect.Height <= inset * 2) return;

            DrawRectangleOutlines(sb, rect, outerColor, System.Math.Max(1, outerWidth), pixel);
            int rule = System.Math.Min(cornerLength * 3, System.Math.Max(24, rect.Width / 4));
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Top, rule,
                System.Math.Max(3, outerWidth + 1)), innerColor);
        }

        /// <summary>
        /// Measures text and draws it centered on <paramref name="center"/>,
        /// shrinking from <paramref name="preferredScale"/> until it fits
        /// <paramref name="maxWidth"/>. When <paramref name="minScale"/> is
        /// greater than zero the scale never drops below it.
        /// </summary>
        public static void DrawCenteredFit(SpriteBatch batch, SpriteFont font, string text, Vector2 center,
            float maxWidth, float preferredScale, Color color, float minScale = 0f)
        {
            string safe = UIFontGuard.Sanitize(font, text);
            Vector2 size = font.MeasureString(safe);
            if (size.X <= 0f) return;
            float scale = System.Math.Min(preferredScale, maxWidth / size.X);
            if (minScale > 0f) scale = System.Math.Max(minScale, scale);
            batch.DrawString(font, safe, center - size * scale / 2f, color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Draws the shared modal backdrop used by the decision dialogs: a
        /// full-viewport vignette, the panel fill, the engraved frame, and
        /// the gold header divider.
        /// </summary>
        public static void DrawModalShell(SpriteBatch sb, Texture2D pixel, Rectangle panel, Viewport viewport)
        {
            sb.Draw(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), UITheme.Vignette * 0.78f);
            sb.Draw(pixel, panel, UITheme.PanelLight);
            DrawEngravedFrame(sb, panel, pixel, UITheme.GoldBright, UITheme.Ink,
                UIConfig.ScaleDesktopMetric(4), UIConfig.ScaleDesktopMetric(10), UIConfig.ScaleDesktopMetric(28));
            sb.Draw(pixel, new Rectangle(panel.X + UIConfig.ScaleDesktopMetric(80),
                panel.Y + UIConfig.ScaleDesktopMetric(86),
                panel.Width - UIConfig.ScaleDesktopMetric(160), UIConfig.ScaleDesktopMetric(2)), UITheme.Gold);
        }

        private static void DrawCorner(SpriteBatch sb, int x, int y, int dx, int dy,
            int length, int width, Color color, Texture2D pixel)
        {
            int horizontalX = dx > 0 ? x : x - length + 1;
            int verticalY = dy > 0 ? y : y - length + 1;
            sb.Draw(pixel, new Rectangle(horizontalX, y - width / 2, length, width), color);
            sb.Draw(pixel, new Rectangle(x - width / 2, verticalY, width, length), color);
        }


        
        /// <summary>
        /// 辅助方法：绘制矩形（用于填充或背景）
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        /// <param name="pixel"></param>
        private static void DrawRectangle(SpriteBatch sb, Rectangle rect, Color color, Texture2D pixel)
        {
            sb.Draw(pixel, rect, color);
        }



        /// <summary>
        /// 绘制游戏消息。
        /// </summary>
        /// <param name="message"></param>
        private static void DrawString(SpriteBatch sb, SpriteFont font, Vector2 position, string text, Color color)
        {
            sb.DrawString(font, text, position, color);
        }


        /// <summary>
        /// 绘制字符串到矩形中间
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="font"></param>
        /// <param name="rect"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        private static void DrawStringInBox(SpriteBatch sb, SpriteFont font, Rectangle rect, string text, Color color)
        {
            var textSize = font.MeasureString(text);
            var position = new Vector2(rect.X + (rect.Width - textSize.X) / 2, rect.Y + (rect.Height - textSize.Y) / 2);
            sb.DrawString(font, text, position, color);
        }
    }
}
