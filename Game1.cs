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
        private State _currentState;
        private ResourceManager _resourceManager; // 资源管理类
        private GameManager _gameManager; // 游戏逻辑管理类
        private UIManager _uiManager;     // UI绘制管理类
        private AgentManager _agentManager; // AI管理类


        private bool sceneLoaded = false;
        private bool _isGameActive = true; // 游戏是否处于激活状态

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
            base.Initialize();
            _gameManager = new GameManager();
            _uiManager = new UIManager(GraphicsDevice, _resourceManager);
            _agentManager = new AgentManager();
            SwitchToState(State.Playing);

        }

        protected override void LoadContent()
        {
            // 将UI资源的加载委托给UIManager
            _resourceManager.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (_isGameActive)
            {
                // 处理退出游戏
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                    Exit();

                _uiManager.Update();

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
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // 绘制游戏界面，将游戏状态从GameManager传递给UIManager
            _uiManager.Draw();

            base.Draw(gameTime);
        }

        public void SwitchToState(State newState)
        {
            _currentState = newState;
            _uiManager.Clear();

            switch (newState)
            {
                case State.MainMenu:
                    BuildMainMenu();
                    break;

                case State.Rules:
                    BuildRulesScreen();
                    break;

                case State.Settings:
                    BuildSettingsScreen();
                    break;

                case State.Playing:
                    // 不需要 UIManager 显示主菜单，但可以加“返回主菜单”按钮
                    LoadScene1();
                    BuildPlayingUI();
                    break;
                case State.GameOver:
                    break;
            }

        }

        public void LoadScene1()
        {
            _gameManager.Initialize(SceneConfig.PlayersScene1.Players);
        }

        private void BuildMainMenu()
        {
            _uiManager.BuildMainMenu();
        }

        private void BuildRulesScreen()
        {
            _uiManager.BuildRulesScreen();
        }

        private void BuildSettingsScreen()
        {
            _uiManager.BuildSettingsScreen();
        }

        private void BuildPlayingUI()
        {
            _uiManager.BuildPlayingUI(_gameManager);


        }



        protected override void OnActivated(object sender, EventArgs args)
        {
            // 游戏恢复（如从后台返回）
            _isGameActive = true; // 恢复游戏
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            // 游戏进入后台（如切换应用）
            // 不再释放资源，仅暂停游戏
            _isGameActive = false; // 暂停游戏
            base.OnDeactivated(sender, args);
        }
    }
}
