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
            var size = _font.MeasureString(displayText);
            Rectangle panel = UITheme.PanelRect(sb.GraphicsDevice.Viewport);
            float maxWidth = MathHelper.Max(1, panel.Width - 120);
            float maxHeight = MathHelper.Max(1, panel.Height * 0.13f);
            float scale = MathHelper.Min(1f, MathHelper.Min(
                maxWidth / MathHelper.Max(1, size.X),
                maxHeight / MathHelper.Max(1, size.Y)));
            Vector2 displaySize = size * scale;
            float centerX = MathHelper.Clamp(_position.X,
                panel.Left + displaySize.X / 2 + 24, panel.Right - displaySize.X / 2 - 24);
            float centerY = MathHelper.Clamp(_position.Y,
                panel.Top + displaySize.Y / 2 + 28, panel.Bottom - displaySize.Y / 2 - 28);
            var pos = new Vector2(centerX - displaySize.X / 2, centerY - displaySize.Y / 2);
            sb.DrawString(_font, displayText, pos, UITheme.Ink, 0f, Vector2.Zero,
                scale, SpriteEffects.None, 0f);

            float ruleW = MathHelper.Max(90f, displaySize.X * 0.42f);
            float ruleX = centerX - ruleW / 2;
            float ruleY = centerY + displaySize.Y / 2 + 6f;
            sb.Draw(_pixel, new Rectangle((int)ruleX, (int)ruleY, (int)ruleW, 4),
                UITheme.Gold);
        }
    }
}
