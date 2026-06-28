using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 连接等待状态 - 处理取消按钮和连接检查
    /// 使用事件驱动方式绑定按钮点击
    /// </summary>
    public class ConnectingState : StateBase
    {
        public ConnectingState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            BindButton("cancelConnect", () =>
            {
                _networkManager.Cleanup();
                _stateMachine.SwitchToState(State.OnlineMenu);
            });
        }

        /// <summary>
        /// 处理输入 + 连接检查
        /// </summary>
        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);

            if (_networkManager.IsConnected)
            {
                _uiManager.BuildConnectingScreen(_networkManager.StatusText);
                _stateMachine.SwitchToState(State.OnlineLobby);
            }
        }
    }
}