// Game1.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private bool _isGameActive = true;
        private bool _previousEscPressed = false;

        // --- Network ---
        private GameServer _gameServer;
        private GameClient _gameClient;
        private ServerConfig _serverConfig;
        private bool _isHost;
        private bool _networkGameStarted;
        private string _networkStatusText = string.Empty;

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

            // 加载服务器配置
            _serverConfig = ServerConfig.Load("server.csv");

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
                case State.OnlineMenu:
                    HandleOnlineMenuInput();
                    break;
                case State.Hosting:
                    HandleHostingInput();
                    break;
                case State.Connecting:
                    HandleConnectingInput();
                    break;
                case State.OnlinePlaying:
                    HandleOnlinePlayingInput();
                    EnsureOnlineGameStarted();
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

        private void EnsureOnlineGameStarted()
        {
            if (!_networkGameStarted)
            {
                _networkGameStarted = true;

                if (_isHost)
                {
                    // 主机：本地玩家 ID=0，远程玩家 ID=1
                    var players = new List<Player>
                    {
                        new Player(0, "YOU"),
                        new Player(1, "OPPONENT")
                    };
                    _playingSceneManager.Initialize(players);
                    _playingSceneManager.StartGame();
                }
                else
                {
                    // 客机：本地玩家 ID=1，远程玩家 ID=0
                    var players = new List<Player>
                    {
                        new Player(0, "OPPONENT"),
                        new Player(1, "YOU")
                    };
                    _playingSceneManager.Initialize(players);
                    _playingSceneManager.StartGame();
                }
            }
        }

        private void HandleExitInput()
        {
            bool escPressed = Keyboard.GetState().IsKeyDown(Keys.Escape);

            if (_currentState == State.Playing || _currentState == State.OnlinePlaying)
            {
                if (escPressed && !_previousEscPressed)
                {
                    SwitchToState(State.Paused);
                }
            }
            else if (_currentState == State.Paused)
            {
                if (escPressed && !_previousEscPressed)
                {
                    // 恢复之前的状态
                    if (_isHost || _gameClient != null)
                    {
                        SwitchToState(State.OnlinePlaying);
                    }
                    else
                    {
                        SwitchToState(State.Playing);
                    }
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
            _uiManager.UpdateUIButtons(_uiManager.GameManager);

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
                            case 1: // 联机模式按钮
                                SwitchToState(State.OnlineMenu);
                                return;
                            case 2: // 退出按钮
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
                                if (_isHost || _gameClient != null)
                                {
                                    SwitchToState(State.OnlinePlaying);
                                }
                                else
                                {
                                    SwitchToState(State.Playing);
                                }
                                return;
                            case 1: // 返回按钮
                                CleanupNetwork();
                                SwitchToState(State.MainMenu);
                                return;
                        }
                    }
                }
            }
        }

        private void HandleOnlineMenuInput()
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
                            case 0: // 创建房间 (主机)
                                StartHosting();
                                return;
                            case 1: // 加入房间 (客机)
                                StartConnecting();
                                return;
                            case 2: // 返回
                                SwitchToState(State.MainMenu);
                                return;
                        }
                    }
                }
            }
        }

        private void HandleHostingInput()
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);

            // 检查是否有客户端连接
            if (_gameServer != null && _gameServer.HasClientConnected)
            {
                _networkStatusText = "对手已连接！正在开始游戏...";
                _uiManager.BuildHostingScreen(_networkStatusText);

                // 延迟一帧后开始游戏，让 UI 刷新
                Task.Delay(500).ContinueWith(_ =>
                {
                    _isHost = true;
                    _networkGameStarted = false;
                    SwitchToState(State.OnlinePlaying);
                });
                return;
            }

            foreach (var control in _uiManager.GetControls())
            {
                if (control is UIButton button)
                {
                    if (button.IsClicked())
                    {
                        switch (button.GetId())
                        {
                            case 0: // 取消
                                CleanupNetwork();
                                SwitchToState(State.OnlineMenu);
                                return;
                        }
                    }
                }
            }
        }

        private void HandleConnectingInput()
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);

            // 检查是否已连接
            if (_gameClient != null && _gameClient.IsConnected)
            {
                _networkStatusText = "已连接到主机！正在开始游戏...";
                _uiManager.BuildConnectingScreen(_networkStatusText);

                Task.Delay(500).ContinueWith(_ =>
                {
                    _isHost = false;
                    _networkGameStarted = false;
                    SwitchToState(State.OnlinePlaying);
                });
                return;
            }

            foreach (var control in _uiManager.GetControls())
            {
                if (control is UIButton button)
                {
                    if (button.IsClicked())
                    {
                        switch (button.GetId())
                        {
                            case 0: // 取消
                                CleanupNetwork();
                                SwitchToState(State.OnlineMenu);
                                return;
                        }
                    }
                }
            }
        }

        private void HandlePlayerInput()
        {
            var gameManager = _uiManager.GameManager;

            _uiManager.UpdateUIButtons(gameManager);

            if (gameManager.CurrentPlayerId != 0)
                return;

            var clickAction = _uiManager.UpdateMouseAndCheckClicks(gameManager);

            if (clickAction.Type != ActionType.None)
            {
                gameManager.UpdateGame(clickAction);
            }
        }

        private void HandleOnlinePlayingInput()
        {
            var gameManager = _uiManager.GameManager;

            _uiManager.UpdateUIButtons(gameManager);

            int localPlayerId = _isHost ? 0 : 1;

            // 处理网络消息（接收远程玩家的动作）
            ProcessNetworkMessages();

            // 只有当前是本地玩家回合时才处理点击
            if (gameManager.CurrentPlayerId != localPlayerId)
                return;

            var clickAction = _uiManager.UpdateMouseAndCheckClicks(gameManager);

            if (clickAction.Type != ActionType.None)
            {
                // 将本地动作发送给远程玩家
                SendActionToRemote(clickAction);
                gameManager.UpdateGame(clickAction);
            }
        }

        private void ProcessNetworkMessages()
        {
            if (_isHost && _gameServer != null)
            {
                var messages = _gameServer.ReceiveMessages();
                foreach (var msg in messages)
                {
                    if (msg.Type == MessageType.GameAction)
                    {
                        var action = NetworkMessage.DeserializeAction(msg.Payload);
                        _uiManager.GameManager.UpdateGame(action);
                    }
                }
            }
            else if (!_isHost && _gameClient != null)
            {
                var messages = _gameClient.ReceiveMessages();
                foreach (var msg in messages)
                {
                    if (msg.Type == MessageType.GameAction)
                    {
                        var action = NetworkMessage.DeserializeAction(msg.Payload);
                        _uiManager.GameManager.UpdateGame(action);
                    }
                }
            }
        }

        private void SendActionToRemote(Fida.Logic.Action action)
        {
            var payload = NetworkMessage.SerializeAction(action);
            var message = new NetworkMessage(MessageType.GameAction, payload);

            if (_isHost && _gameServer != null)
            {
                _gameServer.SendToClient(message);
            }
            else if (!_isHost && _gameClient != null)
            {
                _gameClient.SendMessage(message);
            }
        }

        private void StartHosting()
        {
            _isHost = true;
            _networkGameStarted = false;

            // 使用 CSV 中第一个服务器的地址
            string ipAddress = _serverConfig?.GetServerAddress() ?? "::1";
            int port = _serverConfig?.GetServerPort() ?? 8888;

            _gameServer = new GameServer(ipAddress, port);
            _gameServer.Start();

            _networkStatusText = $"等待对手连接...\n地址: [{ipAddress}]:{port}";
            SwitchToState(State.Hosting);
        }

        private void StartConnecting()
        {
            _isHost = false;
            _networkGameStarted = false;

            string ipAddress = _serverConfig?.GetServerAddress() ?? "::1";
            int port = _serverConfig?.GetServerPort() ?? 8888;

            _gameClient = new GameClient();
            _gameClient.Connect(ipAddress, port);

            _networkStatusText = $"正在连接到 [{ipAddress}]:{port} ...";
            SwitchToState(State.Connecting);
        }

        private void CleanupNetwork()
        {
            _gameServer?.Stop();
            _gameServer = null;

            _gameClient?.Disconnect();
            _gameClient = null;

            _isHost = false;
            _networkGameStarted = false;
            _networkStatusText = string.Empty;
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
                        _uiManager.BuildPlayingUI(_uiManager.GameManager);
                    }
                    break;
                case State.Paused:
                    _uiManager.BuildPauseMenu();
                    break;
                case State.GameOver:
                    break;
                case State.OnlineMenu:
                    _uiManager.BuildOnlineMenu();
                    break;
                case State.Hosting:
                    _uiManager.BuildHostingScreen(_networkStatusText);
                    break;
                case State.Connecting:
                    _uiManager.BuildConnectingScreen(_networkStatusText);
                    break;
                case State.OnlinePlaying:
                    _uiManager.BuildPlayingUI(_uiManager.GameManager);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupNetwork();
                _uiManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}