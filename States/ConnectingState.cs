using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 连接等待状态 - 处理取消按钮和连接检查
    /// 使用事件驱动方式绑定按钮点击
    ///
    /// Also owns the LAN search: a direct guest with no typed address listens for a beacon
    /// here, and the first host it hears switches this state into a normal connection.
    /// </summary>
    public class ConnectingState : StateBase
    {
        private string _lastShownStatus = "";

        public ConnectingState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            void Cancel()
            {
                _networkManager.Cleanup();
                _stateMachine.SwitchToState(State.OnlineMenu);
            }
            BindButton("cancelConnect", Cancel);
            BindButton("waitingBack", Cancel);
        }

        /// <summary>
        /// 处理输入 + 连接检查
        /// </summary>
        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);

            // Dials the first LAN host it hears; a no-op unless a search is running.
            _networkManager.PollLanSearch();

            if (_networkManager.StatusText != _lastShownStatus)
            {
                _lastShownStatus = _networkManager.StatusText;
                string[] lines = (_lastShownStatus ?? string.Empty)
                    .Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                string status = lines.Length > 0 ? lines[0] : "Connecting...";
                string address = lines.Length > 1 ? string.Join("\n", lines[1..]) : string.Empty;
#if FIDA_ANDROID
                status = status.ToUpperInvariant();
#endif
                _uiManager.GetLabelByName("statusLabel")?.Update(status);
                _uiManager.GetLabelByName("addressLabel")?.Update(address);
            }

            if (_networkManager.IsConnected)
            {
                _uiManager.BuildConnectingScreen(_networkManager.StatusText);
                _stateMachine.SwitchToState(State.MainMenu);
            }
        }
    }
}
