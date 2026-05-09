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
        private bool _previousEscPressed = false;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1200;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            _currentState = State.MainMenu;
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

            if (_currentState == State.MainMenu)
            {
                _uiManager.BuildMainMenu();
            }
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

            switch (_currentState)
            {
                case State.MainMenu:
                    HandleMainMenuInput();
                    break;
                case State.Playing:
                    HandlePlayerInput();
                    EnsureGameStarted();
                    break;
                case State.Paused:
                    HandlePausedInput();
                    break;
            }

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
            bool escPressed = Keyboard.GetState().IsKeyDown(Keys.Escape);

            if (_currentState == State.Playing)
            {
                // Playing 状态下按 ESC → 进入暂停菜单
                if (escPressed && !_previousEscPressed)
                {
                    SwitchToState(State.Paused);
                }
            }
            else if (_currentState == State.Paused)
            {
                // Paused 状态下按 ESC → 继续游戏
                if (escPressed && !_previousEscPressed)
                {
                    SwitchToState(State.Playing);
                }
            }
            else if (escPressed || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            _previousEscPressed = escPressed;
        }

        private void HandleMainMenuInput()
        {
            // 更新主菜单按钮的悬浮/按下状态
            _uiManager.UpdateUIButtons(_uiManager.GameManager);

            var mouseState = Mouse.GetState();
            bool leftClick = mouseState.LeftButton == ButtonState.Pressed;

            // 只在鼠标释放时检测点击（通过 UIButton.IsClicked 判断完整点击序列）
            foreach (var control in _uiManager.GetControls())
            {
                if (control is UIButton button)
                {
                    if (button.IsClicked())
                    {
                        switch (button.GetId())
                        {
                            case 0: // 开始按钮
                                SwitchToState(State.Playing);
                                return;
                            case 1: // 退出按钮
                                Exit();
                                return;
                        }
                    }
                }
            }
        }

        private void HandlePausedInput()
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);

            foreach (var control in _uiManager.GetControls())
            {
                if (control is UIButton button)
                {
                    if (button.IsClicked())
                    {
                        switch (button.GetId())
                        {
                            case 0: // 继续按钮
                                SwitchToState(State.Playing);
                                return;
                            case 1: // 返回按钮
                                SwitchToState(State.MainMenu);
                                return;
                        }
                    }
                }
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
                    if (!_playingSceneManager.IsInitialized)
                    {
                        _playingSceneManager.Initialize(SceneConfig.PlayersScene1.Players);
                        _playingSceneManager.StartGame();
                    }
                    else
                    {
                        // 从暂停恢复，只重建 UI，不重置游戏
                        _uiManager.BuildPlayingUI(_uiManager.GameManager);
                    }
                    break;
                case State.Paused:
                    _uiManager.BuildPauseMenu();
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