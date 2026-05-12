// Game1.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Fida.Network;
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
        private NetworkManager _networkManager;

        private bool _isGameActive = true;
        private bool _previousEscPressed = false;
        private bool _networkGameStarted;

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
            _networkManager = new NetworkManager();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _resourceManager.LoadContent(Content);

            if (_currentState == State.MainMenu)
                _uiManager.BuildMainMenu();
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
            _uiManager.UpdateInputMode();

            // 根据输入模式切换鼠标可见性
            IsMouseVisible = _uiManager.CurrentInputMode == InputMode.Mouse;

            switch (_currentState)
            {
                case State.MainMenu:      HandleMainMenuInput(gameTime); break;
                case State.Playing:       HandlePlayerInput(gameTime); EnsureGameStarted(); break;
                case State.Paused:        HandlePausedInput(gameTime); break;
                case State.OnlineMenu:    HandleOnlineMenuInput(gameTime); break;
                case State.Hosting:       HandleHostingInput(gameTime); break;
                case State.Connecting:    HandleConnectingInput(gameTime); break;
                case State.OnlinePlaying: HandleOnlinePlayingInput(gameTime); EnsureOnlineGameStarted(); break;
            }

            base.Update(gameTime);
        }

        // ─── 单机 ──────────────────────────────────────────

        private void EnsureGameStarted()
        {
            if (!_playingSceneManager.IsInitialized)
            {
                _playingSceneManager.Initialize(SceneConfig.PlayersScene1.Players);
                _playingSceneManager.StartGame();
            }
        }

        private void HandlePlayerInput(GameTime gameTime)
        {
            var gm = _uiManager.GameManager;
            _uiManager.UpdateUIButtons(gm);

            // 手柄输入更新
            _uiManager.UpdateControllerInput(gameTime);

            if (gm.CurrentPlayerId != 0)
                return;

            var action = _uiManager.UpdateMouseAndCheckClicks(gm);

            // 手柄确认键
            if (action.Type == ActionType.None)
                action = _uiManager.CheckControllerConfirm(gm);

            if (action.Type != ActionType.None)
                gm.UpdateGame(action);
        }

        // ─── 联机 ──────────────────────────────────────────

        private void EnsureOnlineGameStarted()
        {
            if (_networkGameStarted) return;
            _networkGameStarted = true;

            int localId = _networkManager.GetLocalPlayerId();
            int remoteId = localId == 0 ? 1 : 0;

            var players = new List<Player>
            {
                new Player(localId, "YOU"),
                new Player(remoteId, "OPPONENT")
            };

            // 确保 Player 列表按 ID 排序
            if (localId == 1)
                players.Reverse();

            _playingSceneManager.Initialize(players);
            _playingSceneManager.StartGame();
        }

        private void HandleOnlinePlayingInput(GameTime gameTime)
        {
            var gm = _uiManager.GameManager;
            _uiManager.UpdateUIButtons(gm);

            // 手柄输入更新
            _uiManager.UpdateControllerInput(gameTime);

            _networkManager.ProcessMessages(gm);

            if (gm.CurrentPlayerId != _networkManager.GetLocalPlayerId())
                return;

            var action = _uiManager.UpdateMouseAndCheckClicks(gm);

            // 手柄确认键
            if (action.Type == ActionType.None)
                action = _uiManager.CheckControllerConfirm(gm);

            if (action.Type != ActionType.None)
            {
                _networkManager.SendAction(action);
                gm.UpdateGame(action);
            }
        }

        // ─── 菜单输入处理 ──────────────────────────────────

        private void HandleExitInput()
        {
            bool escPressed = Keyboard.GetState().IsKeyDown(Keys.Escape);

            if (_currentState == State.Playing || _currentState == State.OnlinePlaying)
            {
                if (escPressed && !_previousEscPressed)
                    SwitchToState(State.Paused);
            }
            else if (_currentState == State.Paused)
            {
                if (escPressed && !_previousEscPressed)
                    SwitchToState(_networkManager.IsConnected ? State.OnlinePlaying : State.Playing);
            }
            else if (escPressed || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            _previousEscPressed = escPressed;
        }

        /// <summary>
        /// 处理菜单按钮点击（鼠标或手柄确认键）
        /// </summary>
        private bool HandleMenuButtonClick()
        {
            foreach (var c in _uiManager.GetControls())
            {
                if (c is UIButton b && b.IsClicked())
                {
                    return HandleButtonById(b.GetId());
                }
            }
            return false;
        }

        /// <summary>
        /// 处理手柄菜单按钮点击
        /// </summary>
        private bool HandleControllerMenuButtonClick()
        {
            var controller = _uiManager.GetControls().Count > 0 ? 
                new ControllerInputHandler(PlayerIndex.One) : null;
            
            foreach (var c in _uiManager.GetControls())
            {
                if (c is UIButton b && b.IsFocused && b.IsClicked())
                {
                    return HandleButtonById(b.GetId());
                }
            }
            return false;
        }

        /// <summary>
        /// 根据按钮 ID 执行菜单操作
        /// </summary>
        private bool HandleButtonById(int id)
        {
            switch (_currentState)
            {
                case State.MainMenu:
                    switch (id)
                    {
                        case 0: SwitchToState(State.Playing); return true;
                        case 1: SwitchToState(State.OnlineMenu); return true;
                        case 2: Exit(); return true;
                    }
                    break;
                case State.Paused:
                    switch (id)
                    {
                        case 0:
                            SwitchToState(_networkManager.IsConnected ? State.OnlinePlaying : State.Playing);
                            return true;
                        case 1:
                            _networkManager.Cleanup();
                            SwitchToState(State.MainMenu);
                            return true;
                    }
                    break;
                case State.OnlineMenu:
                    switch (id)
                    {
                        case 0:
                            _networkManager.StartHosting(_networkManager.GetServerAddress(), _networkManager.GetServerPort());
                            SwitchToState(State.Hosting);
                            return true;
                        case 1:
                            _networkManager.StartConnecting(_networkManager.GetServerAddress(), _networkManager.GetServerPort());
                            SwitchToState(State.Connecting);
                            return true;
                        case 2:
                            SwitchToState(State.MainMenu);
                            return true;
                    }
                    break;
                case State.Hosting:
                    if (id == 0)
                    {
                        _networkManager.Cleanup();
                        SwitchToState(State.OnlineMenu);
                        return true;
                    }
                    break;
                case State.Connecting:
                    if (id == 0)
                    {
                        _networkManager.Cleanup();
                        SwitchToState(State.OnlineMenu);
                        return true;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 处理手柄菜单确认键 - 使用 UIManager 中已更新的 ControllerInputHandler
        /// </summary>
        private bool HandleControllerMenuConfirm()
        {
            var controller = _uiManager.GetControls().Count > 0 ? 
                _uiManager.GetControllerInput() : null;
            if (controller != null && controller.IsConnected && controller.IsConfirmPressed())
            {
                foreach (var c in _uiManager.GetControls())
                {
                    if (c is UIButton b && b.IsFocused)
                    {
                        return HandleButtonById(b.GetId());
                    }
                }
            }
            return false;
        }

        private void HandleMainMenuInput(GameTime gameTime)
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);
            _uiManager.UpdateControllerInput(gameTime);
            if (HandleMenuButtonClick()) return;
            HandleControllerMenuConfirm();
        }

        private void HandlePausedInput(GameTime gameTime)
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);
            _uiManager.UpdateControllerInput(gameTime);
            if (HandleMenuButtonClick()) return;
            HandleControllerMenuConfirm();
        }

        private void HandleOnlineMenuInput(GameTime gameTime)
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);
            _uiManager.UpdateControllerInput(gameTime);
            if (HandleMenuButtonClick()) return;
            HandleControllerMenuConfirm();
        }

        private void HandleHostingInput(GameTime gameTime)
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);
            _uiManager.UpdateControllerInput(gameTime);

            if (_networkManager.CheckHostConnection())
            {
                _uiManager.BuildHostingScreen(_networkManager.StatusText);
                _networkGameStarted = false;
                SwitchToState(State.OnlinePlaying);
                return;
            }

            if (HandleMenuButtonClick()) return;
            HandleControllerMenuConfirm();
        }

        private void HandleConnectingInput(GameTime gameTime)
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);
            _uiManager.UpdateControllerInput(gameTime);

            if (_networkManager.IsConnected)
            {
                _uiManager.BuildConnectingScreen(_networkManager.StatusText);
                _networkGameStarted = false;
                SwitchToState(State.OnlinePlaying);
                return;
            }

            if (HandleMenuButtonClick()) return;
            HandleControllerMenuConfirm();
        }

        // ─── 状态切换 ──────────────────────────────────────

        public void SwitchToState(State newState)
        {
            _currentState = newState;
            _uiManager.Clear();

            switch (newState)
            {
                case State.MainMenu:      _uiManager.BuildMainMenu(); break;
                case State.Rules:         _uiManager.BuildRulesScreen(); break;
                case State.Settings:      _uiManager.BuildSettingsScreen(); break;
                case State.Playing:
                    if (!_playingSceneManager.IsInitialized)
                    {
                        _playingSceneManager.Initialize(SceneConfig.PlayersScene1.Players);
                        _playingSceneManager.StartGame();
                    }
                    else _uiManager.BuildPlayingUI(_uiManager.GameManager);
                    break;
                case State.Paused:        _uiManager.BuildPauseMenu(); break;
                case State.GameOver:      break;
                case State.OnlineMenu:    _uiManager.BuildOnlineMenu(); break;
                case State.Hosting:       _uiManager.BuildHostingScreen(_networkManager.StatusText); break;
                case State.Connecting:    _uiManager.BuildConnectingScreen(_networkManager.StatusText); break;
                case State.OnlinePlaying: _uiManager.BuildPlayingUI(_uiManager.GameManager); break;
            }
        }

        // ─── MonoGame 生命周期 ─────────────────────────────

        protected override void Draw(GameTime gameTime)
        {
            _uiManager.Draw(gameTime);
            base.Draw(gameTime);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _networkManager?.Dispose();
                _uiManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}