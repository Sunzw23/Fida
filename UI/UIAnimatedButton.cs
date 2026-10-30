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
        private string _cachedDisplayText = "";
        private bool _textSizeDirty = true;

        /// <summary>Card-owned buttons remain interactive controls but are drawn by their parent card.</summary>
        public bool ParentDrawOnly { get; set; }
        public bool DarkTileStyle { get; set; }

        public Rectangle LayoutBounds
        {
            get => Bounds;
            set
            {
                Bounds = value;
                _textSizeDirty = true;
            }
        }

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
            UpdateMotion(gameTime);
            if (UIMotion.Reduced) Refresh();
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
            if (ParentDrawOnly) return;
            DrawInternal(sb, gameTime);
        }

        public void DrawAt(SpriteBatch sb, GameTime gameTime, Rectangle bounds)
        {
            Rectangle original = Bounds;
            Bounds = bounds;
            _textSizeDirty = true;
            DrawInternal(sb, gameTime);
            Bounds = original;
            _textSizeDirty = true;
        }

        private void DrawInternal(SpriteBatch sb, GameTime gameTime)
        {
            if (!IsVisible) return; // 不可见时不绘制（数字面板非 ChooseNumber 步骤时隐藏）
            if (DarkTileStyle || Text?.Length == 1 && char.IsDigit(Text[0]))
            {
                DrawNumberTile(sb);
                return;
            }
            // 更新文字尺寸缓存
            if (_textSizeDirty)
            {
                if (!string.IsNullOrEmpty(Text))
                {
                    _cachedDisplayText = UIFontGuard.Sanitize(Font, Text);
                    _cachedTextSize = Font.MeasureString(_cachedDisplayText);
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
            if (DarkTileStyle)
            {
                backColor = State == UIAnimatedButtonState.Selected
                    ? UITheme.AccentBlue : UITheme.Ink;
                foreColor = UITheme.PanelLight;
                borderColor = State == UIAnimatedButtonState.Selected
                    ? UITheme.AccentGreen : UITheme.Ink;
                borderThickness = Math.Max(1, borderThickness);
            }

            // 绘制背景
            UIPaper.Sheet(sb, Bounds, backColor, _isPressed ? 0 : 3, 7);

            bool emphasized = State is UIAnimatedButtonState.Hovered or
                UIAnimatedButtonState.Pressed or UIAnimatedButtonState.Selected;
            Color accent = State == UIAnimatedButtonState.Selected
                ? UITheme.AccentGreen : UITheme.Gold;
            if (!DarkTileStyle)
                sb.Draw(Pixel, new Rectangle(Bounds.Left, Bounds.Top, Bounds.Width, 4), accent);

            // 绘制文字（居中，使用缓存坐标）
            if (!string.IsNullOrEmpty(Text))
            {
                float availableWidth = MathHelper.Max(1, Bounds.Width - 24);
                float availableHeight = MathHelper.Max(1, Bounds.Height - 16);
                float preferredScale = 1f;
#if !FIDA_ANDROID
                preferredScale = UIDesktopLayout.Scale(sb.GraphicsDevice.Viewport);
#endif
                float textScale = UITextRenderer.FitScale(_cachedTextSize, availableWidth, availableHeight, preferredScale);
                Vector2 textPos = new(
                    Bounds.Center.X - _cachedTextSize.X * textScale / 2,
                    Bounds.Center.Y - _cachedTextSize.Y * textScale / 2 +
                    (State == UIAnimatedButtonState.Pressed ? 2f : 0f));
                UITextRenderer.DrawString(sb, Font, _cachedDisplayText, textPos, foreColor,
                    0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            }

            // 绘制边框（禁用状态 borderThickness=0，不绘制）
            if (borderThickness > 0)
            {
                UIUtils.DrawRectangleOutlines(sb, Bounds,
                    State == UIAnimatedButtonState.Selected ? UITheme.AccentGreen : borderColor,
                    emphasized ? Math.Max(2, borderThickness) : borderThickness, Pixel);
            }

            // A locked first number remains visibly alive while the opponent
            // considers the second number, making the two-beat action readable.
            if (State == UIAnimatedButtonState.Selected)
            {
                float pulse = UIMotion.Pulse(gameTime.TotalGameTime.TotalSeconds, 3f);
                int expansion = 3 + (int)(pulse * 3);
                var pulseBounds = new Rectangle(
                    Bounds.X - expansion,
                    Bounds.Y - expansion,
                    Bounds.Width + expansion * 2,
                    Bounds.Height + expansion * 2);
                UIUtils.DrawRectangleOutlines(sb, pulseBounds,
                    Color.Lerp(UITheme.Gold, UITheme.GoldBright, pulse), 2, Pixel);
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
                sb.Draw(Pixel, wipeRect, DarkTileStyle ? UITheme.InkMuted : UITheme.Panel);
            }
        }

        private void DrawNumberTile(SpriteBatch sb)
        {
            bool selected = State == UIAnimatedButtonState.Selected;
            bool hover = State == UIAnimatedButtonState.Hovered;
            bool pressed = State == UIAnimatedButtonState.Pressed;
            bool disabled = State == UIAnimatedButtonState.Disabled;
            Color fill = selected ? UITheme.Gold : Color.Lerp(UIPaper.Face, UITheme.Gold, HoverAmount * .13f);
            if (disabled) fill = Color.Lerp(UIPaper.Face, UITheme.PanelLight, .22f);
            Color foreground = disabled ? UIPaper.MutedInk : UIPaper.Ink;
            var backing = Bounds; backing.Offset(-3, 3);
            UIPaper.Sheet(sb, backing, UITheme.PanelHover, 1, 8);
            UIPaper.Sheet(sb, Bounds, fill, pressed ? 0 : 3, Math.Max(5, Bounds.Width / 18));
            int horizontal = Math.Max(8, Bounds.Width / 5);
            int vertical = Math.Max(7, Bounds.Height / 6);
            var glyphBounds = new Rectangle(Bounds.Left + horizontal, Bounds.Top + vertical,
                Math.Max(1, Bounds.Width - 2 * horizontal), Math.Max(1, Bounds.Height - 2 * vertical));
            if (pressed) glyphBounds.Y += Math.Max(1, Bounds.Height / 50);
            if (!UIAngularNumber.Draw(sb, Text, glyphBounds, foreground))
                UIUtils.DrawCenteredFit(sb, Font, Text, Bounds.Center.ToVector2(), Bounds.Width - 16, 1, foreground);
            // A local wipe signals a changed value without moving its input target.
            if (_isAnimating)
            {
                int x = Bounds.Left + (int)(Bounds.Width * _wipeProgress);
                sb.Draw(Pixel, new Rectangle(x, Bounds.Top, Math.Max(1, Bounds.Width / 40), Bounds.Height),
                    selected ? UITheme.BgTop * 0.5f : UITheme.Gold * 0.65f);
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
