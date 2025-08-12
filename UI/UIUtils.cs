using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

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
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Bottom, rect.Width, width), color); // Bottom
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Top, width, rect.Height), color); // Left
            sb.Draw(pixel, new Rectangle(rect.Right, rect.Top, width, rect.Height + width), color); // Right
        }
    }
}
