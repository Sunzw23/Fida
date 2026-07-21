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
