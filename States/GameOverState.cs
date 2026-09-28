using Fida.Logic;
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>战斗结算：可重试当前配装，或明确返回主菜单。</summary>
    public sealed class GameOverState : StateBase
    {
        public GameOverState(UI.UIManager uiManager, Network.NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

    public override void OnEnter()
        {
            bool wasOnlineDuel = _stateMachine.LastBattleOutcome?.WasOnlineDuel == true;
            BindButton("mainMenu", () =>
            {
                if (wasOnlineDuel || _networkManager.CurrentGameMode == 2)
                {
                    _networkManager.Cleanup();
                }
                _stateMachine.SwitchToState(State.MainMenu);
            });

            if (wasOnlineDuel) return;

            bool onlineCampaign = _networkManager.CurrentGameMode == 2;
            BindButton("retry", () => _stateMachine.SwitchToState(
                    onlineCampaign ? State.OnlineCampaignSkills : State.Skills));
        }

    public override void HandleInput(GameTime gameTime)
    {
        // Both peers publish their clear at battle end. Keep consuming campaign
        // presence while the result screen is open so the online save converges
        // before either player leaves the session.
        if (_networkManager.CurrentGameMode == 2 && _networkManager.IsConnected)
            _networkManager.ProcessCampaignMessages();
        base.HandleInput(gameTime);
    }
}
}
