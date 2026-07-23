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

    public class UIAnimatedButton : UIControl
    {
        private Rectangle Bounds { get; set; }
        private string Text { get; set; }
        private Texture2D Pixel { get; set; }
        private SpriteFont Font { get; set; }

        // 环境检查器 - 按钮主动询问环境来决定自己是否启用
        public Func<bool> EnabledChecker { get; set; }

        // 按钮状态（优先级：Disabled > Selected > Pressed > Hovered > Normal）
        public UIAnimatedButtonState State
        {
            get
            {
                // Selected 优先级高于 Disabled，允许已选中的按钮在禁用状态下仍显示为选中样式
                if (IsSelected) return UIAnimatedButtonState.Selected;
                if (!IsEnabled) return UIAnimatedButtonState.Disabled;
                if (_isPressed) return UIAnimatedButtonState.Pressed;
                if (IsHovered) return UIAnimatedButtonState.Hovered;
                return UIAnimatedButtonState.Normal;
            }
        }

        private bool IsEnabled { get; set; } = true;
        private bool IsHovered { get; set; } = false;
        private bool _isPressed { get; set; } = false;
        private bool IsSelected { get; set; } = false;
        
        // 状态变化回调 - 当按钮状态发生变化时通知外部刷新
        public Action<UIAnimatedButtonState> OnStateChanged { get; set; }
        
        // 记录上一帧的状态用于检测变化
        private UIAnimatedButtonState? _lastState = null;
        
        // 获取当前状态并记录（用于外部直接获取当前状态）
        private UIAnimatedButtonState GetCurrentState()
        {
            var currentState = State;
            _lastState = currentState;
            return currentState;
        }
        
        // 检测状态变化并通知（只在 Update 中自动调用）
        private void CheckStateChanged()
        {
            var currentState = State;
            if (_lastState != currentState)
            {
                _lastState = currentState;
                OnStateChanged?.Invoke(currentState);
            }
        }
        
        // 强制通知状态变化（用于外部方法手动修改状态后调用）
        private void NotifyStateChanged()
        {
            var currentState = GetCurrentState();
            OnStateChanged?.Invoke(currentState);
        }

        // 支持切换模式 - 点击后切换选定状态
        public bool IsToggleMode { get; set; } = false;

        // ─── 手柄焦点支持 ──────────────────────────────────
        /// <summary>
        /// 按钮可以被手柄聚焦
        /// </summary>
        public override bool CanFocus => IsEnabled;

        // 纯黑白配色方案
        private static readonly Color Black = Color.Black;
        private static readonly Color White = Color.White;

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
        /// 根据状态获取背景色
        /// </summary>
        private Color GetBackColor()
        {
            return State switch
            {
                UIAnimatedButtonState.Normal => Black,
                UIAnimatedButtonState.Disabled => Black,
                UIAnimatedButtonState.Hovered => Black,
                UIAnimatedButtonState.Pressed => White,     // 按下：白色背景（反色）
                UIAnimatedButtonState.Selected => White,    // 选定：白色背景
                _ => Black
            };
        }

        /// <summary>
        /// 根据状态获取前景色（文字颜色）
        /// </summary>
        private Color GetForeColor()
        {
            return State switch
            {
                UIAnimatedButtonState.Normal => White,
                UIAnimatedButtonState.Disabled => White,    // 禁用：白色文字（可见但不可交互）
                UIAnimatedButtonState.Hovered => White,
                UIAnimatedButtonState.Pressed => Black,     // 按下：黑色文字（反色效果）
                UIAnimatedButtonState.Selected => Black,    // 选定：黑色文字
                _ => White
            };
        }

        /// <summary>
        /// 根据状态获取边框颜色
        /// </summary>
        private Color GetBorderColor()
        {
            return State switch
            {
                UIAnimatedButtonState.Normal => White,
                UIAnimatedButtonState.Disabled => Black,    // 禁用：黑色边框（与背景融合 = 无边框）
                UIAnimatedButtonState.Hovered => White,
                UIAnimatedButtonState.Pressed => White,
                UIAnimatedButtonState.Selected => White,
                _ => White
            };
        }

        /// <summary>
        /// 根据状态获取边框厚度
        /// </summary>
        private int GetBorderThickness()
        {
            return State switch
            {
                UIAnimatedButtonState.Normal => 1,          // 可用：小边框
                UIAnimatedButtonState.Disabled => 0,        // 禁用：无边框
                UIAnimatedButtonState.Hovered => 4,         // 悬浮：大边框
                UIAnimatedButtonState.Pressed => 2,         // 按下：中等边框（双层）
                UIAnimatedButtonState.Selected => 2,        // 选定：中等边框（双层）
                _ => 1
            };
        }

        /// <summary>
        /// 是否需要绘制双层边框 - 按下和选定状态
        /// </summary>
        private bool HasDoubleBorder()
        {
            return State == UIAnimatedButtonState.Pressed || State == UIAnimatedButtonState.Selected;
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
                    // 外层边框（白色）
                    UIUtils.DrawRectangleOutlines(sb, Bounds, White, borderThickness, Pixel);
                    // 内层边框（黑色），向内偏移
                    var innerBounds = new Rectangle(
                        Bounds.X + borderThickness,
                        Bounds.Y + borderThickness,
                        Bounds.Width - borderThickness * 2,
                        Bounds.Height - borderThickness * 2
                    );
                    UIUtils.DrawRectangleOutlines(sb, innerBounds, Black, 2, Pixel);
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
                sb.Draw(Pixel, wipeRect, Color.White);
            }
        }

        // ---- 鼠标交互状态机 ----
        // _clickStartedOnThis: 当前点击序列是否起始于此按钮
        // _mouseWasDown: 上一帧鼠标左键是否按下
        private bool _clickStartedOnThis = false;
        private bool _mouseWasDown = false;

        public override void Update(MouseState mouseState)
        {
            bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
            var mousePoint = new Point(mouseState.X, mouseState.Y);

            // 按钮主动询问环境：我现在应该启用吗？
            bool shouldBeEnabled = EnabledChecker?.Invoke() ?? true;
            
            // 如果按钮从启用变为禁用，清除交互状态但保留 Selected（用于显示已选择的数字）
            if (IsEnabled && !shouldBeEnabled)
            {
                IsEnabled = false;
                IsHovered = false;
                _isPressed = false;
                _clickStartedOnThis = false;
                _mouseWasDown = mouseDown;
                // 不清除 IsSelected — 允许已选中的按钮在禁用状态下仍显示为选中样式
                NotifyStateChanged();
                return;
            }
            
            // 如果按钮从禁用变为启用，需要通知状态变化
            if (!IsEnabled && shouldBeEnabled)
            {
                IsEnabled = true;
                _mouseWasDown = mouseDown;
            // 启用时恢复悬浮状态检测
                IsHovered = Contains(mousePoint) || IsFocused;
                IsSelected = false; // 启用时默认不选定
                NotifyStateChanged();
                return;
            }
            
            IsEnabled = shouldBeEnabled;

            if (!IsEnabled)
            {
                IsHovered = false;
                _isPressed = false;
                _clickStartedOnThis = false;
                _mouseWasDown = mouseDown;
                NotifyStateChanged();
                return;
            }

            bool wasHovered = IsHovered;
            bool wasPressed = _isPressed;
            IsHovered = Contains(mousePoint) || IsFocused;

            // 鼠标刚在此按钮上按下
            if (IsHovered && mouseDown && !_mouseWasDown)
            {
                _isPressed = true;
                _clickStartedOnThis = true;
            }

            // 鼠标释放：重置按下状态
            if (!mouseDown && _mouseWasDown)
            {
                _isPressed = false;
            }

            // 鼠标移出按钮（拖拽取消）：取消本次点击
            if (!IsHovered && mouseDown)
            {
                _isPressed = false;
                _clickStartedOnThis = false;
            }

            _mouseWasDown = mouseDown;
            
            // 检测状态变化（Hovered、Pressed 变化都需要通知）
            CheckStateChanged();
        }

        public override void Update(string newText)
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

        /// <summary>
        /// 检测按钮是否完成了一次完整点击（按下 + 释放均在按钮上）
        /// 调用后状态会被消费（返回 true 后再次调用返回 false）
        /// </summary>
        public bool IsClicked()
        {
            if (!IsEnabled) return false;

            // 完整点击：起始于此按钮 + 已释放 + 仍在按钮上
            bool clicked = _clickStartedOnThis && !_isPressed && IsHovered;
            if (clicked)
            {
                _clickStartedOnThis = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 切换选定状态（用于切换模式按钮）
        /// </summary>
        public void Toggle()
        {
            if (IsEnabled && IsToggleMode)
            {
                IsSelected = !IsSelected;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 设置选定状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            NotifyStateChanged();
        }

        /// <summary>
        /// 设置悬停状态（用于输入模式切换时清除）
        /// </summary>
        public void SetHover(bool hovered)
        {
            IsHovered = hovered;
            NotifyStateChanged();
        }

        /// <summary>
        /// 检查按钮是否正在被按下（鼠标按住中）
        /// 用于在 ActionChooseNumberIndex / ActionChooseTarget 中检测当前按下的按钮
        /// </summary>
        public bool IsPressed()
        {
            return _isPressed;
        }

        /// <summary>
        /// 清除选定状态
        /// </summary>
        public void ClearSelected()
        {
            IsSelected = false;
            NotifyStateChanged();
        }

        /// <summary>
        /// 启用按钮
        /// </summary>
        public void Enable()
        {
            IsEnabled = true;
            NotifyStateChanged();
        }

        /// <summary>
        /// 禁用按钮（重置所有交互状态）
        /// </summary>
        public void Disable()
        {
            IsEnabled = false;
            IsHovered = false;
            _isPressed = false;
            IsSelected = false;
            _clickStartedOnThis = false;
            _mouseWasDown = false;
            NotifyStateChanged();
        }

        /// <summary>
        /// 刷新状态（清除上一帧状态记录，强制在下次 Update 时重新计算并通知）
        /// 用于游戏状态变化时强制刷新按钮显示
        /// </summary>
        public void RefreshState()
        {
            // 清除上一帧的状态记录，让下次 Update 时能检测到状态变化
            _lastState = null;
            // 清除选定状态，确保新回合开始时按钮不保留上一回合的选定状态
            IsSelected = false;
            // 重新计算当前状态并更新内部状态记录
            GetCurrentState();
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

        public bool Contains(Point point) => Bounds.Contains(point);
        public bool Contains(int x, int y) => Bounds.Contains(x, y);
    }
}