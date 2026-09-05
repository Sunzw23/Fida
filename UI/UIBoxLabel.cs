using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    public enum UIBoxLabelAlignment
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// 带边框的标签控件，支持左/中/右对齐
    /// </summary>
    public class UIBoxLabel : UIControl
    {
        protected Rectangle Bounds { get; set; }
        protected string Text { get; set; }
        protected Texture2D Pixel { get; set; }
        protected SpriteFont Font { get; set; }

        // 配色
        public Color BackColor { get; set; } = UITheme.PanelDark;
        public Color ForeColor { get; set; } = UITheme.Ink;
        public Color BorderColor { get; set; } = UITheme.Gold;
        public int BorderThickness { get; set; } = 1;
        public UIBoxLabelAlignment Alignment { get; set; } = UIBoxLabelAlignment.Center;

        // 缓存 MeasureString 结果，避免每帧重复计算
        protected Vector2 _cachedTextSize;
        protected Vector2 _cachedTextPos;
        private bool _textSizeDirty = true;

        public UIBoxLabel(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font)
            : base(id)
        {
            Bounds = bounds;
            Text = text;
            Pixel = pixel;
            Font = font;
        }

        public override void Update(GameTime gameTime) { }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            // 更新文字尺寸缓存
            if (_textSizeDirty)
            {
                if (!string.IsNullOrEmpty(Text))
                {
                    _cachedTextSize = Font.MeasureString(Text);
                    float x = Alignment switch
                    {
                        UIBoxLabelAlignment.Left => Bounds.X + BorderThickness + 2,
                        UIBoxLabelAlignment.Right => Bounds.Right - _cachedTextSize.X - BorderThickness - 2,
                        _ => Bounds.Center.X - _cachedTextSize.X / 2
                    };
                    _cachedTextPos = new Vector2(x, Bounds.Center.Y - _cachedTextSize.Y / 2);
                }
                _textSizeDirty = false;
            }

            // 绘制背景
            sb.Draw(Pixel, Bounds, BackColor);

            // 绘制文字（使用缓存坐标）
            if (!string.IsNullOrEmpty(Text))
            {
                sb.DrawString(Font, Text, _cachedTextPos, ForeColor);
            }

            // 绘制边框
            if (BorderThickness > 0)
            {
                UIUtils.DrawRectangleOutlines(sb, Bounds, BorderColor, BorderThickness, Pixel);
            }
        }

        public override void Update(string newText)
        {
            Text = newText;
            _textSizeDirty = true;
        }

        /// <summary>
        /// 直接设置文本
        /// </summary>
        public void SetText(string text)
        {
            Text = text;
            _textSizeDirty = true;
        }

        /// <summary>
        /// 立即刷新 - 虚方法，子类可重写以取消动画
        /// </summary>
        public virtual void Refresh()
        {
        }

        /// <summary>
        /// 获取当前显示的文本
        /// </summary>
        public string GetText()
        {
            return Text;
        }
    }
}