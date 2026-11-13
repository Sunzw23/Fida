#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Fida.UI
{
    /// <summary>
    /// 按钮基类：承载 UIButton 与 UIAnimatedButton 的公共字段、状态机与鼠标交互逻辑。
    /// TState 为具体按钮的状态枚举（UIButtonState / UIAnimatedButtonState），
    /// 各子类通过重写 <see cref="GetState"/> 定义自己的状态优先级
    /// （UIButton: Disabled&gt;Selected；UIAnimatedButton: Selected&gt;Disabled），
    /// 并实现 <see cref="IsPressedOrSelected"/> 等状态判定钩子（用各自的具名枚举），
    /// 以及重写 <see cref="OnStateChanged"/> 暴露类型精确的状态变化回调。
    /// </summary>
    public abstract class UIButtonBase<TState> : UIControl where TState : struct
    {
        // ─── 字段存储（供具体子类读写） ─────────────────────────────
        protected Rectangle Bounds { get; set; }
        protected string Text { get; set; } = null!;
        protected Texture2D Pixel { get; set; } = null!;
        protected SpriteFont Font { get; set; } = null!;

        // 环境检查器 - 按钮主动询问环境来决定自己是否启用
        public Func<bool> EnabledChecker { get; set; } = null!;

        // 按钮状态（各子类通过 GetState 定义自己的优先级）
        public TState State => GetState();

        protected bool IsEnabled { get; set; } = true;
        protected bool IsHovered { get; set; } = false;
        protected bool _isPressed { get; set; } = false;
        protected bool IsSelected { get; set; } = false;
        protected float HoverAmount;
        protected float PressAmount;
        public float EntranceDelay { get; set; }
        private double _motionAge;
        private bool _audioHovered;
        protected float EntranceAmount => UIMotion.Reveal(_motionAge - EntranceDelay, 0.22f);

        protected void UpdateMotion(GameTime time)
        {
            _motionAge += time.ElapsedGameTime.TotalSeconds;
            bool audioHovered = IsEnabled && IsVisible && (IsHovered || IsFocused);
            if (audioHovered && !_audioHovered) Audio.AudioService.Feedback(Audio.SoundCue.Hover);
            _audioHovered = audioHovered;
            HoverAmount = UIMotion.Approach(HoverAmount, IsEnabled && IsVisible && (IsHovered || IsFocused) ? 1f : 0f,
                time.ElapsedGameTime.TotalSeconds);
            PressAmount = UIMotion.Approach(PressAmount, _isPressed ? 1f : 0f,
                time.ElapsedGameTime.TotalSeconds, 24f);
        }

        public override void Update(GameTime gameTime) => UpdateMotion(gameTime);

        protected void DrawPaperButton(SpriteBatch batch, Color color, int cut = 7, UIPaperShape shape = UIPaperShape.Cut) =>
            UIPaper.Button(batch, Bounds, color, HoverAmount, PressAmount, IsEnabled, EntranceAmount, cut, shape);

        // 支持切换模式 - 点击后切换选定状态
        public bool IsToggleMode { get; set; } = false;

        // 状态变化回调 - 具体子类重写以使用精确枚举类型
        public Action<TState> OnStateChanged { get; set; } = null!;

        // 记录上一帧的状态用于检测变化
        private TState? _lastState = null;

        // ─── 手柄焦点支持 ──────────────────────────────────
        /// <summary>
        /// 按钮可以被手柄聚焦
        /// </summary>
        public override bool CanFocus => IsEnabled && IsVisible;

        public override Rectangle? GetScreenBounds(Viewport viewport) =>
            IsVisible ? Bounds : null;

        // 配色统一取自 UITheme（深色科技主题）

        // ---- 鼠标交互状态机 ----
        // _clickStartedOnThis: 当前点击序列是否起始于此按钮
        // _mouseWasDown: 上一帧鼠标左键是否按下
        private bool _clickStartedOnThis = false;
        private bool _mouseWasDown = false;

        protected UIButtonBase(int id) : base(id) { }

        /// <summary>
        /// 由各子类实现自己的状态优先级
        /// </summary>
        protected abstract TState GetState();

        /// <summary>
        /// 当前状态是否为 Pressed 或 Selected（子类用各自具名枚举实现）
        /// </summary>
        protected abstract bool IsPressedOrSelected();

        /// <summary>
        /// 当前状态是否为 Disabled
        /// </summary>
        protected abstract bool IsDisabled();

        /// <summary>
        /// 当前状态是否为 Hovered
        /// </summary>
        protected abstract bool IsHoveredState();

        /// <summary>
        /// 获取当前状态并记录（用于外部直接获取当前状态）
        /// </summary>
        protected TState GetCurrentState()
        {
            var currentState = State;
            _lastState = currentState;
            return currentState;
        }

        /// <summary>
        /// 检测状态变化并通知（只在 Update 中自动调用）
        /// </summary>
        protected void CheckStateChanged()
        {
            var currentState = State;
            if (_lastState == null || !_lastState.Value.Equals(currentState))
            {
                _lastState = currentState;
                OnStateChanged?.Invoke(currentState);
            }
        }

        /// <summary>
        /// 强制通知状态变化（用于外部方法手动修改状态后调用）
        /// </summary>
        protected void NotifyStateChanged()
        {
            var currentState = GetCurrentState();
            OnStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// 根据状态获取背景色。
        /// </summary>
        protected Color GetBackColor()
        {
            return Color.Lerp(Color.Lerp(UITheme.PanelLight, UITheme.PanelHover, HoverAmount),
                UITheme.PanelPressed, IsSelected ? 1f : PressAmount) * EntranceAmount;
        }

        /// <summary>
        /// 根据状态获取前景色。
        /// </summary>
        protected Color GetForeColor()
        {
            if (IsDisabled()) return UITheme.InkMuted * EntranceAmount;
            return UITheme.Ink * EntranceAmount;
        }

        /// <summary>
        /// 根据状态获取边框颜色。
        /// </summary>
        protected Color GetBorderColor()
        {
            if (IsDisabled()) return UITheme.Panel * EntranceAmount;
            if (IsHoveredState() || IsPressedOrSelected()) return UITheme.GoldBright * EntranceAmount;
            return UITheme.Line * EntranceAmount;
        }

        /// <summary>
        /// 根据状态获取边框厚度
        /// </summary>
        protected int GetBorderThickness()
        {
            if (IsDisabled()) return 0;                 // 禁用：无边框
            if (IsHoveredState()) return 3;             // Focused: clear target
            if (IsPressedOrSelected()) return 3;        // Pressed/selected: strong target
            return 1;                                   // Normal: quiet signal
        }

        /// <summary>
        /// 是否需要绘制双层边框 - 按下和选定状态
        /// </summary>
        protected bool HasDoubleBorder() => IsPressedOrSelected();

        public override void Update(MouseState mouseState)
        {
            if (!IsVisible) return; // 不可见时不响应鼠标/手柄（数字面板非 ChooseNumber 步骤时隐藏）
            bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
            var mousePoint = new Point(mouseState.X, mouseState.Y);

            // 按钮主动询问环境：我现在应该启用吗？
            bool shouldBeEnabled = EnabledChecker?.Invoke() ?? IsEnabled;

            // 按钮从启用变为禁用
            if (IsEnabled && !shouldBeEnabled)
            {
                IsEnabled = false;
                IsHovered = false;
                _isPressed = false;
                _clickStartedOnThis = false;
                _mouseWasDown = mouseDown;
                OnDisabledTransition();
                NotifyStateChanged();
                return;
            }

            // 按钮从禁用变为启用
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
            // Focus shares the visual highlight, but cannot start a mouse click elsewhere.
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

            // Leaving the target cancels the sequence, including release outside.
            if (!IsHovered)
            {
                _isPressed = false;
                _clickStartedOnThis = false;
            }

            _mouseWasDown = mouseDown;

            // 检测状态变化（Hovered、Pressed 变化都需要通知）
            CheckStateChanged();
        }

        /// <summary>
        /// 由"启用→禁用"分支调用的钩子：
        /// UIButton 清除 Selected；UIAnimatedButton 保留 Selected（用于显示已选择项）
        /// </summary>
        protected virtual void OnDisabledTransition() { IsSelected = false; }

        /// <summary>
        /// 检测按钮是否完成了一次完整点击（按下 + 释放均在按钮上）
        /// 调用后状态会被消费（返回 true 后再次调用返回 false）
        /// </summary>
        public virtual bool IsClicked()
        {
            if (!IsEnabled) return false;

            // 完整点击：起始于此按钮 + 已释放 + 仍在按钮上
            bool clicked = _clickStartedOnThis && !_isPressed && IsHovered;
            if (clicked)
            {
                _clickStartedOnThis = false;
                OnClickCompleted();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 点击完成后的钩子（UIButton 用于触发 Clicked 事件）
        /// </summary>
        protected virtual void OnClickCompleted() { }

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
        public virtual void SetSelected(bool selected)
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
            _lastState = null;
            IsSelected = false;
            GetCurrentState();
        }

        public virtual bool Contains(Point point) => Bounds.Contains(point);
        public bool Contains(int x, int y) => Contains(new Point(x, y));
    }
}
