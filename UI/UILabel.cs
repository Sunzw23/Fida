using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    public class UILabel: UIControl
    {
        string Text { get; set; }
        Color Color { get; set; } = Color.White;
        Vector2 Position { get; set; }
        SpriteFont Font { get; set; }

        public UILabel(string text, Vector2 position, SpriteFont font)
        {
            Text = text;
            Position = position;
            Font = font;
        }

        public override void Draw(SpriteBatch sb)
        {
            if (!IsVisible)
            {
                return; // 如果不可见，则不绘制
            }
            if (string.IsNullOrEmpty(Text))
            {
                return; // 如果文本为空，则不绘制
            }
            var size = Font.MeasureString(Text);
            var pos = new Vector2(
                Position.X - size.X / 2,
                Position.Y - size.Y / 2
            );
            sb.DrawString(Font, Text, pos, Color);
        }



        public void UpdateText(string text)
        {
            Text = text; // 更新文本内容
        }


    }
}
