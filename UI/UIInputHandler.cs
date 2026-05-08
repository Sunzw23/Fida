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

        public UIInputHandler(List<UIControl> uiControls)
        {
            _uiControls = uiControls;
        }

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
    }
}