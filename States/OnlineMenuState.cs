#nullable enable
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 联机菜单状态 - 处理创建主机、连接、返回
    /// 使用事件驱动方式绑定按钮点击
    ///
    /// Two transports share this screen. A relay session (server.csv first row flagged
    /// "relay") pairs players with a four-character room code. A direct session has no
    /// server at all: Create game listens on every adapter and broadcasts on the LAN, and
    /// Join scans for that broadcast or dials the address the host is displaying.
    /// </summary>
    public class OnlineMenuState : StateBase
    {
        private bool _submitRequested;
        public OnlineMenuState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            _submitRequested = false;
            if (_networkManager.IsConnected)
            {
                BindButton("backToMain", () => _stateMachine.SwitchToState(State.MainMenu));
                BindButton("disconnect", () =>
                {
                    _networkManager.Cleanup();
                    _stateMachine.SwitchToState(State.MainMenu);
                });
                return;
            }

            bool direct = !_networkManager.GetServerIsRelay();

            BindButton("hostGame", () =>
            {
                if (direct)
                    _networkManager.StartHostingDirect(_networkManager.GetServerPort());
                else
                    _networkManager.StartHosting(_networkManager.GetServerAddress(),
                        _networkManager.GetServerPort(), isRelay: true);
                // Always show the waiting screen: when the listener could not bind it prints the
                // reason there instead of failing silently on this menu.
                _stateMachine.SwitchToState(State.Hosting);
            });

            if (direct)
            {
                BindAddressField("addressInput", _ => _submitRequested = true);
            }
            else
            {
                var input = _uiManager.GetTextBoxByName("roomCodeInput");
                if (input != null) input.IsVisible = true;
                var art = _uiManager.GetControlByName<UI.UIReferenceSurface>("online_art");
                if (art != null) art.RoomCodeRequired = true;
                var joinBox = _uiManager.GetButtonByName("joinGame");
                if (joinBox != null) joinBox.EnabledChecker = () => input?.Value.Length == 4;
                BindTextBox("roomCodeInput", _ => _submitRequested = true);
            }

            BindButton("joinGame", JoinGame);
            // Defer keyboard submission until state input handling, so the UI control
            // list is not rebuilt while its Update loop is enumerating it.
            BindButton("backToMain", () =>
                _stateMachine.SwitchToState(State.MainMenu));
        }

        private void JoinGame()
        {
            if (!_networkManager.GetServerIsRelay())
            {
                string typed = _uiManager.GetAddressFieldByName("addressInput")?.Value?.Trim() ?? string.Empty;
                if (typed.Length == 0)
                {
                    // No address typed: look for a host broadcasting on this network.
                    _networkManager.StartLanSearch();
                }
                else if (!_networkManager.StartConnectingDirect(typed))
                {
                    // The connecting screen prints the parse error instead of failing silently.
                }
                _stateMachine.SwitchToState(State.Connecting);
                return;
            }

            string? roomCode = _uiManager.GetTextBoxByName("roomCodeInput")?.Value
                ?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 4)
                return; // 房间码未输入/不全，停留原地

            _networkManager.StartConnecting(_networkManager.GetServerAddress(), _networkManager.GetServerPort(),
                isRelay: true, roomCode);
            _stateMachine.SwitchToState(State.Connecting);
        }

        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);
            if (!_submitRequested) return;
            _submitRequested = false;
            if (_stateMachine.CurrentState != State.OnlineMenu) return;
            JoinGame();
        }
    }
}
