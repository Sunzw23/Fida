using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 暂停菜单状态 - 处理继续游戏、返回主菜单
    /// 使用事件驱动方式绑定按钮点击
    /// </summary>
    public class PausedState : StateBase
    {
        public PausedState(UI.UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            BindButton("resume", () =>
                _stateMachine.SwitchToState(_networkManager.IsConnected ||
                    _networkManager.IsCampaignBattleContinuingLocally
                    ? State.OnlinePlaying
                    : State.Playing));

            BindButton("backToMenu", () =>
            {
                // Leaving an active Duel is a forfeit: closing the socket lets the
                // opponent's session award the win immediately.
                bool leavingSharedCampaign = _networkManager.CurrentGameMode == 2 &&
                    _networkManager.IsCampaignSharedBattle;
                if (_networkManager.IsConnected &&
                    (_networkManager.CurrentGameMode == 0 || leavingSharedCampaign))
                    _networkManager.Cleanup();
                _stateMachine.GoToMainMenu();
            });
            BindButton("settings", () => _stateMachine.SwitchToState(State.Settings));
            BindButton("rules", _stateMachine.OpenRules);
        }
    }
}
