using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>战役配装状态：在节点预算内选择任意数量的已解锁技能。</summary>
    public sealed class SkillSelectState : StateBase
    {
        public SkillSelectState(UIManager uiManager, Network.NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            // The wheel is a data-driven composite control. Input is consumed below so
            // adding a skill does not require adding another button or event binding.
        }

        public override void HandleInput(GameTime gameTime)
        {
            if (HandleStoryInput(gameTime)) return;
            if (!CampaignLoadout.HasSelectableSkills(CampaignRuntime.Progress, GameSetup.SelectedNode))
            {
                _stateMachine.SwitchToState(State.Playing);
                return;
            }
            base.HandleInput(gameTime);
            UpdatePartyStrip();
            var wheel = _uiManager.GetControlByName<UICampaignSkillWheel>("skill_wheel");
            if (wheel == null) return;

            if (wheel.ConsumeBackRequest())
            {
                _stateMachine.SwitchToState(PreviousState());
                return;
            }

            if (wheel.TryConsumeToggle(out SkillType skill)) GameSetup.ToggleCampaignSkill(skill);

            if (wheel.ConsumeStartRequest())
                _stateMachine.SwitchToState(State.Playing);
        }

        /// <summary>The party strip mirrors the choice made on the companion screen.</summary>
        private void UpdatePartyStrip()
        {
            var strip = _uiManager.GetControlByName<UICampaignPartyStrip>("party_strip");
            if (strip == null) return;
            strip.SetParty(GameSetup.SelectedCompanionIds);
            if (strip.TryConsumeChangeRequest()) _stateMachine.SwitchToState(PreviousState());
        }

        /// <summary>Back returns to the screen that owns the party, when this battle has one.</summary>
        private State PreviousState() => CampaignLoadout.HasCompanionChoices(GameSetup.SelectedNode,
            CampaignRuntime.Progress)
            ? State.CompanionSelect
            : State.LevelSelect;
    }
}
