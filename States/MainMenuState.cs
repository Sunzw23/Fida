using System;
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 主菜单状态 - 处理开始游戏、联机、退出
    /// 使用事件驱动方式绑定按钮点击
    /// </summary>
    public class MainMenuState : StateBase
    {
        public MainMenuState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            BindButton("startGame", () => _stateMachine.SwitchToState(State.LevelSelect));
            BindButton("onlineMode", () =>
            {
                // 如果已连接则直接进入大厅，否则进入联机菜单
                if (_networkManager.IsConnected)
                    _stateMachine.SwitchToState(State.OnlineLobby);
                else
                    _stateMachine.SwitchToState(State.OnlineMenu);
            });
            BindButton("exit", () => Environment.Exit(0));
        }
    }
}