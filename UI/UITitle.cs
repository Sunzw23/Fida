// UITitle.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// 菜单大标题控件：无衬线高对比标题与分段霓虹信号线。
    /// 非交互控件（不可聚焦、无鼠标逻辑），仅用于菜单屏的视觉层级。
    /// </summary>
    public class UITitle : UIControl
    {
        private string _text;
        private Vector2 _position;
        private Texture2D _pixel;
        private SpriteFont _font;

        public UITitle(int id, string text, Vector2 position, Texture2D pixel, SpriteFont titleFont) : base(id)
        {
            _text = text;
            _position = position;
            _pixel = pixel;
            _font = titleFont;
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            if (!IsVisible) return;
            if (string.IsNullOrEmpty(_text)) return;

            string displayText = UIFontGuard.Sanitize(_font, _text);
            var (rect, size, scale) = ComputeBounds(displayText, sb.GraphicsDevice.Viewport);
            UITextRenderer.DrawString(sb, _font, displayText, new Vector2(rect.Left, rect.Top), UITheme.Ink, 0f, Vector2.Zero,
                scale, SpriteEffects.None, 0f);

            float ruleW = MathHelper.Max(90f, size.X * 0.42f);
            float ruleX = rect.Center.X - ruleW / 2f;
            float ruleY = rect.Bottom + 6f;
            sb.Draw(_pixel, new Rectangle((int)ruleX, (int)ruleY, (int)ruleW, 4),
                UITheme.Line);
        }

        public override Rectangle? GetScreenBounds(Viewport viewport)
        {
            if (!IsVisible || string.IsNullOrEmpty(_text)) return null;
            string displayText = UIFontGuard.Sanitize(_font, _text);
            return ComputeBounds(displayText, viewport).Rect;
        }

        /// <summary>
        /// 标题文本的实际绘制范围：按面板尺寸缩放后夹在面板内。
        /// Draw 与 GetScreenBounds 共用同一份几何计算。
        /// </summary>
        private (Rectangle Rect, Vector2 Size, float Scale) ComputeBounds(string displayText, Viewport viewport)
        {
            var size = _font.MeasureString(displayText);
            Rectangle panel = UITheme.PanelRect(viewport);
            float layoutScale = UIDesktopLayout.Scale(viewport);
            float maxWidth = MathHelper.Max(1, panel.Width - 120);
            float maxHeight = MathHelper.Max(1, panel.Height * 0.13f);
            float scale = MathHelper.Min(1f, MathHelper.Min(
                maxWidth / MathHelper.Max(1, size.X),
                maxHeight / MathHelper.Max(1, size.Y)));
            Vector2 displaySize = size * scale;
            float centerX = MathHelper.Clamp(_position.X,
                panel.Left + displaySize.X / 2 + 24 * layoutScale, panel.Right - displaySize.X / 2 - 24 * layoutScale);
            float centerY = MathHelper.Clamp(_position.Y,
                panel.Top + displaySize.Y / 2 + 28 * layoutScale, panel.Bottom - displaySize.Y / 2 - 28 * layoutScale);
            var pos = new Vector2(centerX - displaySize.X / 2, centerY - displaySize.Y / 2);
            return (new Rectangle((int)pos.X, (int)pos.Y,
                System.Math.Max(1, (int)System.Math.Round(displaySize.X)),
                System.Math.Max(1, (int)System.Math.Round(displaySize.Y))), displaySize, scale);
        }
    }
}
