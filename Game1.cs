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

            switch (_currentState)
            {
                case State.MainMenu:      HandleMainMenuInput(); break;
                case State.Playing:       HandlePlayerInput(); EnsureGameStarted(); break;
                case State.Paused:        HandlePausedInput(); break;
                case State.OnlineMenu:    HandleOnlineMenuInput(); break;
                case State.Hosting:       HandleHostingInput(); break;
                case State.Connecting:    HandleConnectingInput(); break;
                case State.OnlinePlaying: HandleOnlinePlayingInput(); EnsureOnlineGameStarted(); break;
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

        private void HandlePlayerInput()
        {
            var gm = _uiManager.GameManager;
            _uiManager.UpdateUIButtons(gm);

            if (gm.CurrentPlayerId != 0)
                return;

            var action = _uiManager.UpdateMouseAndCheckClicks(gm);
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

        private void HandleOnlinePlayingInput()
        {
            var gm = _uiManager.GameManager;
            _uiManager.UpdateUIButtons(gm);

            _networkManager.ProcessMessages(gm);

            if (gm.CurrentPlayerId != _networkManager.GetLocalPlayerId())
                return;

            var action = _uiManager.UpdateMouseAndCheckClicks(gm);
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

        private void HandleMainMenuInput()
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);
            foreach (var c in _uiManager.GetControls())
            {
                if (c is UIButton b && b.IsClicked())
                {
                    switch (b.GetId())
                    {
                        case 0: SwitchToState(State.Playing); return;
                        case 1: SwitchToState(State.OnlineMenu); return;
                        case 2: Exit(); return;
                    }
                }
            }
        }

        private void HandlePausedInput()
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);
            foreach (var c in _uiManager.GetControls())
            {
                if (c is UIButton b && b.IsClicked())
                {
                    switch (b.GetId())
                    {
                        case 0:
                            SwitchToState(_networkManager.IsConnected ? State.OnlinePlaying : State.Playing);
                            return;
                        case 1:
                            _networkManager.Cleanup();
                            SwitchToState(State.MainMenu);
                            return;
                    }
                }
            }
        }

        private void HandleOnlineMenuInput()
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);
            foreach (var c in _uiManager.GetControls())
            {
                if (c is UIButton b && b.IsClicked())
                {
                    switch (b.GetId())
                    {
                        case 0:
                            _networkManager.StartHosting(_networkManager.GetServerAddress(), _networkManager.GetServerPort());
                            SwitchToState(State.Hosting);
                            return;
                        case 1:
                            _networkManager.StartConnecting(_networkManager.GetServerAddress(), _networkManager.GetServerPort());
                            SwitchToState(State.Connecting);
                            return;
                        case 2:
                            SwitchToState(State.MainMenu);
                            return;
                    }
                }
            }
        }

        private void HandleHostingInput()
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);

            if (_networkManager.CheckHostConnection())
            {
                _uiManager.BuildHostingScreen(_networkManager.StatusText);
                _networkGameStarted = false;
                SwitchToState(State.OnlinePlaying);
                return;
            }

            foreach (var c in _uiManager.GetControls())
            {
                if (c is UIButton b && b.IsClicked() && b.GetId() == 0)
                {
                    _networkManager.Cleanup();
                    SwitchToState(State.OnlineMenu);
                    return;
                }
            }
        }

        private void HandleConnectingInput()
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);

            if (_networkManager.IsConnected)
            {
                _uiManager.BuildConnectingScreen(_networkManager.StatusText);
                _networkGameStarted = false;
                SwitchToState(State.OnlinePlaying);
                return;
            }

            foreach (var c in _uiManager.GetControls())
            {
                if (c is UIButton b && b.IsClicked() && b.GetId() == 0)
                {
                    _networkManager.Cleanup();
                    SwitchToState(State.OnlineMenu);
                    return;
                }
            }
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