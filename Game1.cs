// Game1.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Fida.Scene;
using Fida.UI;
using Fida.Agent;
using Fida.Logic;

namespace Fida
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private State _currentState;
        private ResourceManager _resourceManager;
        private UIManager _uiManager;
        private AgentManager _agentManager;
        private PlayingSceneManager _playingSceneManager;

        private bool _isGameActive = true;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1200;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            _currentState = State.Playing;
        }

        protected override void Initialize()
        {
            _resourceManager = new ResourceManager(GraphicsDevice);
            _uiManager = new UIManager(GraphicsDevice, _resourceManager);
            _agentManager = new AgentManager();
            _agentManager.LoadAgents();
            _agentManager.SetGameManager(_uiManager.GameManager);
            _playingSceneManager = new PlayingSceneManager(_uiManager.GameManager, _uiManager, _agentManager);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _resourceManager.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (!_isGameActive)
            {
                base.Update(gameTime);
                return;
            }

            HandleExitInput();
            _uiManager.Update(gameTime);
            HandlePlayerInput();
            EnsureGameStarted();

            base.Update(gameTime);
        }

        private void EnsureGameStarted()
        {
            if (!_playingSceneManager.IsInitialized)
            {
                _playingSceneManager.Initialize(SceneConfig.PlayersScene1.Players);
                _playingSceneManager.StartGame();
            }
        }

        private void HandleExitInput()
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }
        }

        private void HandlePlayerInput()
        {
            var gameManager = _uiManager.GameManager;
            
            // 始终更新 UI 按钮状态（让按钮通过 EnabledChecker 询问环境来决定是否启用）
            _uiManager.UpdateUIButtons(gameManager);
            
            // 只有当前是玩家回合时才处理点击
            if (gameManager.CurrentPlayerId != 0)
                return;

            var clickAction = _uiManager.UpdateMouseAndCheckClicks(gameManager);
            
            if (clickAction.Type != ActionType.None)
            {
                gameManager.UpdateGame(clickAction);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            _uiManager.Draw(gameTime);
            base.Draw(gameTime);
        }

        public void SwitchToState(State newState)
        {
            _currentState = newState;
            _uiManager.Clear();

            switch (newState)
            {
                case State.MainMenu:
                    _uiManager.BuildMainMenu();
                    break;
                case State.Rules:
                    _uiManager.BuildRulesScreen();
                    break;
                case State.Settings:
                    _uiManager.BuildSettingsScreen();
                    break;
                case State.Playing:
                    _playingSceneManager.Initialize(SceneConfig.PlayersScene1.Players);
                    _playingSceneManager.StartGame();
                    break;
                case State.GameOver:
                    break;
            }
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            _isGameActive = true;
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            _isGameActive = false;
            base.OnDeactivated(sender, args);
        }
    }
}
