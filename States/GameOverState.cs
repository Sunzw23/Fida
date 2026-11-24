using Fida.Logic;
using Microsoft.Xna.Framework;
using Fida.Campaign;
using Fida.UI;
using System.Linq;

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
                if (!wasOnlineDuel && _stateMachine.LastBattleOutcome?.Won == true)
                {
                    _stateMachine.SwitchToState(_networkManager.CurrentGameMode == 2
                        ? State.OnlineCampaignMap
                        : State.LevelSelect);
                    return;
                }
                // A finished battle never ends the session; the connected main menu stays
                // available until the player chooses Disconnect explicitly.
                _stateMachine.SwitchToState(State.MainMenu);
            });

            if (wasOnlineDuel) return;

            bool onlineCampaign = _networkManager.CurrentGameMode == 2;
            BindButton("retry", () => _stateMachine.SwitchToState(
                    onlineCampaign ? State.OnlineCampaignSkills : State.Skills));

            var node = GameSetup.SelectedNode;
            if (_stateMachine.LastBattleOutcome?.Won != true || node.AfterStory.Count == 0) return;
            var progress = CampaignRuntime.GetProgress(onlineCampaign);
            var dialogue = _uiManager.AddTutorialDialogue(node);
            dialogue.PresentPages(node.AfterStory, progress);
            SetResultButtons(false, false);
            void AfterJourney()
            {
                dialogue.Finished -= AfterJourney;
                SetResultButtons(node.Endings.Count == 0, node.Endings.Count > 0);
            }
            dialogue.Finished += AfterJourney;
            if (!dialogue.IsVisible) AfterJourney();

            foreach (var ending in node.Endings)
                BindButton("ending_" + ending.Id, () =>
                {
                    SetResultButtons(false, false);
                    dialogue.PresentPages(ending.Pages, progress);
                    void FinishEnding()
                    {
                        dialogue.Finished -= FinishEnding;
                        progress.SeenEndings.Add(ending.Id);
                        (onlineCampaign ? CampaignRuntime.Online : CampaignRuntime.Local).Save();
                        SetResultButtons(true, false);
                    }
                    dialogue.Finished += FinishEnding;
                    if (!dialogue.IsVisible) FinishEnding();
                });
        }

        private void SetResultButtons(bool results, bool endings)
        {
            foreach (var control in _uiManager.GetControls().OfType<UIButton>())
            {
                if (control.Name is "retry" or "mainMenu") control.IsVisible = results;
                else if (control.Name.StartsWith("ending_")) control.IsVisible = endings;
            }
            _uiManager.FocusFirst();
        }

    public override void HandleInput(GameTime gameTime)
    {
        // Both peers publish their clear at battle end. Keep consuming campaign
        // presence while the result screen is open so the online save converges
        // before either player leaves the session.
        if (_networkManager.CurrentGameMode == 2 && _networkManager.IsConnected)
            _networkManager.ProcessCampaignMessages();
        if (HandleStoryInput(gameTime)) return;
        base.HandleInput(gameTime);
    }
}
}
