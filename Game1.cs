// Game1.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources; // 用于Linq查询

namespace Fida
{
    // Player.cs 保持不变，请确保它在项目中存在

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private ResourceManager _resourceManager; // 资源管理类
        private GameManager _gameManager; // 游戏逻辑管理类
        private UIManager _uiManager;     // UI绘制管理类
        private AgentManager _agentManager; // AI管理类

        private bool sceneLoaded = false;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // 设置窗口大小 (可根据需要调整)
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1200;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            _resourceManager = new ResourceManager(GraphicsDevice);
            _gameManager = new GameManager(_resourceManager);
            _uiManager = new UIManager(GraphicsDevice, _resourceManager);
            _agentManager = new AgentManager();
            _gameManager.InitializeGame(); // 初始化游戏逻辑
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // 将UI资源的加载委托给UIManager
            _resourceManager.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (sceneLoaded == false)
            {
                // 加载场景
                _gameManager.LoadScene1();
                sceneLoaded = true;
            }
            // 处理退出游戏
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();







            // 更新游戏逻辑，并将鼠标点击信息传递给GameManager
            if (_gameManager.CurrentGamePhase.SourcePlayerId == 0)
            {
                // 从UIManager获取鼠标点击信息
                Action clickAction = _uiManager.UpdateMouseAndCheckClicks(_gameManager);
                _gameManager.UpdateGame(clickAction);
            }
            else
            {
                // 从AgentManager获取AI模拟信息
                Action agentAction = _agentManager.UpdateAction(_gameManager);
                _gameManager.UpdateGame(agentAction);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // 绘制游戏界面，将游戏状态从GameManager传递给UIManager
            _uiManager.DrawGame(_gameManager);

            base.Draw(gameTime);
        }

        protected override void OnActivated(object sender, System.EventArgs args)
        {
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, System.EventArgs args)
        {
            // 清理UIManager的资源
            _resourceManager.Dispose();
            _uiManager.Dispose();
            _agentManager.Dispose();
            base.OnDeactivated(sender, args);
        }
    }
}
