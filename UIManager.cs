using Fida.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

namespace Fida
{


    // 定义一个委托，用于处理开始按钮点击事件。
    public delegate void StartButtonHandler();
    public delegate void RelusButtonHandler();
    public delegate void SkillsButtonHandler();
    public delegate void ExitButtonHandler();
    public delegate void LevelButtonHandler(int index);

    public delegate void ReturnToMainMenuHandler();


    /// <summary>
    /// UIManager 类负责游戏的所有绘制操作和UI元素的交互检测。
    /// The UIManager class is responsible for all drawing operations and UI element interaction detection in the game.
    /// </summary>
    public class UIManager
    {
        // 声明公共事件，供 GameManager 订阅。
        // 开始界面相关事件
        public event StartButtonHandler OnStartButtonClick;
        public event RelusButtonHandler OnRulesButtonClick;
        public event SkillsButtonHandler OnSkillsButtonClick;
        public event ExitButtonHandler OnExitButtonClick;
        public event LevelButtonHandler OnLevelButtonClick;

        public event ReturnToMainMenuHandler OnReturnToMainMenuClick;

        // 游戏过程相关事件
        public event ChooseNumberIndexHandler OnChooseNumberIndex;
        public event ChooseTargetHandler OnChooseTarget;
        public event DecideApplyHandler OnDecideApply;
        public event ChooseNumberHandler OnChooseNumber;

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
                case ActionType.DecideApply:
                    DisableAll();
                    BuildDecideApplyUI(e.Message);
                    break;
                case ActionType.ChooseNumber:
                    DisableAll();
                    BuildChooseNumberUI(e.Message);
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

