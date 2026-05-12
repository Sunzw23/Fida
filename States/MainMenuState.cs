using System;
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 主菜单状态 - 处理开始游戏、联机、退出
    /// 使用事件驱动方式绑定按钮点击
    /// </summary>
    public class MainMenuState : MenuStateBase
    {
        public MainMenuState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            BindButton("startGame", () => _stateMachine.SwitchToState(State.Playing));
            BindButton("onlineMode", () => _stateMachine.SwitchToState(State.OnlineMenu));
            BindButton("exit", () => Environment.Exit(0));
        }
    }
}