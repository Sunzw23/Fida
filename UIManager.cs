using Fida.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Fida
{






    /// <summary>
    /// UIManager 类负责游戏的所有绘制操作和UI元素的交互检测。
    /// The UIManager class is responsible for all drawing operations and UI element interaction detection in the game.
    /// </summary>
    public class UIManager
    {
        // 声明公共事件，供 GameManager 订阅。
        public event StartButtonHandler OnStartButtonClick;
        public event ChooseNumberIndexHandler OnChooseNumberIndex;
        public event ChooseTargetHandler OnChooseTarget;


        private readonly GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;
        private readonly ResourceManager _resourceManager; // 引用资源管理器


        private List<UIControl> uIControls = [];

        // 构造函数
        public UIManager(GraphicsDevice graphicsDevice, ResourceManager resourceManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = new SpriteBatch(graphicsDevice);
            _resourceManager = resourceManager; // 注入资源管理器实例
        }

        public void HandleActionRequired(PlayerActionRequiredEventArgs e)
        {
            DisableAll();
            switch (e.RequiredActionType)
            { 
                case ActionType.ChooseNumberIndex:
                    // 如果是选择数字的操作，启用数字按钮
                    EnableNumbers(e.ValidTargets);
                    break;
                case ActionType.ChooseTarget:
                    // 如果是选择目标的操作，启用生命值按钮
                    EnableHealth(e.ValidTargets);
                    break;
                default:
                    // 对于其他操作，禁用所有按钮
                    break;
            }
        }





        public void Clear()
        {
            uIControls = [];
        }

        public void Draw()
        {
            _graphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();
            // 清空
            foreach (UIControl c in uIControls)
            {
                c.Draw(_spriteBatch);
            }
            _spriteBatch.End();
        }

        public void Update(MouseState mouseState)
        {
            // 在事件驱动模式下，Update方法只负责更新所有UI控件的状态。
            // 控件内部会处理鼠标状态并触发事件。
            foreach (UIControl c in uIControls)
            {
                c.Update(mouseState);
            }
        }

        private void AddControl(UIControl control)
        {
            uIControls.Add(control);
        }

        public void BuildMainMenu()
        {
            Clear();

            // 创建开始按钮
            UIButton startButton = new UIButton(new Rectangle(400, 400, 200, 100), "开始", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(startButton);

            // 订阅开始按钮的 OnClick 事件
            startButton.OnClick += HandleStartButtonClick;
            startButton.Enable();
        }

        private void HandleStartButtonClick(object sender)
        {
            // 当开始按钮被点击时，触发 UIManager 自己的 OnStartButtonClick 事件
            OnStartButtonClick?.Invoke();
        }

        public void BuildPlayingUI(GameManager gameManager)
        {
            Clear();

            UILabel gameMessageLabel = new UILabel(gameManager.GameMessage, UIConfig.GameMessagePosition, _resourceManager.Font);
            AddControl(gameMessageLabel);

            for (int i = 0; i < gameManager.Players.Count; i++)
            {
                var player = gameManager.Players[i];
                // 创建玩家UI控件
                Character character = new Character(player, SceneConfig.PlayersScene1.Positions[i], _resourceManager.Font, _resourceManager.Pixel);
                AddControl(character);

                // 订阅 Character 控件的 OnNumberButtonClick 事件，并将其向上层传递。
                character.OnNumberButtonClick += (numberIndex) => {
                    // 当 Character 的数字按钮被点击时，触发 UIManager 自己的事件，并传递数字。
                    OnChooseNumberIndex?.Invoke(player.PlayerId, numberIndex);


                };
                // 订阅 Character 控件的 OnHealthButtonClick 事件，并将其向上层传递。
                character.OnHealthButtonClick += () => {
                    // 当 Character 的生命值按钮被点击时，触发 UIManager 自己的事件，并传递玩家ID。
                    OnChooseTarget?.Invoke(player.PlayerId);
                };
            }
        }

        public void DisableAll()
        {
            // 禁用所有UI控件
            foreach (var control in uIControls.OfType<Character>())
            {
                control.Disable();
            }
        }

        public void EnableNumbers(List<int> validTargets)
        {
            foreach (var control in uIControls.OfType<Character>())
            {
                if (validTargets.Contains(control.Player.PlayerId))
                {
                    // 如果玩家ID在有效目标列表中，则启用数字按钮
                    control.NumberButton[0].Enable();
                    control.NumberButton[1].Enable();
                }
            }
        }

        

        public void EnableHealth(List<int> validTargets)
        {
            // 启用生命值按钮
            foreach (var control in uIControls.OfType<Character>())
            {
                if (validTargets.Contains(control.Player.PlayerId))
                {
                    control.HealthButton.Enable();
                }
            }
        }



        public void UpdateMessage(string message)
        {
            // 更新游戏消息标签的文本
            var messageLabel = uIControls.OfType<UILabel>().FirstOrDefault();
            if (messageLabel != null)
            {
                messageLabel.UpdateText(message);
            }
        }


        public void UpdateAll()
        {
            // 更新所有UI控件的状态
            foreach (UIControl control in uIControls)
            {
                control.Update(Mouse.GetState());
            }
        }



        public void UpdateNumber(int playerId)
        {
            // 更新指定玩家的数字按钮文本
            var control = uIControls.OfType<Character>().FirstOrDefault(c => c.Player.PlayerId == playerId);
            control.UpdateNumbers();
        }


        public void UpdateNumber()
        {
            // 更新数字按钮的文本
            foreach (var control in uIControls.OfType<Character>())
            {
                control.UpdateNumbers();
            }
        }

        public void UpdateHealth(int playerId)
        {
            // 更新指定玩家的生命值按钮文本
            var control = uIControls.OfType<Character>().FirstOrDefault(c => c.Player.PlayerId == playerId);
            control.UpdateHealth();
        }

        public void UpdateHealth()
        {
            // 更新所有玩家的生命值按钮文本
            foreach (var control in uIControls.OfType<Character>())
            {
                control.UpdateHealth();
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
