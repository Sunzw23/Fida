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
        public bool AutoFitText { get; set; }
        public float MinimumTextScale { get; set; } = 0.55f;
        public float MaximumTextScale { get; set; } = 1f;
        public int TextPadding { get; set; } = 12;
        public bool AccentBar { get; set; }
        /// <summary>Draws bracket-like signal rails for the primary battle command HUD.</summary>
        public bool SignalRails { get; set; }

        // 缓存 MeasureString 结果，避免每帧重复计算
        protected Vector2 _cachedTextSize;
        protected Vector2 _cachedTextPos;
        protected string _cachedDisplayText = "";
        protected float _cachedTextScale = 1f;
        private bool _textSizeDirty = true;

        public UIBoxLabel(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font)
            : base(id)
        {
            Bounds = bounds;
            Text = text;
            Pixel = pixel;
            Font = font;
        }

        public override Rectangle? GetScreenBounds(Viewport viewport) =>
            IsVisible ? Bounds : null;

        public override void Update(GameTime gameTime) { }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            if (!IsVisible) return;

            // 更新文字尺寸缓存
            if (_textSizeDirty)
            {
                if (!string.IsNullOrEmpty(Text))
                {
                    _cachedDisplayText = UIFontGuard.Sanitize(Font, Text);
                    _cachedTextSize = Font.MeasureString(_cachedDisplayText);
                    float availableWidth = MathHelper.Max(1, Bounds.Width - TextPadding * 2);
                    float availableHeight = MathHelper.Max(1, Bounds.Height - TextPadding * 2);
                    _cachedTextScale = AutoFitText
                        ? MathHelper.Clamp(MathHelper.Min(
                            availableWidth / MathHelper.Max(1, _cachedTextSize.X),
                            availableHeight / MathHelper.Max(1, _cachedTextSize.Y)),
                            MinimumTextScale, MaximumTextScale)
                        : 1f;
#if !FIDA_ANDROID
                    _cachedTextScale *= UIDesktopLayout.Scale(sb.GraphicsDevice.Viewport);
#endif
                    Vector2 displaySize = _cachedTextSize * _cachedTextScale;
                    float x = Alignment switch
                    {
                        UIBoxLabelAlignment.Left => Bounds.X + TextPadding,
                        UIBoxLabelAlignment.Right => Bounds.Right - displaySize.X - TextPadding,
                        _ => Bounds.Center.X - displaySize.X / 2
                    };
                    _cachedTextPos = new Vector2(x, Bounds.Center.Y - displaySize.Y / 2);
                }
                _textSizeDirty = false;
            }

            // 绘制背景
            sb.Draw(Pixel, Bounds, BackColor);
            if (AccentBar)
                sb.Draw(Pixel, new Rectangle(Bounds.Left, Bounds.Top, 6, Bounds.Height),
                    BorderColor);

            // 绘制文字（使用缓存坐标）
            if (!string.IsNullOrEmpty(Text))
            {
                sb.DrawString(Font, _cachedDisplayText, _cachedTextPos, ForeColor,
                    0f, Vector2.Zero, _cachedTextScale, SpriteEffects.None, 0f);
            }

            // 绘制边框
            if (BorderThickness > 0)
            {
                if (BorderThickness >= 2)
                    UIUtils.DrawGlowFrame(sb, Bounds, Pixel, BorderColor,
                        BorderThickness, 0.55f);
                else
                    UIUtils.DrawRectangleOutlines(sb, Bounds, BorderColor,
                        BorderThickness, Pixel);
            }

            if (SignalRails)
            {
                int rail = System.Math.Clamp(Bounds.Height / 3, 16, 30);
                int line = System.Math.Clamp(Bounds.Width / 12, 28, 64);
                sb.Draw(Pixel, new Rectangle(Bounds.Left, Bounds.Center.Y - rail, 3, rail * 2),
                    UITheme.GoldBright);
                sb.Draw(Pixel, new Rectangle(Bounds.Left + 3, Bounds.Center.Y, line, 2),
                    UITheme.GoldBright);
                sb.Draw(Pixel, new Rectangle(Bounds.Right - 3, Bounds.Center.Y - rail, 3, rail * 2),
                    UITheme.GoldBright);
                sb.Draw(Pixel, new Rectangle(Bounds.Right - line - 3, Bounds.Center.Y, line, 2),
                    UITheme.GoldBright);
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
