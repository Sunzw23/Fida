using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// 带擦除动画的标签控件，继承自 UIBoxLabel
    /// </summary>
    public class AnimatedBoxLabel : UIBoxLabel
    {
        private bool _isAnimating;
        private float _animationTimer;
        private float _animationStartTime;
        private const float AnimationDuration = 0.2f;
        private string _newText;
        private float _wipeProgress;

        public AnimatedBoxLabel(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font)
            : base(id, bounds, text, pixel, font)
        {
        }

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
                SetText(_newText);
            }
            else
            {
                _wipeProgress = _animationTimer / AnimationDuration;
            }
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            base.Draw(sb, gameTime);

            // 绘制擦除动画块（从左到右扫过，仅覆盖文字区域，不覆盖边框）
            if (_isAnimating && !string.IsNullOrEmpty(Text))
            {
                int wipeWidth = (int)(_cachedTextSize.X * _wipeProgress);
                var wipeRect = new Rectangle(
                    (int)_cachedTextPos.X,
                    (int)_cachedTextPos.Y,
                    wipeWidth,
                    (int)_cachedTextSize.Y
                );
                sb.Draw(Pixel, wipeRect, BackColor);
            }
        }

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

        public override void Refresh()
        {
            if (_isAnimating)
            {
                SetText(_newText);
                _isAnimating = false;
                _wipeProgress = 1f;
            }
        }

    }
}