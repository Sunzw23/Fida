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
        // MonoGame initializes these in Initialize(), after the constructor.
        private ResourceManager _resourceManager = null!;
        private UIManager _uiManager = null!;
        private AgentManager _agentManager = null!;
        private PlayingSceneManager _playingSceneManager = null!;
        private NetworkManager _networkManager = null!;

        private GameStateMachine _stateMachine = null!;
        private MenuInputHandler _menuInput = null!;
        private GameSessionManager _gameSession = null!;
        private ExitHandler _exitHandler = null!;

        private bool _isGameActive = true;

        // F8 布局重叠调试开关（边沿检测，仅桌面构建）
        private bool _overlapDebugWasDown;
        private bool _rulesShortcutWasDown;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

#if FIDA_ANDROID
            IsMouseVisible = false;
            _graphics.GraphicsProfile = GraphicsProfile.Reach;
            _graphics.IsFullScreen = true;
            DisplaySettingsManager.InitializeMobile(_graphics);
#else
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
#endif
            _graphics.PreferMultiSampling = true;
            _graphics.SynchronizeWithVerticalRetrace = true;
#if !FIDA_ANDROID
            DisplaySettingsManager.Initialize(_graphics);
#endif
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

            _exitHandler.HandleExitInput(_uiManager.TutorialDialogue != null);
            _uiManager.Update(gameTime);
            _uiManager.UpdateInputMode();
            _gameSession.ProcessPendingNetworkEvents();

            bool rulesShortcutDown = Keyboard.GetState().IsKeyDown(Keys.Tab);
            if (rulesShortcutDown && !_rulesShortcutWasDown)
            {
                if (_stateMachine.CurrentState == State.Rules)
                    _stateMachine.CloseRules();
                else if (_stateMachine.CurrentState is State.MainMenu or State.Paused or
                         State.Playing or State.OnlinePlaying && _uiManager.TutorialDialogue == null)
                    _stateMachine.OpenRules();
            }
            _rulesShortcutWasDown = rulesShortcutDown;

#if !FIDA_ANDROID
            // F8 开关布局重叠调试器（任意屏幕，边沿检测避免连按）
            bool overlapDebugDown = Keyboard.GetState().IsKeyDown(Keys.F8);
            if (overlapDebugDown && !_overlapDebugWasDown)
                UIOverlapDebugger.Toggle();
            _overlapDebugWasDown = overlapDebugDown;
#endif

            // 根据输入模式切换鼠标可见性
#if !FIDA_ANDROID
            IsMouseVisible = _uiManager.CurrentInputMode == InputMode.Mouse;
#endif

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
                case State.Rules:         _menuInput.HandleMenuInput(gameTime); break;
                case State.Settings:      _menuInput.HandleMenuInput(gameTime); break;
                case State.OnlineMenu:    _menuInput.HandleMenuInput(gameTime); break;
                case State.Hosting:       _menuInput.HandleHostingInput(gameTime); break;
                case State.Connecting:    _menuInput.HandleConnectingInput(gameTime); break;
                case State.OnlineLobby:   _menuInput.HandleMenuInput(gameTime); break;
                case State.DuelDraft:     _menuInput.HandleMenuInput(gameTime); break;
                case State.OnlineCampaignMap:
                case State.OnlineCampaignSkills: _menuInput.HandleMenuInput(gameTime); break;
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
