#nullable enable
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 联机菜单状态 - 处理创建主机、连接、返回
    /// 使用事件驱动方式绑定按钮点击
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

            BindButton("hostGame", () =>
            {
                _networkManager.StartHosting(_networkManager.GetServerAddress(), _networkManager.GetServerPort(), _networkManager.GetServerIsRelay());
                _stateMachine.SwitchToState(State.Hosting);
            });

            bool isRelay = _networkManager.GetServerIsRelay();
            var input = _uiManager.GetTextBoxByName("roomCodeInput");
            if (input != null) input.IsVisible = isRelay;
            var art = _uiManager.GetControlByName<UI.UIReferenceSurface>("online_art");
            if (art != null) art.RoomCodeRequired = isRelay;
            var join = _uiManager.GetButtonByName("joinGame");
            if (join != null) join.EnabledChecker = () => !isRelay || input?.Value.Length == 4;
            BindButton("joinGame", JoinGame);
            // Defer keyboard submission until state input handling, so the UI control
            // list is not rebuilt while its Update loop is enumerating it.
            BindTextBox("roomCodeInput", _ => _submitRequested = true);

            BindButton("backToMain", () =>
                _stateMachine.SwitchToState(State.MainMenu));
        }

        private void JoinGame()
        {
            bool isRelay = _networkManager.GetServerIsRelay();
            string? roomCode = null;

            if (isRelay)
            {
                roomCode = _uiManager.GetTextBoxByName("roomCodeInput")?.Value
                    ?.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 4)
                    return; // 房间码未输入/不全，停留原地
            }

            if (roomCode == null)
                _networkManager.StartConnecting(_networkManager.GetServerAddress(), _networkManager.GetServerPort(), isRelay);
            else
                _networkManager.StartConnecting(_networkManager.GetServerAddress(), _networkManager.GetServerPort(), isRelay, roomCode);
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
