// UIManager.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
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
                                                  // Reference to the resource manager

        // 鼠标状态
        // Mouse state
        private MouseState _currentMouseState;
        private MouseState _previousMouseState;


        // 构造函数
        public UIManager(GraphicsDevice graphicsDevice, ResourceManager resourceManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = new SpriteBatch(graphicsDevice);
            _resourceManager = resourceManager; // 注入资源管理器实例
        }

        /// <summary>
        /// 绘制游戏界面。
        /// Draws the game interface.
        /// </summary>
        /// <param name="gameManager">提供游戏状态的GameManager实例。</param>
        /// <param name="gameManager">The GameManager instance providing game state.</param>
        public void DrawGame(GameManager gameManager)
        {
            _graphicsDevice.Clear(Color.Black); // 清空屏幕
                                                        

            _spriteBatch.Begin();

            // 绘制玩家信息
            // Draw player information
            foreach (var player in gameManager.Players)
            {
                player.Draw(_spriteBatch);
            }

            if (gameManager.CurrentGamePhase.SourcePlayerId == 0)
            {
                DrawGameMessage(gameManager.GameMessage);
            }

            


            if (gameManager.CurrentGamePhase.Type == GamePhaseType.HandleSkillDoubleAttack)
            {
                if (gameManager.CurrentActionType == ActionType.DecideApply)
                {
                    DrawDecideButton("使用双倍攻击吗? ");
                }
            }

            if (gameManager.CurrentGamePhase.Type == GamePhaseType.HandleSkillShen)
            {
                if (gameManager.CurrentActionType == ActionType.ChooseNumber)
                {
                    DrawChangeNumberForms("选择数字:");
                }
            }

            if (gameManager.CurrentGamePhase.Type == GamePhaseType.HandleSkillMo)
            {
                if (gameManager.CurrentActionType == ActionType.DecideApply)
                {
                    DrawDecideButton("使用技能: 魔吗? ");
                }
            }

            if (gameManager.CurrentGamePhase.Type == GamePhaseType.HandleSkillXian)
            {
                if (gameManager.CurrentActionType == ActionType.ChooseNumber)
                {
                    DrawChangeNumberForms("选择数字:");
                }
            }

            if (gameManager.CurrentGamePhase.Type == GamePhaseType.HandleSkillTong)
            {
                if (gameManager.CurrentActionType == ActionType.DecideApply)
                {
                    DrawDecideButton("使用技能: 同变吗? ");
                }

                if (gameManager.CurrentActionType == ActionType.ChooseNumber)
                {
                    DrawChangeNumberForms("选择数字:");
                }
            }

            if (gameManager.CurrentGamePhase.Type == GamePhaseType.HandleSkillDi)
            {
                if (gameManager.CurrentActionType == ActionType.ChooseNumber)
                {
                    DrawChangeNumberForms("选择数字:");
                }
            }



            _spriteBatch.End();
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
                case ActionType.DecideApply:
                    return ActionDecideApply(gameManager);
                case ActionType.ChooseNumber:
                    return ActionChooseNumber(gameManager);
                default:
                    break;
            }
            return new Action { Type = ActionType.None }; // 没有点击或点击在非交互区域
        }

        private Action ActionChooseNumberIndex(GameManager gameManager)
        {
            foreach (Character player in gameManager.Players)
            {
                for (int i = 0; i < 2; i++)
                {
                    
                    if (CheckClickArea(player.GetNumberRectangle(i)))
                    {
                        return new Action { Type = ActionType.ChooseNumberIndex, TargetPlayerId = player.PlayerId, NumberIndex = i };
                    }
                }
                
            }
            return new Action();
        }


        private Action ActionChooseTarget(GameManager gameManager)
        {
            foreach (Character player in gameManager.Players)
            {
                if (CheckClickArea(player.GetTargetRectangle()))
                {
                    return new Action { Type = ActionType.ChooseTarget, TargetPlayerId = player.PlayerId };
                }
            }
            return new Action();
        }

        private Action ActionDecideApply(GameManager gameManager)
        {
            if (CheckClickArea(new Rectangle(10, 100, 30, 30)))
            {
                return new Action { Type = ActionType.DecideApply, IsApply = true };
            }
            if (CheckClickArea(new Rectangle(10, 140, 30, 30)))
            {
                return new Action { Type = ActionType.DecideApply, IsApply = false };
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



        // 辅助方法：检查鼠标点击是否在指定区域内
        // Helper method: Checks if the mouse click is within the specified area
        private bool CheckClickArea(Rectangle area)
        {
            return area.Contains(_currentMouseState.X, _currentMouseState.Y);
        }


        private void DrawPlayerInfo(Character player)
        {
            // 绘制玩家名称
            _spriteBatch.DrawString(_resourceManager.Font, "Name: " + player.Name, player.Position, Color.White);
            // 绘制玩家生命值
            _spriteBatch.DrawString(_resourceManager.Font, "Health: " + player.Health, player.HealthLocation, Color.Red);
            // 绘制玩家数字
            for (int i = 0; i < player.Numbers.Count; i++)
            {
                _spriteBatch.DrawString(_resourceManager.Font, player.Numbers[i].ToString(), player.NumberLocations[i], Color.White);
            }
        }

        private void DrawDecideButton(string message)
        {
            _spriteBatch.DrawString(_resourceManager.Font, message, new Vector2(10, 50), Color.White);
            _spriteBatch.DrawString(_resourceManager.Font, "Yes", new Vector2(10, 100), Color.Green);
            _spriteBatch.DrawString(_resourceManager.Font, "No", new Vector2(10, 140), Color.Red);
        }

        private void DrawChangeNumberForms(string message)
        {
            _spriteBatch.DrawString(_resourceManager.Font, message, new Vector2(10, 50), Color.White);
            for (int i = 0; i <= 9; i++)
            {
                int _x = i % 5;
                int _y = i / 5;
                _spriteBatch.DrawString(_resourceManager.Font, i.ToString(), new Vector2(10 + _x * 50, 100 + _y * 50), Color.White);
            }
        }

        private void DrawGameMessage(string message)
        {
            _spriteBatch.DrawString(_resourceManager.Font, message, new Vector2(10, 10), Color.Gray);
        }


        

        // 辅助方法：绘制矩形边框(用于模拟按钮或选择区域）
        // Helper method: Draws a rectangle outline (used to simulate buttons or selection areas)
        private void DrawRectangleOutline(Rectangle rect, Color color)
        {
            // 绘制四个边
            // Draw the four sides
            _spriteBatch.Draw(_resourceManager.Pixel, new Rectangle(rect.Left, rect.Top, rect.Width, 1), color); // Top
            _spriteBatch.Draw(_resourceManager.Pixel, new Rectangle(rect.Left, rect.Bottom, rect.Width, 1), color); // Bottom
            _spriteBatch.Draw(_resourceManager.Pixel, new Rectangle(rect.Left, rect.Top, 1, rect.Height), color); // Left
            _spriteBatch.Draw(_resourceManager.Pixel, new Rectangle(rect.Right, rect.Top, 1, rect.Height + 1), color); // Right
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
