using System.Collections.Generic;
using System.Linq;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    /// <summary>
    /// UIInputHandler 类负责处理鼠标输入和 UI 交互。
    /// </summary>
    public class UIInputHandler
    {
        private readonly List<UIControl> _uiControls;
        private MouseState _currentMouseState;
        private MouseState _previousMouseState;

        // ─── 手柄输入支持 ──────────────────────────────────
        private ControllerInputHandler _controllerInput;
        private bool _controllerActionConsumed = false;

        public UIInputHandler(List<UIControl> uiControls)
        {
            _uiControls = uiControls;
            _controllerInput = new ControllerInputHandler(PlayerIndex.One);
        }

        /// <summary>
        /// 获取手柄输入处理器（供 UIManager 使用）
        /// </summary>
        public ControllerInputHandler ControllerInput => _controllerInput;

        /// <summary>
        /// 更新鼠标状态并检测点击的 UI 区域。
        /// 注意：此方法假设 UI 控件已经在 UIManager.UpdateUIButtons 中更新过
        /// </summary>
        public Action UpdateMouseAndCheckClicks(GameManager gameManager)
        {
            _currentMouseState = Mouse.GetState();

            // 不再更新 UI 控件状态 - 已经在 UIManager.UpdateUIButtons 中更新过了
            // 只处理切换模式按钮的点击
            HandleToggleButtons();

            bool leftClick = _currentMouseState.LeftButton == ButtonState.Pressed &&
                             _previousMouseState.LeftButton == ButtonState.Released;
            _previousMouseState = _currentMouseState;

            if (!leftClick)
            {
                return new Action { Type = ActionType.None };
            }

            switch (gameManager.CurrentActionType)
            {
                case ActionType.ChooseNumberIndex:
                    return ActionChooseNumberIndex(gameManager);
                case ActionType.ChooseTarget:
                    return ActionChooseTarget(gameManager);
                case ActionType.ChooseNumber:
                    return ActionChooseNumber(gameManager);
                default:
                    break;
            }

            return new Action { Type = ActionType.None };
        }

        /// <summary>
        /// 更新手柄输入状态（每帧调用）
        /// </summary>
        public void UpdateController(GameTime gameTime)
        {
            _controllerInput.Update(gameTime);
        }

        /// <summary>
        /// 检查手柄确认键是否被按下，并生成对应的游戏动作
        /// 由 UIManager 在焦点管理后调用
        /// </summary>
        public Action CheckControllerConfirm(GameManager gameManager)
        {
            if (!_controllerInput.IsConnected)
                return new Action { Type = ActionType.None };

            // 防止同一帧重复触发
            if (_controllerActionConsumed)
            {
                if (!_controllerInput.IsHeld(Buttons.A))
                    _controllerActionConsumed = false;
                return new Action { Type = ActionType.None };
            }

            if (!_controllerInput.IsConfirmPressed())
                return new Action { Type = ActionType.None };

            _controllerActionConsumed = true;

            // 找到当前聚焦的控件
            UIControl focusedControl = _uiControls.FirstOrDefault(c => c.IsFocused && c.CanFocus);
            if (focusedControl == null)
                return new Action { Type = ActionType.None };

            // 触发确认事件
            focusedControl.OnConfirmed();

            // 根据当前游戏动作类型和聚焦控件生成对应动作
            switch (gameManager.CurrentActionType)
            {
                case ActionType.ChooseNumberIndex:
                    return ControllerChooseNumberIndex(gameManager, focusedControl);
                case ActionType.ChooseTarget:
                    return ControllerChooseTarget(gameManager, focusedControl);
                case ActionType.ChooseNumber:
                    return ControllerChooseNumber(gameManager, focusedControl);
                default:
                    break;
            }

            return new Action { Type = ActionType.None };
        }

        /// <summary>
        /// 手柄选择数字索引
        /// </summary>
        private Action ControllerChooseNumberIndex(GameManager gameManager, UIControl focusedControl)
        {
            // 检查聚焦的是否是某个玩家的数字按钮
            foreach (UIPlayer uiPlayer in _uiControls.OfType<UIPlayer>())
            {
                if (focusedControl == uiPlayer.Number0Button)
                {
                    return new Action
                    {
                        Type = ActionType.ChooseNumberIndex,
                        TargetPlayerId = uiPlayer.Player.PlayerId,
                        NumberIndex = 0
                    };
                }
                if (focusedControl == uiPlayer.Number1Button)
                {
                    return new Action
                    {
                        Type = ActionType.ChooseNumberIndex,
                        TargetPlayerId = uiPlayer.Player.PlayerId,
                        NumberIndex = 1
                    };
                }
            }
            return new Action { Type = ActionType.None };
        }

        /// <summary>
        /// 手柄选择目标
        /// </summary>
        private Action ControllerChooseTarget(GameManager gameManager, UIControl focusedControl)
        {
            // 检查聚焦的是否是某个玩家的卡片
            foreach (UIPlayer uiPlayer in _uiControls.OfType<UIPlayer>())
            {
                if (focusedControl == uiPlayer)
                {
                    return new Action
                    {
                        Type = ActionType.ChooseTarget,
                        TargetPlayerId = uiPlayer.Player.PlayerId
                    };
                }
            }
            return new Action { Type = ActionType.None };
        }

        /// <summary>
        /// 手柄选择数字
        /// </summary>
        private Action ControllerChooseNumber(GameManager gameManager, UIControl focusedControl)
        {
            // 数字选择按钮的 ID 映射：ID = 100 + number
            if (focusedControl is UIButton btn)
            {
                int number = btn.GetId() - 100;
                if (number >= 0 && number <= 9)
                {
                    return new Action { Type = ActionType.ChooseNumber, Number = number };
                }
            }
            return new Action { Type = ActionType.None };
        }

        /// <summary>
        /// 处理切换模式按钮的点击
        /// </summary>
        private void HandleToggleButtons()
        {
            foreach (UIPlayer uiPlayer in _uiControls.OfType<UIPlayer>())
            {
                // 新设计中没有切换按钮，此方法保留但无需操作
            }
        }

        /// <summary>
        /// 处理选择数字索引的输入
        /// </summary>
        private Action ActionChooseNumberIndex(GameManager gameManager)
        {
            foreach (UIPlayer uiPlayer in _uiControls.OfType<UIPlayer>())
            {
                // 检测数字 0 按钮的点击
                if (uiPlayer.Number0Button.IsPressed())
                {
                    return new Action
                    {
                        Type = ActionType.ChooseNumberIndex,
                        TargetPlayerId = uiPlayer.Player.PlayerId,
                        NumberIndex = 0
                    };
                }
                
                // 检测数字 1 按钮的点击
                if (uiPlayer.Number1Button.IsPressed())
                {
                    return new Action
                    {
                        Type = ActionType.ChooseNumberIndex,
                        TargetPlayerId = uiPlayer.Player.PlayerId,
                        NumberIndex = 1
                    };
                }
            }

            // 没有点击任何有效按钮，返回空动作
            return new Action { Type = ActionType.None };
        }

        /// <summary>
        /// 处理选择目标的输入
        /// </summary>
        private Action ActionChooseTarget(GameManager gameManager)
        {
            foreach (UIPlayer uiPlayer in _uiControls.OfType<UIPlayer>())
            {
                if (CheckClickArea(uiPlayer.CardBounds))
                {
                    return new Action
                    {
                        Type = ActionType.ChooseTarget,
                        TargetPlayerId = uiPlayer.Player.PlayerId
                    };
                }
            }

            // 没有点击任何有效按钮，返回空动作
            return new Action { Type = ActionType.None };
        }

        /// <summary>
        /// 处理选择数字的输入
        /// </summary>
        private Action ActionChooseNumber(GameManager gameManager)
        {
            for (int i = 0; i <= 9; i++)
            {
                int x = i % 5;
                int y = i / 5;
                if (CheckClickArea(new Rectangle(10 + x * 50, 100 + y * 50, 50, 50)))
                {
                    return new Action { Type = ActionType.ChooseNumber, Number = i };
                }
            }

            // 没有点击任何有效按钮，返回空动作
            return new Action { Type = ActionType.None };
        }

        /// <summary>
        /// 检查鼠标是否在指定区域内
        /// </summary>
        private bool CheckClickArea(Rectangle area)
        {
            return area.Contains(_currentMouseState.X, _currentMouseState.Y);
        }

        /// <summary>
        /// 检测是否有鼠标活动（位置变化或点击）
        /// </summary>
        public bool HasMouseActivity()
        {
            var current = Mouse.GetState();
            bool moved = current.X != _previousMouseState.X || current.Y != _previousMouseState.Y;
            bool clicked = current.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;
            return moved || clicked;
        }

        /// <summary>
        /// 检测是否有键盘活动（任意按键按下）
        /// </summary>
        public bool HasKeyboardActivity()
        {
            var state = Keyboard.GetState();
            return state.GetPressedKeys().Length > 0;
        }
    }
}