        public void Clean()
        {
            uIControls.RemoveAll(c => !c.IsVisible);
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
            Clean();
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
            UIButton startButton = new UIButton(UIConfig.StartButtonArea, "开始", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(startButton);

            // 订阅开始按钮的 OnClick 事件
            startButton.OnClick += (sender) => OnStartButtonClick?.Invoke();
            startButton.Enable();

            UIButton rulesButton = new UIButton(UIConfig.RulesButtonArea, "规则", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(rulesButton);
            rulesButton.OnClick += (sender) => OnRulesButtonClick?.Invoke();
            rulesButton.Enable();

            UIButton skillsButton = new UIButton(UIConfig.SkillsButtonArea, "技能", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(skillsButton);
            skillsButton.OnClick += (sender) => OnSkillsButtonClick?.Invoke();
            skillsButton.Enable();

            UIButton exitButton = new UIButton(UIConfig.ExitButtonArea, "退出", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(exitButton);
            exitButton.OnClick += (sender) => OnExitButtonClick?.Invoke();
            exitButton.Enable();

        }


        public void BuildRulesScreen()
        {
            Clear();

            UILabel rulesText = new UILabel(GameText.Rules, UIConfig.RulesTextPosition, _resourceManager.Font);
            AddControl(rulesText);

            UIButton returnToMainMenuButton = new UIButton(UIConfig.ReturnToMainMenuButtonArea, "返回", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(returnToMainMenuButton);
            returnToMainMenuButton.Enable();
            returnToMainMenuButton.OnClick += (sender) => OnReturnToMainMenuClick();

        }

        public void BuildSkillsScreen()
        {
            Clear();
            UILabel skillsText = new UILabel(GameText.Skills, UIConfig.SkillsTextPosition, _resourceManager.Font);
            AddControl(skillsText);
            UIButton returnToMainMenuButton = new UIButton(UIConfig.ReturnToMainMenuButtonArea, "返回", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(returnToMainMenuButton);
            returnToMainMenuButton.Enable();
            returnToMainMenuButton.OnClick += (sender) => OnReturnToMainMenuClick();
        }

        public void BuildLevelSelectScreen()
        {
            Clear();
            for (int i = 0; i < 10; i++) 
            {
                int index = i;
                UIButton levelButton = new UIButton(UIConfig.LevelButtonArea(i), i.ToString(), _resourceManager.Pixel, _resourceManager.Font);
                AddControl(levelButton);
                levelButton.Enable();
                levelButton.OnClick += (sender) => {
                    // 当点击关卡按钮时，触发 UIManager 的事件，并传递关卡索引。
                    OnLevelButtonClick?.Invoke(index);
                };
            }




            UIButton returnToMainMenuButton = new UIButton(UIConfig.ReturnToMainMenuButtonArea, "返回", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(returnToMainMenuButton);
            returnToMainMenuButton.Enable();
            returnToMainMenuButton.OnClick += (sender) => OnReturnToMainMenuClick();

        }


        public void BuildPlayingUI(GameManager gameManager, int level)
        {
            Clear();
            UIButton returnToMainMenuButton = new UIButton(UIConfig.ReturnToMainMenuButtonArea, "返回", _resourceManager.Pixel, _resourceManager.Font);
            AddControl(returnToMainMenuButton);
            returnToMainMenuButton.Enable();
            returnToMainMenuButton.OnClick += (sender) => OnReturnToMainMenuClick();

            UILabel gameMessageLabel = new UILabel(gameManager.GameMessage, UIConfig.GameMessagePosition, _resourceManager.Font);
            AddControl(gameMessageLabel);

            for (int i = 0; i < gameManager.Players.Count; i++)
            {
                var player = gameManager.Players[i];
                // 创建玩家UI控件
                UIPlayer character = new UIPlayer(player, SceneConfig.PlayersScene(level).Positions[i], _resourceManager.Font, _resourceManager.Pixel);
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

            UIDicideApply decideApply = new UIDicideApply(_resourceManager.Pixel, _resourceManager.Font, "");
            AddControl(decideApply);
            decideApply.Hide(); // 初始时隐藏决定应用按钮

            UIChooseNumber chooseNumber = new UIChooseNumber(_resourceManager.Pixel, _resourceManager.Font, "");
            AddControl(chooseNumber);
            chooseNumber.Hide(); // 初始时隐藏选择数字按钮



        }

        private void BuildDecideApplyUI(string message)
        {
            var decideApply = uIControls.OfType<UIDicideApply>().FirstOrDefault();
            decideApply.Show(); // 显示决定应用按钮
            decideApply.UpdateText(message); // 更新决定应用按钮的文本
            decideApply.Enable(); // 启用决定应用按钮
            // 订阅决定应用的事件
            decideApply.OnDecideApply += (decision) => {
                // 当决定应用被点击时，触发 UIManager 自己的事件，并传递决策结果。
                OnDecideApply?.Invoke(decision);
                decideApply.Hide(); // 决定应用后隐藏按钮
            };
        }

        private void BuildChooseNumberUI(string message)
        {
            var chooseNumber = uIControls.OfType<UIChooseNumber>().FirstOrDefault();
            chooseNumber.Show(); // 显示选择数字按钮
            chooseNumber.UpdateText(message); // 更新选择数字按钮的文本
            chooseNumber.Enable(); // 启用选择数字按钮
            // 订阅选择数字的事件
            chooseNumber.OnChooseNumber += number => {
                // 当选择数字被点击时，触发 UIManager 自己的事件，并传递选择的数字。
                OnChooseNumber?.Invoke(number);
                chooseNumber.Hide(); // 选择数字后隐藏按钮
            };
        }



        public void DisableAll()
        {
            // 禁用所有UI控件
            foreach (var control in uIControls.OfType<UIPlayer>())
            {
                control.Disable();
            }
        }

        public void EnableNumbers(List<int> validTargets)
        {
            foreach (var control in uIControls.OfType<UIPlayer>())
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
            foreach (var control in uIControls.OfType<UIPlayer>())
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
            UpdateNumber();
            UpdateHealth();
        }



        public void UpdateNumber(int playerId)
        {
            // 更新指定玩家的数字按钮文本
            var control = uIControls.OfType<UIPlayer>().FirstOrDefault(c => c.Player.PlayerId == playerId);
            control.UpdateNumbers();
        }


        public void UpdateNumber()
        {
            // 更新数字按钮的文本
            foreach (var control in uIControls.OfType<UIPlayer>())
            {
                control.UpdateNumbers();
            }
        }

        public void UpdateHealth(int playerId)
        {
            // 更新指定玩家的生命值按钮文本
            var control = uIControls.OfType<UIPlayer>().FirstOrDefault(c => c.Player.PlayerId == playerId);
            control.UpdateHealth();
        }

        public void UpdateHealth()
        {
            // 更新所有玩家的生命值按钮文本
            foreach (var control in uIControls.OfType<UIPlayer>())
            {
                control.UpdateHealth();
            }
        }



        public void BuildSettingsScreen()
        {
            Clear();
            // 创建设置按钮
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
