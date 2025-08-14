using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace Fida.UI
{
    public class UIButton: UIControl
    {
        private Rectangle Bounds { get; set; }
        private string Text { get; set; }
        private Color FrontColor { get; set; } = Color.White;
        private Color BackColor { get; set; } = Color.Black;
        private bool IsEnabled { get; set; } = false;
        private bool IsHovered { get; set; } = false;
        private bool IsPressed { get; set; } = false;
        private Texture2D Pixel { get; set; }
        private SpriteFont Font { get; set; }
        private bool IsSelected { get; set; } = false;

        public UIButton(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font): base(id)
        {
            Bounds = bounds;
            Text = text;
            Pixel = pixel;
            Font = font;
        }


        public override void Draw(SpriteBatch sb)
        {
            // 绘制背景
            sb.Draw(Pixel, Bounds, BackColor);
            var size = Font.MeasureString(Text);
            var pos = new Vector2(
                Bounds.Center.X - size.X / 2,
                Bounds.Center.Y - size.Y / 2
            );
            sb.DrawString(Font, Text, pos, FrontColor);
            if (!IsEnabled) return;
            if (IsHovered)
            {
                UIUtils.DrawRectangleOutlines(sb, Bounds, Color.White, 2, Pixel);
            }
        }

        public override void Update(MouseState mouseState)
        {
            if (!IsEnabled)
            {
                IsHovered = false;
                IsPressed = false;
                return;
            }
            var mousePoint = new Point(mouseState.X, mouseState.Y);
            
            IsHovered = Contains(mousePoint);

            if (IsHovered && mouseState.LeftButton == ButtonState.Pressed)
            {
                IsPressed = true;
            }
            else
            {
                IsPressed = false;
            }
        }

        public override void Update(string text)
        {
            Text = text;
        }

        public bool IsButtonPressed()
        {
            return IsPressed;
        }

        public void Enable()
        {
            IsEnabled = true;
        }

        public void Disable()
        {
            IsEnabled = false;
            IsHovered = false;
            IsSelected = false;
        }

        public bool Contains(Point point) => Bounds.Contains(point);
        public bool Contains(int x, int y) => Bounds.Contains(x, y);

        
    }
}
