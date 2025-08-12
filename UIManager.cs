// UIManager.cs 文件
using Fida.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions; // For .Any() and .Where()


namespace Fida
{

    public enum ActionType
    {
        ChooseNumberIndex,
        ChooseTarget,
        DecideApply,
        ChooseNumber,
        None,
    }

    public struct Action
    {
        public ActionType Type { get; set; }
        public int TargetPlayerId { get; set; }
        public int NumberIndex { get; set; }
        public int Number { get; set; }
        public bool IsApply { get; set; } 

        public Action()
        {
            this.Type = ActionType.None;
        }
    }



    /// <summary>
    /// UIManager 类负责游戏的所有绘制操作和UI元素的交互检测。
    /// The UIManager class is responsible for all drawing operations and UI element interaction detection in the game.
    /// </summary>
    public class UIManager
    {
        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch; 
        private ResourceManager _resourceManager; // 引用资源管理器

        // 鼠标状态
        // Mouse state
        private MouseState _currentMouseState;
        private MouseState _previousMouseState;

        private List<UIControl> uIControls = [];

        // 构造函数
        public UIManager(GraphicsDevice graphicsDevice, ResourceManager resourceManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = new SpriteBatch(graphicsDevice);
            _resourceManager = resourceManager; // 注入资源管理器实例
        }

        public void Clear()
        {
            uIControls = [];
        }


        private void AddControl(UIControl control)
        {
            uIControls.Add(control);
        }

        public void BuildMainMenu()
        {
            Clear();

            // 创建开始按钮
            UIButton startButton = new UIButton(0, new Rectangle(400, 400, 200, 100), "开始", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(startButton);
        }

        public void BuildPlayingUI(GameManager gameManager)
        {
            Clear();

            UILabel gameMessageLabel = new UILabel(0, gameManager.GameMessage, UIConfig.GameMessagePosition, _resourceManager.Font);
            AddControl(gameMessageLabel);

            for (int i = 0; i < gameManager.Players.Count; i++)
            {
                var player = gameManager.Players[i];
                // 创建玩家UI控件
                Character character = new Character(player.PlayerId, player, SceneConfig.PlayersScene1.Positions[i], _resourceManager.Font, _resourceManager.Pixel);
                AddControl(character);
            }



        }

        public void BuildSettingsScreen()
        {
            Clear();
            // 创建设置按钮

        }

        public void BuildRulesScreen()
        {
            Clear();

        }


        /// <summary>
        /// 更新鼠标状态并检测点击的UI区域。
        /// Updates mouse state and detects clicked UI areas.
        /// </summary>
        /// <returns>如果鼠标左键被点击，返回点击区域的Rectangle；否则返回null。</returns>
        /// <returns>If the left mouse button is clicked, returns the clicked area's Rectangle; otherwise, returns null.</returns>
        public Action UpdateMouseAndCheckClicks(GameManager gameManager)
        {
            _currentMouseState = Mouse.GetState();
            bool leftClick = _currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;
            _previousMouseState = _currentMouseState;

            if (!leftClick)
            {
                return new Action { Type = ActionType.None }; // 鼠标左键未点击，返回空Action
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
            return new Action { Type = ActionType.None }; // 没有点击或点击在非交互区域
        }

        private Action ActionChooseNumberIndex(GameManager gameManager)
        {
            foreach (Character c in uIControls.OfType<Character>())
            {
                for (int i = 0; i < 2; i++) 
                {
                    if (c.NumberButton[i].IsButtonPressed())
                    {
                        return new Action { Type = ActionType.ChooseNumberIndex, TargetPlayerId = c.Player.PlayerId, NumberIndex = i };
                    }
                }

            }
            return new Action();
        }


        private Action ActionChooseTarget(GameManager gameManager)
        {
            foreach (Character c in uIControls.OfType<Character>())
            {
                if (c.HealthButton.IsButtonPressed())
                {
                    return new Action { Type = ActionType.ChooseTarget, TargetPlayerId = c.Player.PlayerId };
                }
            }
            return new Action();
        }

        private Action ActionChooseNumber(GameManager gameManager)
        {
            for (int i = 0; i<=9; i++)
            {
                int _x = i % 5;
                int _y = i / 5;
                if (CheckClickArea(new Rectangle(10 + _x * 50, 100 + _y * 50, 50, 50)))
                {
                    return new Action { Type = ActionType.ChooseNumber, Number = i };
                }
            }
            return new Action();
        }



        /// <summary>
        /// 辅助方法：检查鼠标是否在指定区域内
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        private bool CheckClickArea(Rectangle area)
        {
            return area.Contains(_currentMouseState.X, _currentMouseState.Y);
        }




        /// <summary>
        /// 绘制游戏消息。
        /// </summary>
        /// <param name="message"></param>
        private void DrawGameMessage(string message)
        {
            _spriteBatch.DrawString(_resourceManager.Font, message, new Vector2(10, 10), Color.Gray);
        }


        


        /// <summary>
        /// 辅助方法：绘制矩形（用于填充或背景）
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        private void DrawRectangle(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_resourceManager.Pixel, rect, color);
        }

        /// <summary>
        /// 辅助方法：在矩形内绘制字符串（用于按钮或标签）
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        private void DrawStringInBox(Rectangle rect, string text, Color color)
        {
            var textSize = _resourceManager.Font.MeasureString(text);
            var position = new Vector2(rect.X + (rect.Width - textSize.X) / 2, rect.Y + (rect.Height - textSize.Y) / 2);
            _spriteBatch.DrawString(_resourceManager.Font, text, position, color);
        }




        /// <summary>
        /// 清理资源。
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _spriteBatch?.Dispose();
            _spriteBatch = null;
        }


    }
}
