namespace Fida.States
{
    /// <summary>单机节点结算：保留当前配装重试，或返回树状地图。</summary>
    public sealed class GameOverState : StateBase
    {
        public GameOverState(UI.UIManager uiManager, Network.NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            BindButton("retry", () => _stateMachine.SwitchToState(State.Skills));
            BindButton("map", () => _stateMachine.SwitchToState(State.LevelSelect));
        }
    }
}
