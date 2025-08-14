using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    // 定义一个按钮点击事件的委托。
    // 这定义了可以订阅该事件的方法的签名。
    public delegate void ButtonClickedHandler(object sender);

    public class UIButton : UIControl
    {
        // 定义一个事件，当按钮被点击时会触发。
        // 它使用了上面定义的委托类型。
        public event ButtonClickedHandler OnClick;

        // 按钮的边界矩形。
        private Rectangle Bounds { get; set; }
        // 按钮上显示的文本。
        private string Text { get; set; }

        // 按钮文本颜色。
        public Color FrontColor { get; set; } = Color.White;
        // 按钮背景颜色（非悬停/按下状态）。
        public Color BackColor { get; set; } = Color.Black;
        // 鼠标悬停在按钮上时的背景颜色。
        public Color HoverColor { get; set; } = Color.Gray;
        // 鼠标按下按钮时的背景颜色。
        public Color PressedColor { get; set; } = Color.DarkGray;

        // 按钮是否启用。
        private bool IsEnabled { get; set; } = false;
        // 鼠标是否悬停在按钮上。
        private bool IsHovered { get; set; } = false;

        // 用于绘制背景的1x1像素纹理。
        private Texture2D Pixel { get; set; }
        // 按钮文本的字体。
        private SpriteFont Font { get; set; }
        // 按钮是否被选中，此版本代码中未使用。
        private bool IsSelected { get; set; } = false;

        // 我们需要存储上一帧的鼠标状态，以检测“点击”（按下后释放）事件。
        private MouseState _previousMouseState;

        public UIButton(Rectangle bounds, string text, Texture2D pixel, SpriteFont font)
        {
            Bounds = bounds;
            Text = text;
            Pixel = pixel;
            Font = font;
        }

        public override void Draw(SpriteBatch sb)
        {
            // 根据按钮的当前状态确定背景颜色。
            Color currentBackColor = BackColor;
            if (!IsEnabled)
            {
                currentBackColor = Color.DimGray;
            }
            // 使用上一帧的鼠标状态来检查是否按下，因为这一帧的Update尚未执行。
            else if (_previousMouseState.LeftButton == ButtonState.Pressed && IsHovered)
            {
                currentBackColor = PressedColor;
            }
            else if (IsHovered)
            {
                currentBackColor = HoverColor;
            }

            // 绘制背景。
            sb.Draw(Pixel, Bounds, currentBackColor);

            // 绘制文本。
            var size = Font.MeasureString(Text);
            var pos = new Vector2(
                Bounds.Center.X - size.X / 2,
                Bounds.Center.Y - size.Y / 2
            );
            // 修复：添加了位置参数 pos。
            sb.DrawString(Font, Text, pos, FrontColor);

            // 如果鼠标悬停在按钮上，绘制一个白色的边框。
            if (IsEnabled && IsHovered)
            {
                UIUtils.DrawRectangleOutlines(sb, Bounds, Color.White, 2, Pixel);
            }
        }

        public override void Update(MouseState mouseState)
        {
            // 首先，更新鼠标悬停状态。
            IsHovered = Bounds.Contains(mouseState.Position);

            // 如果按钮未启用，则不处理点击事件。
            if (!IsEnabled)
            {
                IsHovered = false;
                // 更新上一帧状态，以防按钮被禁用时，鼠标恰好被按下。
                _previousMouseState = mouseState;
                return;
            }

            // --- 使用事件的点击检测逻辑 ---
            // 一个“点击”发生在左键从“按下”状态（上一帧）变为“释放”状态（当前帧）时，
            // 并且鼠标指针在此期间一直悬停在按钮上。
            if (mouseState.LeftButton == ButtonState.Released &&
                _previousMouseState.LeftButton == ButtonState.Pressed &&
                IsHovered)
            {
                // 如果检测到点击，就触发事件。
                // '?' 是空条件运算符，它可以防止在没有方法订阅事件时发生错误。
                OnClick?.Invoke(this);
            }

            // 重要的：将当前的鼠标状态存储起来，供下一帧的更新周期使用。
            _previousMouseState = mouseState;
        }

        public void UpdateText(string text)
        {
            Text = text;
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
    }
}
