using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    public class UILabel: UIControl
    {
        string Text { get; set; }
        Color Color { get; set; } = UITheme.Ink;
        Vector2 Position { get; set; }
        SpriteFont Font { get; set; }

        /// <summary>Optional text width constraint. Zero means unconstrained.</summary>
        public float MaximumWidth { get; set; }
        public float MinimumScale { get; set; } = 0.45f;

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
            DrawAt(sb, gameTime, Position, 1f);
        }

        public void DrawAt(SpriteBatch sb, GameTime gameTime, Vector2 position, float visualScale)
        {
            if (!IsVisible)
            {
                return; // 如果不可见，则不绘制
            }
            if (string.IsNullOrEmpty(Text))
            {
                return;
            }

            string displayText = UIFontGuard.Sanitize(Font, Text);
            var textSize = Font.MeasureString(displayText);
            float scale = MaximumWidth > 0 && textSize.X > MaximumWidth
                ? MathHelper.Max(MinimumScale, MaximumWidth / textSize.X)
                : 1f;
            scale *= visualScale;
            var pos = new Vector2(
                position.X - textSize.X * scale / 2,
                position.Y - textSize.Y * scale / 2
            );
            sb.DrawString(Font, displayText, pos, Color, 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0f);
        }



        public override void Update(string newText)
        {
            Text = newText;
        }



    }
}
