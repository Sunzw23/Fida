using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 联机菜单状态 - 处理创建主机、连接、返回
    /// 使用事件驱动方式绑定按钮点击
    /// </summary>
    public class OnlineMenuState : StateBase
    {
        public OnlineMenuState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            BindButton("hostGame", () =>
            {
                _networkManager.StartHosting(_networkManager.GetServerAddress(), _networkManager.GetServerPort());
                _stateMachine.SwitchToState(State.Hosting);
            });

            BindButton("joinGame", () =>
            {
                _networkManager.StartConnecting(_networkManager.GetServerAddress(), _networkManager.GetServerPort());
                _stateMachine.SwitchToState(State.Connecting);
            });

            BindButton("backToMain", () =>
                _stateMachine.SwitchToState(State.MainMenu));
        }
    }
}