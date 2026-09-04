using System;
using System.Collections.Generic;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    /// <summary>
    /// 手柄焦点与输入模式管理：
    /// - 输入模式（鼠标 / 手柄）切换与状态清理
    /// - 手柄导航焦点管理（方向键 / 确认键 / 自动聚焦）
    /// 从 UIManager 中提取，便于独立维护。
    /// </summary>
    public class UIFocusManager
    {
        private readonly List<UIControl> _uiControls;
        private readonly UIInputHandler _inputHandler;

        /// <summary>当前输入模式</summary>
        public InputMode CurrentInputMode { get; private set; } = InputMode.Mouse;

        public UIFocusManager(List<UIControl> uiControls, UIInputHandler inputHandler)
        {
            _uiControls = uiControls;
            _inputHandler = inputHandler;
        }

        /// <summary>获取手柄输入处理器</summary>
        public ControllerInputHandler GetControllerInput() => _inputHandler.ControllerInput;

        /// <summary>更新手柄输入并处理焦点导航</summary>
        public void UpdateControllerInput(GameTime gameTime)
        {
            _inputHandler.UpdateController(gameTime);
            // 仅在控制器模式下管理焦点，避免鼠标模式下 EnsureFocus 重新设置焦点
            if (CurrentInputMode == InputMode.Controller)
            {
                UpdateFocusNavigation();
                EnsureFocus();
            }
        }

        /// <summary>检查手柄确认键并生成游戏动作</summary>
        public GameAction CheckControllerConfirm(GameManager gameManager)
        {
            return _inputHandler.CheckControllerConfirm(gameManager);
        }

        /// <summary>更新输入模式 - 根据最近的输入活动自动切换</summary>
        public void UpdateInputMode()
        {
            if (_inputHandler.ControllerInput.HasAnyInput())
            {
                SetInputMode(InputMode.Controller);
            }
            else if (_inputHandler.HasMouseActivity() || _inputHandler.HasKeyboardActivity())
            {
                SetInputMode(InputMode.Mouse);
            }
        }

        /// <summary>设置输入模式，切换时清理另一种模式的 UI 状态</summary>
        private void SetInputMode(InputMode mode)
        {
            if (CurrentInputMode == mode) return;
            CurrentInputMode = mode;

            if (mode == InputMode.Controller)
            {
                // 切换到控制器模式：隐藏鼠标指针，清除 hover，确保有焦点
                ClearHoverStates();
                EnsureFocus();
            }
            else
            {
                // 切换到鼠标模式：清除焦点
                ClearFocus();
            }
        }

        /// <summary>清除所有控件的 hover 状态</summary>
        private void ClearHoverStates()
        {
            foreach (var control in _uiControls)
            {
                if (control is UIButton btn)
                    btn.SetHover(false);
                if (control is UIAnimatedButton animBtn)
                    animBtn.SetHover(false);
            }
        }

        /// <summary>获取当前可聚焦的控件列表（按 FocusIndex 排序）</summary>
        private List<UIControl> GetFocusableControls()
        {
            var focusable = new List<UIControl>();
            foreach (var control in _uiControls)
            {
                if (control.CanFocus)
                    focusable.Add(control);
            }
            focusable.Sort((a, b) => a.FocusIndex.CompareTo(b.FocusIndex));
            return focusable;
        }

        /// <summary>根据手柄导航方向更新焦点</summary>
        private void UpdateFocusNavigation()
        {
            var controller = _inputHandler.ControllerInput;
            if (!controller.IsConnected) return;

            var direction = controller.GetNavigationDirection();
            if (direction == ControllerInputHandler.Direction.None) return;

            var focusable = GetFocusableControls();
            if (focusable.Count == 0) return;

            // 找到当前聚焦的控件索引
            int currentIndex = focusable.FindIndex(c => c.IsFocused);

            int newIndex = currentIndex;

            switch (direction)
            {
                case ControllerInputHandler.Direction.Up:
                    // 向上导航：找上一行（按 FocusIndex 递减）
                    newIndex = currentIndex > 0 ? currentIndex - 1 : focusable.Count - 1;
                    break;
                case ControllerInputHandler.Direction.Down:
                    // 向下导航：找下一行（按 FocusIndex 递增）
                    newIndex = currentIndex < focusable.Count - 1 ? currentIndex + 1 : 0;
                    break;
                case ControllerInputHandler.Direction.Left:
                    // 向左导航：找上一个
                    newIndex = currentIndex > 0 ? currentIndex - 1 : focusable.Count - 1;
                    break;
                case ControllerInputHandler.Direction.Right:
                    // 向右导航：找下一个
                    newIndex = currentIndex < focusable.Count - 1 ? currentIndex + 1 : 0;
                    break;
            }

            if (newIndex != currentIndex)
            {
                // 取消旧焦点
                if (currentIndex >= 0 && currentIndex < focusable.Count)
                    focusable[currentIndex].SetFocus(false);

                // 设置新焦点
                focusable[newIndex].SetFocus(true);
            }
            else if (currentIndex < 0 && focusable.Count > 0)
            {
                // 没有焦点时，默认聚焦第一个
                focusable[0].SetFocus(true);
            }
        }

        /// <summary>清除所有控件的焦点</summary>
        public void ClearFocus()
        {
            foreach (var control in _uiControls)
            {
                if (control.IsFocused)
                    control.SetFocus(false);
            }
        }

        /// <summary>自动聚焦第一个可聚焦控件</summary>
        public void FocusFirst()
        {
            var focusable = GetFocusableControls();
            if (focusable.Count > 0)
            {
                ClearFocus();
                focusable[0].SetFocus(true);
            }
        }

        /// <summary>确保至少有一个控件获得焦点（当没有控件聚焦时自动聚焦第一个）</summary>
        private void EnsureFocus()
        {
            var focusable = GetFocusableControls();
            if (focusable.Count == 0) return;

            bool anyFocused = false;
            foreach (var c in focusable)
            {
                if (c.IsFocused)
                {
                    anyFocused = true;
                    break;
                }
            }

            if (!anyFocused)
            {
                focusable[0].SetFocus(true);
            }
        }
    }
}
