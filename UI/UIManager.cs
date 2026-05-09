// UIManager.cs 文件
using System.Collections.Generic;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    /// <summary>
    /// UIManager 类负责游戏的所有绘制操作和 UI 元素的协调管理。
    /// The UIManager class is responsible for all drawing operations and UI element coordination in the game.
    /// </summary>
    public class UIManager
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly ResourceManager _resourceManager;

        private readonly List<UIControl> _uiControls = new List<UIControl>();
        private readonly UIPlayerManager _playerManager;
        private readonly UIInputHandler _inputHandler;

        public GameManager GameManager { get; private set; }

        public UIManager(GraphicsDevice graphicsDevice, ResourceManager resourceManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = new SpriteBatch(graphicsDevice);
            _resourceManager = resourceManager;

            _playerManager = new UIPlayerManager(_uiControls, _resourceManager);
            _inputHandler = new UIInputHandler(_uiControls);
            
            // 创建 GameManager 实例
            GameManager = new GameManager();
        }

        public void Draw(GameTime gameTime)
        {
            _graphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            foreach (UIControl control in _uiControls)
            {
                control.Draw(_spriteBatch, gameTime);
            }
            _spriteBatch.End();
        }

        public void Clear()
        {
            _uiControls.Clear();
        }

        public void BuildMainMenu()
        {
            Clear();

            // 屏幕 1920×1200，按钮 200×80，垂直居中排列，间距 40px
            int buttonWidth = 200;
            int buttonHeight = 80;
            int centerX = (1920 - buttonWidth) / 2;
            int totalHeight = buttonHeight * 2 + 40;
            int startY = (1200 - totalHeight) / 2;

            var startButton = new UIButton(0, new Rectangle(centerX, startY, buttonWidth, buttonHeight), "开始", _resourceManager.Pixel, _resourceManager.Font);
            var exitButton = new UIButton(1, new Rectangle(centerX, startY + buttonHeight + 40, buttonWidth, buttonHeight), "退出", _resourceManager.Pixel, _resourceManager.Font);

            _uiControls.Add(startButton);
            _uiControls.Add(exitButton);
        }

        public void BuildPlayingUI(GameManager gameManager)
        {
            _playerManager.BuildPlayingUI(gameManager);
        }

        public void UpdateGameMessage(string message)
        {
            _playerManager.UpdateGameMessage(message);
        }

        /// <summary>
        /// 每帧逻辑更新 - 处理动画计时等与渲染无关的逻辑
        /// </summary>
        public void Update(GameTime gameTime)
        {
            foreach (UIControl control in _uiControls)
            {
                control.Update(gameTime);
            }
        }

        /// <summary>
        /// 一键刷新所有控件 - 立即同步显示最新值，取消进行中的动画
        /// </summary>
        public void RefreshAll()
        {
            foreach (UIControl control in _uiControls)
            {
                if (control is UIBoxLabel boxLabel)
                    boxLabel.Refresh();
                else if (control is UIAnimatedButton animatedButton)
                    animatedButton.Refresh();
            }
        }

        public void UpdateUIButtons(GameManager gameManager)
        {
            // 更新所有 UI 按钮的状态（让按钮通过 EnabledChecker 询问环境来决定是否启用）
            var mouseState = Mouse.GetState();
            foreach (UIControl control in _uiControls)
            {
                control.Update(mouseState);
            }
        }

        public Action UpdateMouseAndCheckClicks(GameManager gameManager)
        {
            return _inputHandler.UpdateMouseAndCheckClicks(gameManager);
        }

        public UIPlayer GetUIPlayer(int playerId)
        {
            return _playerManager.GetUIPlayer(playerId);
        }

        /// <summary>
        /// 设置玩家头像纹理。如果纹理为 null 或未设置，头像区域不显示。
        /// 应在 BuildPlayingUI 之前调用。
        /// </summary>
        public void SetPortraitTexture(int playerId, Texture2D texture)
        {
            _playerManager.SetPortraitTexture(playerId, texture);
        }

        public List<UIControl> GetControls()
        {
            return _uiControls;
        }

        public void BuildPauseMenu()
        {
            Clear();

            int buttonWidth = 200;
            int buttonHeight = 80;
            int centerX = (1920 - buttonWidth) / 2;
            int totalHeight = buttonHeight * 2 + 40;
            int startY = (1200 - totalHeight) / 2;

            var continueButton = new UIButton(0, new Rectangle(centerX, startY, buttonWidth, buttonHeight), "继续", _resourceManager.Pixel, _resourceManager.Font);
            var returnButton = new UIButton(1, new Rectangle(centerX, startY + buttonHeight + 40, buttonWidth, buttonHeight), "返回", _resourceManager.Pixel, _resourceManager.Font);

            _uiControls.Add(continueButton);
            _uiControls.Add(returnButton);
        }

        public void BuildSettingsScreen()
        {
            Clear();
        }

        public void BuildRulesScreen()
        {
            Clear();
        }

        public void Dispose()
        {
            _spriteBatch?.Dispose();
        }
    }
}