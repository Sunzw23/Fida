using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Fida.UI
{
    /// <summary>
    /// 按钮状态枚举
    /// </summary>
    public enum UIButtonState
    {
        Normal,     // 可用状态 - 静态，小边框
        Disabled,   // 禁用状态 - 静态，无边框
        Hovered,    // 鼠标悬浮/手柄聚焦 - 静态，大边框
        Pressed,    // 按下状态 - 双层边框
        Selected    // 已选择状态 - 双层边框
    }

    public class UIButton : UIControl
    {
        private Rectangle Bounds { get; set; }
        private string Text { get; set; }
        private Texture2D Pixel { get; set; }
        private SpriteFont Font { get; set; }

        // 环境检查器 - 按钮主动询问环境来决定自己是否启用
        public Func<bool> EnabledChecker { get; set; }

        // 按钮状态（优先级：Disabled > Selected > Pressed > Hovered > Normal）
        public UIButtonState State
        {
            get
            {
                if (!IsEnabled) return UIButtonState.Disabled;
                if (IsSelected) return UIButtonState.Selected;
                if (_isPressed) return UIButtonState.Pressed;
                if (IsHovered) return UIButtonState.Hovered;
                return UIButtonState.Normal;
            }
        }

        private bool IsEnabled { get; set; } = true;
        private bool IsHovered { get; set; } = false;
        private bool _isPressed { get; set; } = false;
        private bool IsSelected { get; set; } = false;
        
        // 状态变化回调 - 当按钮状态发生变化时通知外部刷新
        public Action<UIButtonState> OnStateChanged { get; set; }
        
        // 记录上一帧的状态用于检测变化
        private UIButtonState? _lastState = null;
        
        // 获取当前状态并记录（用于外部直接获取当前状态）
        private UIButtonState GetCurrentState()
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

        // 纯黑白配色方案
        private static readonly Color Black = Color.Black;
        private static readonly Color White = Color.White;

        public UIButton(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font) : base(id)
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
                UIButtonState.Normal => Black,
                UIButtonState.Disabled => Black,
                UIButtonState.Hovered => Black,
                UIButtonState.Pressed => White,     // 按下：白色背景（反色）
                UIButtonState.Selected => White,    // 选定：白色背景
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
                UIButtonState.Normal => White,
                UIButtonState.Disabled => White,    // 禁用：白色文字（可见但不可交互）
                UIButtonState.Hovered => White,
                UIButtonState.Pressed => Black,     // 按下：黑色文字（反色效果）
                UIButtonState.Selected => Black,    // 选定：黑色文字
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
                UIButtonState.Normal => White,
                UIButtonState.Disabled => Black,    // 禁用：黑色边框（与背景融合 = 无边框）
                UIButtonState.Hovered => White,
                UIButtonState.Pressed => White,
                UIButtonState.Selected => White,
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
                UIButtonState.Normal => 1,          // 可用：小边框
                UIButtonState.Disabled => 0,        // 禁用：无边框
                UIButtonState.Hovered => 4,         // 悬浮：大边框
                UIButtonState.Pressed => 2,         // 按下：中等边框（双层）
                UIButtonState.Selected => 2,        // 选定：中等边框（双层）
                _ => 1
            };
        }

        /// <summary>
        /// 是否需要绘制双层边框 - 按下和选定状态
        /// </summary>
        private bool HasDoubleBorder()
        {
            return State == UIButtonState.Pressed || State == UIButtonState.Selected;
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            Color backColor = GetBackColor();
            Color foreColor = GetForeColor();
            Color borderColor = GetBorderColor();
            int borderThickness = GetBorderThickness();

            // 绘制背景
            sb.Draw(Pixel, Bounds, backColor);

            // 绘制文字（居中）
            var size = Font.MeasureString(Text);
            var pos = new Vector2(
                Bounds.Center.X - size.X / 2,
                Bounds.Center.Y - size.Y / 2
            );
            sb.DrawString(Font, Text, pos, foreColor);

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
            
            // 如果按钮从启用变为禁用，清除所有状态（包括 Selected）
            if (IsEnabled && !shouldBeEnabled)
            {
                IsEnabled = false;
                IsHovered = false;
                _isPressed = false;
                _clickStartedOnThis = false;
                _mouseWasDown = mouseDown;
                IsSelected = false; // 清除 Selected 状态
                NotifyStateChanged();
                return;
            }
            
            // 如果按钮从禁用变为启用，需要通知状态变化
            if (!IsEnabled && shouldBeEnabled)
            {
                IsEnabled = true;
                _mouseWasDown = mouseDown;
                // 启用时恢复悬浮状态检测
                IsHovered = Contains(mousePoint);
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
            IsHovered = Contains(mousePoint);

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

        public override void Update(string text)
        {
            Text = text;
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
            if (IsEnabled)
            {
                IsSelected = selected;
                NotifyStateChanged();
            }
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

        public bool Contains(Point point) => Bounds.Contains(point);
        public bool Contains(int x, int y) => Bounds.Contains(x, y);
    }
}
