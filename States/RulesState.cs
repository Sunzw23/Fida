using Fida.UI;
using Fida.Network;

namespace Fida.States
{
    /// <summary>Page navigation and return behavior for the in-game field guide.</summary>
    public sealed class RulesState : StateBase
    {
        public RulesState(UIManager uiManager, NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter() => BindControls();

        private void BindControls()
        {
            BindButton("back", _stateMachine.CloseRules);
            BindButton("prevPage", () => ChangePage(-1));
            BindButton("nextPage", () => ChangePage(1));
        }

        private void ChangePage(int offset)
        {
            UnbindAll();
            _stateMachine.SetRulesPage(_stateMachine.RulesPage + offset);
            BindControls();
        }
    }
}
