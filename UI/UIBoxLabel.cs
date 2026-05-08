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
    /// 带边框的标签控件，支持左/中/右对齐和擦除动画
    /// </summary>
    public class UIBoxLabel : UIControl
    {
        private Rectangle Bounds { get; set; }
        private string Text { get; set; }
        private Texture2D Pixel { get; set; }
        private SpriteFont Font { get; set; }

        // 配色
        public Color BackColor { get; set; } = Color.Black;
        public Color ForeColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.White;
        public int BorderThickness { get; set; } = 1;
        public UIBoxLabelAlignment Alignment { get; set; } = UIBoxLabelAlignment.Center;

        // 动画相关（与 UIAnimatedButton 一致的擦除动画）
        private bool _isAnimating;
        private float _animationTimer;
        private float _animationStartTime;
        private const float AnimationDuration = 0.2f;
        private string _newText;
        private float _wipeProgress;

        // 缓存 MeasureString 结果，避免每帧重复计算
        private Vector2 _cachedTextSize;
        private Vector2 _cachedTextPos;
        private bool _textSizeDirty = true;

        public UIBoxLabel(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font)
            : base(id)
        {
            Bounds = bounds;
            Text = text;
            Pixel = pixel;
            Font = font;
        }

        /// <summary>
        /// 每帧逻辑更新 - 处理动画计时（与渲染分离）
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            if (!_isAnimating)
                return;

            if (_animationStartTime == 0f)
            {
                _animationStartTime = (float)gameTime.TotalGameTime.TotalSeconds;
            }

            _animationTimer = (float)gameTime.TotalGameTime.TotalSeconds - _animationStartTime;

            if (_animationTimer >= AnimationDuration)
            {
                _isAnimating = false;
                _wipeProgress = 1f;
                Text = _newText;
                _textSizeDirty = true;
            }
            else
            {
                _wipeProgress = _animationTimer / AnimationDuration;
            }
        }

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

            // 绘制文字（居中对齐，使用缓存坐标）
            if (!string.IsNullOrEmpty(Text))
            {
                sb.DrawString(Font, Text, _cachedTextPos, ForeColor);
            }

            // 绘制边框
            if (BorderThickness > 0)
            {
                UIUtils.DrawRectangleOutlines(sb, Bounds, BorderColor, BorderThickness, Pixel);
            }

            // 绘制擦除动画块（从左到右扫过）
            if (_isAnimating)
            {
                int wipeWidth = (int)(Bounds.Width * _wipeProgress);
                var wipeRect = new Rectangle(
                    Bounds.X,
                    Bounds.Y,
                    wipeWidth,
                    Bounds.Height
                );
                sb.Draw(Pixel, wipeRect, BackColor);
            }
        }

        /// <summary>
        /// 更新文本（触发擦除动画）
        /// 如果动画进行中且目标值再次变化，重置动画以确保最终显示最新值
        /// </summary>
        public override void Update(string newText)
        {
            if (newText != Text)
            {
                // 动画进行中且目标值又变了 → 重置动画
                if (_isAnimating && newText != _newText)
                {
                    _animationTimer = 0f;
                    _animationStartTime = 0f;
                    _wipeProgress = 0f;
                }
                _newText = newText;
                if (!_isAnimating)
                {
                    _isAnimating = true;
                    _animationTimer = 0f;
                    _wipeProgress = 0f;
                    _animationStartTime = 0f;
                }
            }
        }

        /// <summary>
        /// 直接设置文本（不触发动画）
        /// </summary>
        public void SetText(string text)
        {
            Text = text;
            _isAnimating = false;
            _wipeProgress = 1f;
            _textSizeDirty = true;
        }

        /// <summary>
        /// 立即刷新 - 如果有进行中的动画，直接应用最新值并取消动画
        /// 用于"一键刷新所有控件"场景
        /// </summary>
        public void Refresh()
        {
            if (_isAnimating)
            {
                Text = _newText;
                _isAnimating = false;
                _wipeProgress = 1f;
                _textSizeDirty = true;
            }
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