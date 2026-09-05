using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Fida.UI
{
    /// <summary>
    /// 按钮状态枚举
    /// </summary>
    public enum UIAnimatedButtonState
    {
        Normal,     // 可用状态 - 静态，小边框
        Disabled,   // 禁用状态 - 静态，无边框
        Hovered,    // 鼠标悬浮/手柄聚焦 - 静态，大边框
        Pressed,    // 按下状态 - 双层边框
        Selected    // 已选择状态 - 双层边框
    }

    public class UIAnimatedButton : UIButtonBase<UIAnimatedButtonState>
    {
        // 按钮状态（优先级：Selected > Disabled > Pressed > Hovered > Normal）
        // Selected 优先级高于 Disabled，允许已选中的按钮在禁用状态下仍显示为选中样式
        protected override UIAnimatedButtonState GetState()
        {
            if (IsSelected) return UIAnimatedButtonState.Selected;
            if (!IsEnabled) return UIAnimatedButtonState.Disabled;
            if (_isPressed) return UIAnimatedButtonState.Pressed;
            if (IsHovered) return UIAnimatedButtonState.Hovered;
            return UIAnimatedButtonState.Normal;
        }

        protected override bool IsPressedOrSelected() => State == UIAnimatedButtonState.Pressed || State == UIAnimatedButtonState.Selected;
        protected override bool IsDisabled() => State == UIAnimatedButtonState.Disabled;
        protected override bool IsHoveredState() => State == UIAnimatedButtonState.Hovered;

        // 动画相关
        private bool _isAnimating;          // 是否正在播放动画
        private float _animationTimer;      // 动画计时器
        private float _animationStartTime;  // 动画开始时间
        private const float AnimationDuration = 0.2f;  // 动画时长（秒）- 很快
        private string _newText;            // 新文字
        private float _wipeProgress;        // 擦除进度 0-1

        // 缓存 MeasureString 结果，避免每帧重复计算
        private Vector2 _cachedTextSize;
        private Vector2 _cachedTextPos;
        private bool _textSizeDirty = true;

        public UIAnimatedButton(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font) : base(id)
        {
            Bounds = bounds;
            Text = text;
            Pixel = pixel;
            Font = font;
        }

        /// <summary>
        /// 启用→禁用时不清除 IsSelected — 允许已选中的按钮在禁用状态下仍显示为选中样式
        /// </summary>
        protected override void OnDisabledTransition() { /* 保留 IsSelected */ }

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
            if (!IsVisible) return; // 不可见时不绘制（数字面板非 ChooseNumber 步骤时隐藏）
            // 更新文字尺寸缓存
            if (_textSizeDirty)
            {
                if (!string.IsNullOrEmpty(Text))
                {
                    _cachedTextSize = Font.MeasureString(Text);
                    _cachedTextPos = new Vector2(
                        Bounds.Center.X - _cachedTextSize.X / 2,
                        Bounds.Center.Y - _cachedTextSize.Y / 2
                    );
                }
                _textSizeDirty = false;
            }

            Color backColor = GetBackColor();
            Color foreColor = GetForeColor();
            Color borderColor = GetBorderColor();
            int borderThickness = GetBorderThickness();

            // 绘制背景
            sb.Draw(Pixel, Bounds, backColor);

            // 绘制文字（居中，使用缓存坐标）
            if (!string.IsNullOrEmpty(Text))
            {
                sb.DrawString(Font, Text, _cachedTextPos, foreColor);
            }

            // 绘制边框（禁用状态 borderThickness=0，不绘制）
            if (borderThickness > 0)
            {
                if (HasDoubleBorder())
                {
                    // 外层边框（描金）
                    UIUtils.DrawRectangleOutlines(sb, Bounds, UITheme.Gold, borderThickness, Pixel);
                    // 内层边框（深墨），向内偏移
                    var innerBounds = new Rectangle(
                        Bounds.X + borderThickness,
                        Bounds.Y + borderThickness,
                        Bounds.Width - borderThickness * 2,
                        Bounds.Height - borderThickness * 2
                    );
                    UIUtils.DrawRectangleOutlines(sb, innerBounds, UITheme.Ink, 2, Pixel);
                }
                else
                {
                    UIUtils.DrawRectangleOutlines(sb, Bounds, borderColor, borderThickness, Pixel);
                }
            }

            // 绘制白色擦除块（从左到右扫过）
            if (_isAnimating)
            {
                int wipeWidth = (int)(Bounds.Width * _wipeProgress);
                Rectangle wipeRect = new Rectangle(
                    Bounds.X,
                    Bounds.Y,
                    wipeWidth,
                    Bounds.Height
                );
                sb.Draw(Pixel, wipeRect, UITheme.Panel);
            }
        }

        public override void Update(string newText)
        {
            if (newText == Text && !_isAnimating)
                return;

            if (_isAnimating && newText == _newText)
                return;

            _newText = newText;
            _isAnimating = true;
            _animationTimer = 0f;
            _wipeProgress = 0f;
            _animationStartTime = 0f;
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
    }
}
