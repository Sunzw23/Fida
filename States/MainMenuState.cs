using System;
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 主菜单状态 - 处理开始游戏、联机、退出
    /// 使用事件驱动方式绑定按钮点击
    /// </summary>
    public class MainMenuState : StateBase
    {
        private bool _gameStarting;

        public MainMenuState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            _gameStarting = false;

            if (_networkManager.IsConnected)
            {
                if (_networkManager.IsHost)
                {
                    BindButton("startGame", () => RequestMode(1));
                    BindButton("duel", () => RequestMode(0));
                    _networkManager.LobbyGameModeAccepted += OnGameModeAccepted;
                }
                else
                {
                    // Guests make no authoritative mode choice; disabled controls make
                    // that ownership visible while they remain on the same main screen.
                    DisableButton("startGame");
                    DisableButton("duel");
                    _networkManager.LobbyGameModeRequested += OnGameModeRequested;
                    _networkManager.LobbyGameStarting += OnGameStarting;
                }

                BindButton("connectionSettings", () =>
                    _stateMachine.SwitchToState(State.OnlineMenu));
                BindButton("disconnect", () =>
                {
                    _networkManager.Cleanup();
                    _stateMachine.SwitchToState(State.MainMenu);
                });
            }
            else
            {
                BindButton("startGame", () => _stateMachine.SwitchToState(State.LevelSelect));
            }

            BindButton("onlineMode", () =>
            {
                // 如果已连接则直接进入大厅，否则进入联机菜单
                if (_networkManager.IsConnected)
                    _stateMachine.SwitchToState(State.OnlineLobby);
                else
                    _stateMachine.SwitchToState(State.OnlineMenu);
            });
            BindButton("settings", () => _stateMachine.SwitchToState(State.Settings));
            BindButton("exit", () => Environment.Exit(0));
        }

        public override void OnExit()
        {
            _networkManager.LobbyGameModeAccepted -= OnGameModeAccepted;
            _networkManager.LobbyGameModeRequested -= OnGameModeRequested;
            _networkManager.LobbyGameStarting -= OnGameStarting;
            base.OnExit();
        }

        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);
            if (!_networkManager.IsConnected) return;
            _networkManager.ProcessLobbyMessages();
            if (_gameStarting)
            {
                _gameStarting = false;
                _stateMachine.SwitchToState(_networkManager.CurrentGameMode == 0
                    ? State.DuelDraft
                    : State.OnlinePlaying);
            }
        }

        private void RequestMode(int mode)
        {
            if (!_networkManager.IsHost) return;
            _networkManager.CurrentGameMode = mode;
            _networkManager.SendLobbyGameModeRequest(mode);
        }

        private void OnGameModeAccepted(int mode)
        {
            _networkManager.CurrentGameMode = mode;
            _networkManager.SendLobbyGameStartResult(mode);
            _gameStarting = true;
        }

        private void OnGameModeRequested(int mode)
        {
            _networkManager.CurrentGameMode = mode;
            _networkManager.SendLobbyGameModeAccept(mode);
        }

        private void OnGameStarting(int mode)
        {
            _networkManager.CurrentGameMode = mode;
            _gameStarting = true;
        }

        private void DisableButton(string name)
        {
            var button = _uiManager.GetButtonByName(name);
            if (button != null) button.EnabledChecker = () => false;
        }
    }
}
