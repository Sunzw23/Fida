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

        public UILabel(int id, string text, Vector2 position, SpriteFont font) : base(id)
        {
            Text = text;
            Position = position;
            Font = font;
        }

        public void SetColor(Color color)
        {
            Color = color;
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            if (!IsVisible)
            {
                return; // 如果不可见，则不绘制
            }
            if (string.IsNullOrEmpty(Text))
            {
                return;
            }

            var textSize = Font.MeasureString(Text);
            var pos = new Vector2(
                Position.X - textSize.X / 2,
                Position.Y - textSize.Y / 2
            );
            sb.DrawString(Font, Text, pos, Color);
        }



        public override void Update(string newText)
        {
            Text = newText;
        }



    }
}
