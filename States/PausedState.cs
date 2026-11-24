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
                // Walking out of a live online battle concedes it for the opponent. The
                // session itself survives: only an explicit Disconnect closes the socket.
                bool sharedBattle = _networkManager.IsConnected &&
                    (_networkManager.CurrentGameMode == 0 ||
                     (_networkManager.CurrentGameMode == 2 && _networkManager.IsCampaignSharedBattle));
                if (sharedBattle)
                {
                    _networkManager.SendBattleForfeit(_networkManager.GetLocalPlayerId());
                    if (_networkManager.CurrentGameMode == 2)
                    {
                        _networkManager.IsCampaignSharedBattle = false;
                        _networkManager.SendCampaignPresence(new Network.CampaignPresence(
                            _networkManager.GetLocalPlayerId(), Network.CampaignOnlinePhase.Map,
                            string.Empty, false));
                    }
                }
                _stateMachine.GoToMainMenu();
            });
            BindButton("settings", () => _stateMachine.SwitchToState(State.Settings));
            BindButton("rules", _stateMachine.OpenRules);
        }
    }
}
