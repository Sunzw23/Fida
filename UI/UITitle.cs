// UITitle.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// 菜单大标题控件：用衬线字体（TitleFont）居中绘制，下方配一条描金短横线。
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

            // 居中绘制衬线标题
            var size = _font.MeasureString(_text);
            var pos = new Vector2(_position.X - size.X / 2, _position.Y - size.Y / 2);
            sb.DrawString(_font, _text, pos, UITheme.Ink);

            // 标题下方的描金装饰短线（宽约为文字宽度的一半，居中）
            float ruleW = size.X * 0.5f;
            float ruleX = _position.X - ruleW / 2;
            float ruleY = _position.Y + size.Y / 2 + 6f;
            sb.Draw(_pixel, new Rectangle((int)ruleX, (int)ruleY, (int)ruleW, 2), UITheme.Gold);
        }
    }
}
