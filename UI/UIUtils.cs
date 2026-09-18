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
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, width), color); // Top
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - width, rect.Width, width), color); // Bottom
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Top, width, rect.Height), color); // Left
            sb.Draw(pixel, new Rectangle(rect.Right - width, rect.Top, width, rect.Height), color); // Right
        }

        /// <summary>
        /// Draws the shared engraved frame used by cards and modal panels. Keeping the
        /// ornament here prevents every new screen from inventing another border style.
        /// </summary>
        public static void DrawEngravedFrame(SpriteBatch sb, Rectangle rect, Texture2D pixel,
            Color outerColor, Color innerColor, int outerWidth = 3, int inset = 8,
            int cornerLength = 20, bool drawShadow = true)
        {
            if (rect.Width <= inset * 2 || rect.Height <= inset * 2) return;

            if (drawShadow)
            {
                var shadow = new Rectangle(rect.X + 5, rect.Y + 6, rect.Width, rect.Height);
                DrawRectangleOutlines(sb, shadow, UITheme.Vignette * 0.34f,
                    System.Math.Max(2, outerWidth), pixel);
            }

            DrawRectangleOutlines(sb, rect, outerColor, outerWidth, pixel);
            var inner = new Rectangle(rect.X + inset, rect.Y + inset,
                rect.Width - inset * 2, rect.Height - inset * 2);
            DrawRectangleOutlines(sb, inner, innerColor, 1, pixel);

            int length = System.Math.Min(cornerLength,
                System.Math.Min(inner.Width / 4, inner.Height / 4));
            int accent = System.Math.Max(2, outerWidth - 1);
            DrawCorner(sb, inner.Left, inner.Top, 1, 1, length, accent, outerColor, pixel);
            DrawCorner(sb, inner.Right - 1, inner.Top, -1, 1, length, accent, outerColor, pixel);
            DrawCorner(sb, inner.Left, inner.Bottom - 1, 1, -1, length, accent, outerColor, pixel);
            DrawCorner(sb, inner.Right - 1, inner.Bottom - 1, -1, -1, length, accent, outerColor, pixel);

            // Small engraved ticks break up long, rigid edges without visual clutter.
            int tick = System.Math.Max(5, length / 3);
            int cx = rect.Center.X;
            int cy = rect.Center.Y;
            sb.Draw(pixel, new Rectangle(cx - tick, rect.Top, tick * 2, outerWidth + 1), outerColor);
            sb.Draw(pixel, new Rectangle(cx - tick, rect.Bottom - outerWidth - 1,
                tick * 2, outerWidth + 1), outerColor);
            sb.Draw(pixel, new Rectangle(rect.Left, cy - tick, outerWidth + 1, tick * 2), outerColor);
            sb.Draw(pixel, new Rectangle(rect.Right - outerWidth - 1, cy - tick,
                outerWidth + 1, tick * 2), outerColor);
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
