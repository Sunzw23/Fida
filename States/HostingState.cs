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
            BindButton("cancelHost", () =>
            {
                _networkManager.Cleanup();
                _stateMachine.SwitchToState(State.OnlineMenu);
            });
        }

        /// <summary>
        /// 处理输入 + 房间码刷新 + 连接检查
        /// </summary>
        private string _lastShownCode = "";

        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);

            // 房间码一旦分配即刷新到 label（属性轮询，避免事件耦合）
            string code = _networkManager.HostRoomCode;
            if (code != _lastShownCode)
            {
                _lastShownCode = code;
                _uiManager.GetLabelByName("roomCodeLabel")?.Update(
                    code.Length == 4 ? $"Room: {code}" : "Room: generating...");
            }

            if (_networkManager.CheckHostConnection())
            {
                _uiManager.BuildHostingScreen(_networkManager.StatusText, code);
                _stateMachine.SwitchToState(State.OnlineLobby);
            }
        }
    }
}