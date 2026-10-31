using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 连接等待状态 - 处理取消按钮和连接检查
    /// 使用事件驱动方式绑定按钮点击
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

            if (_networkManager.StatusText != _lastShownStatus)
            {
                _lastShownStatus = _networkManager.StatusText;
                string status = string.IsNullOrWhiteSpace(_lastShownStatus)
                    ? "Connecting..."
                    : _lastShownStatus;
#if FIDA_ANDROID
                status = status.ToUpperInvariant();
#endif
                _uiManager.GetLabelByName("statusLabel")?.Update(status);
            }

            if (_networkManager.IsConnected)
            {
                _uiManager.BuildConnectingScreen(_networkManager.StatusText);
                _stateMachine.SwitchToState(State.MainMenu);
            }
        }
    }
}
