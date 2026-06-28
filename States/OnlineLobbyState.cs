using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 联机大厅状态 - 连接保持后进入，主机选择游戏模式
    /// 主机点击按钮后发送请求给客机，客机自动接受，然后双方进入游戏
    /// </summary>
    public class OnlineLobbyState : StateBase
    {
        private bool _gameStarting;

        public OnlineLobbyState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            _gameStarting = false;

            // 主机：绑定游戏模式选择按钮
            if (_networkManager.IsHost)
            {
                BindButton("startPvP", () =>
                {
                    // 与好友对战 (PvP) - mode 0
                    _networkManager.CurrentGameMode = 0;
                    _networkManager.SendLobbyGameModeRequest(0);
                });

                BindButton("startCoop", () =>
                {
                    // 与好友合作对战Agent (Co-op) - mode 1
                    _networkManager.CurrentGameMode = 1;
                    _networkManager.SendLobbyGameModeRequest(1);
                });

                // 主机监听客机接受响应
                _networkManager.LobbyGameModeAccepted += OnGameModeAccepted;
            }
            else
            {
                // 客机监听主机发来的游戏模式请求
                _networkManager.LobbyGameModeRequested += OnGameModeRequested;
                // 客机监听游戏开始确认
                _networkManager.LobbyGameStarting += OnGameStarting;
            }

            // 双方都有断开连接按钮（回到联机菜单）
            BindButton("disconnect", () =>
            {
                UnsubscribeEvents();
                _networkManager.Cleanup();
                _stateMachine.SwitchToState(State.OnlineMenu);
            });

            // 返回主菜单按钮（不中断连接）
            BindButton("backToMain", () =>
            {
                UnsubscribeEvents();
                _stateMachine.SwitchToState(State.MainMenu);
            });
        }

        public override void OnExit()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            _networkManager.LobbyGameModeAccepted -= OnGameModeAccepted;
            _networkManager.LobbyGameModeRequested -= OnGameModeRequested;
            _networkManager.LobbyGameStarting -= OnGameStarting;
        }

        private void OnGameModeAccepted(int mode)
        {
            // 客机已接受，主机发送开始确认并进入游戏
            _networkManager.SendLobbyGameStartResult(mode);
            _gameStarting = true;
        }

        private void OnGameModeRequested(int mode)
        {
            // 客机收到主机发来的游戏模式请求，自动接受
            _networkManager.CurrentGameMode = mode;
            _networkManager.SendLobbyGameModeAccept(mode);
        }

        private void OnGameStarting(int mode)
        {
            // 客机收到游戏开始确认，进入游戏
            _networkManager.CurrentGameMode = mode;
            _gameStarting = true;
        }

        /// <summary>
        /// 处理输入 + 大厅消息
        /// </summary>
        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);

            // 处理大厅消息
            _networkManager.ProcessLobbyMessages();

            // 检查是否应该开始游戏
            if (_gameStarting)
            {
                UnsubscribeEvents();
                _stateMachine.SwitchToState(State.OnlinePlaying);
            }
        }
    }
}