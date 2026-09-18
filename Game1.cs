// Game1.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
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
        private ResourceManager _resourceManager;
        private UIManager _uiManager;
        private AgentManager _agentManager;
        private PlayingSceneManager _playingSceneManager;
        private NetworkManager _networkManager;

        private GameStateMachine _stateMachine;
        private MenuInputHandler _menuInput;
        private GameSessionManager _gameSession;
        private ExitHandler _exitHandler;

        private bool _isGameActive = true;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1200;
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            _graphics.PreferMultiSampling = true;
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();
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

            // 创建重构后的管理器
            _stateMachine = new GameStateMachine(_uiManager, _playingSceneManager, _networkManager);
            _menuInput = new MenuInputHandler(_uiManager, _networkManager, _stateMachine);
            _gameSession = new GameSessionManager(_uiManager, _playingSceneManager, _networkManager, _stateMachine);
            _exitHandler = new ExitHandler(_stateMachine, _networkManager, this);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _resourceManager.LoadContent(Content);

            if (_stateMachine.CurrentState == State.MainMenu)
            {
                _uiManager.BuildMainMenu(_networkManager.IsConnected, _networkManager.IsHost);
                _menuInput.OnInitialEnter();
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (!_isGameActive)
            {
                base.Update(gameTime);
                return;
            }

            _exitHandler.HandleExitInput();
            _uiManager.Update(gameTime);
            _uiManager.UpdateInputMode();

            // 根据输入模式切换鼠标可见性
            IsMouseVisible = _uiManager.CurrentInputMode == InputMode.Mouse;

            switch (_stateMachine.CurrentState)
            {
                case State.MainMenu:      _menuInput.HandleMenuInput(gameTime); break;
                case State.LevelSelect:   _menuInput.HandleMenuInput(gameTime); break;
                case State.Skills:        _menuInput.HandleMenuInput(gameTime); break;
                case State.GameOver:      _menuInput.HandleMenuInput(gameTime); break;
                case State.Playing:
                    _gameSession.EnsureGameStarted();
                    if (_stateMachine.CurrentState == State.Playing)
                        _gameSession.HandlePlayerInput(gameTime);
                    break;
                case State.Paused:        _menuInput.HandleMenuInput(gameTime); break;
                case State.OnlineMenu:    _menuInput.HandleMenuInput(gameTime); break;
                case State.Hosting:       _menuInput.HandleHostingInput(gameTime); break;
                case State.Connecting:    _menuInput.HandleConnectingInput(gameTime); break;
                case State.OnlineLobby:   _menuInput.HandleMenuInput(gameTime); break;
                case State.DuelDraft:     _menuInput.HandleMenuInput(gameTime); break;
                case State.OnlinePlaying:
                    _gameSession.EnsureOnlineGameStarted();
                    if (_stateMachine.CurrentState == State.OnlinePlaying)
                        _gameSession.HandleOnlinePlayingInput(gameTime);
                    break;
            }

            base.Update(gameTime);
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
                _resourceManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
