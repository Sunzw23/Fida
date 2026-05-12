using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 暂停菜单状态 - 处理继续游戏、返回主菜单
    /// 使用事件驱动方式绑定按钮点击
    /// </summary>
    public class PausedState : MenuStateBase
    {
        public PausedState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            BindButton("resume", () =>
                _stateMachine.SwitchToState(_networkManager.IsConnected ? State.OnlinePlaying : State.Playing));

            BindButton("backToMenu", () =>
            {
                _networkManager.Cleanup();
                _stateMachine.SwitchToState(State.MainMenu);
            });
        }
    }
}