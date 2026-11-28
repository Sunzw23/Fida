using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 主机等待状态 - 处理取消按钮和连接检查
    /// 使用事件驱动方式绑定按钮点击
    /// </summary>
    public class HostingState : StateBase
    {
        public HostingState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            void Cancel()
            {
                _networkManager.Cleanup();
                _stateMachine.SwitchToState(State.OnlineMenu);
            }
            BindButton("cancelHost", Cancel);
            BindButton("waitingBack", Cancel);
        }

        /// <summary>
        /// 处理输入 + 房间码刷新 + 连接检查
        /// </summary>
        private string _lastShownCode = "";
        private string _lastShownStatus = "";

        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);

            // Room code (relay) or own address (direct) once hosting is up. Property polling keeps
            // the label in sync without coupling the network layer to UI events.
            string code = _networkManager.HostShareValue;
            if (code != _lastShownCode)
            {
                _lastShownCode = code;
                _uiManager.GetLabelByName("roomCodeLabel")?.Update(
                    code.Length > 0 ? code : "----");
            }

            if (_networkManager.StatusText != _lastShownStatus)
            {
                _lastShownStatus = _networkManager.StatusText;
                string[] statusLines = (_lastShownStatus ?? string.Empty)
                    .Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                string status = statusLines.Length > 0 ? statusLines[0] : "WAITING FOR OPPONENT";
                string address = statusLines.Length > 1
                    ? string.Join("\n", statusLines[1..])
                    : string.Empty;
#if FIDA_ANDROID
                status = status.ToUpperInvariant();
#endif
                _uiManager.GetLabelByName("statusLabel")?.Update(status);
                _uiManager.GetLabelByName("addressLabel")?.Update(address);
            }

            if (_networkManager.CheckHostConnection())
            {
                _uiManager.BuildHostingScreen(_networkManager.StatusText, code);
                _stateMachine.SwitchToState(State.MainMenu);
            }
        }
    }
}
