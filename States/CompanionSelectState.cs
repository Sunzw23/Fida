using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 同伴准备状态：在立绘名册里挑选本场同行者（可带多名，也可独自前往），
    /// 确认后进入技能配装；没有可配技能时直接开战。
    /// </summary>
    public sealed class CompanionSelectState : StateBase
    {
        public CompanionSelectState(UIManager uiManager, Network.NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            // The roster screen is a data-driven composite control; intents are consumed in
            // HandleInput, so a bigger roster needs no new button bindings.
        }

        public override void HandleInput(GameTime gameTime)
        {
            if (HandleStoryInput(gameTime)) return;
            base.HandleInput(gameTime);
            var screen = _uiManager.GetControlByName<UICampaignCompanionSelect>("companion_select");
            if (screen == null) return;
            screen.SetSelection(GameSetup.SelectedCompanionIds);
            if (screen.ConsumeBackRequest())
            {
                _stateMachine.SwitchToState(State.LevelSelect);
                return;
            }
            if (screen.TryConsumeToggle(out string companionId))
            {
                GameSetup.ToggleCampaignCompanion(companionId);
                screen.SetSelection(GameSetup.SelectedCompanionIds);
            }
            if (screen.ConsumeContinueRequest()) _stateMachine.SwitchToState(NextState());
        }

        /// <summary>Zero-SP lessons skip the loadout but keep the player's party choice.</summary>
        private State NextState() =>
            CampaignLoadout.HasSelectableSkills(CampaignRuntime.Progress, GameSetup.SelectedNode)
                ? State.Skills
                : State.Playing;
    }
}
