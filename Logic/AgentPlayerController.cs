using Fida.Agent;

namespace Fida.Logic
{
    /// <summary>
    /// AI 代理控制器。
    /// RequestAction 调用 AgentManager.GetAgentAction() 并执行 game.UpdateGame(action)。
    /// </summary>
    public class AgentPlayerController : IPlayerController
    {
        public int PlayerId { get; }
        private readonly AgentManager _agentManager;

        public AgentPlayerController(int playerId, AgentManager agentManager)
        {
            PlayerId = playerId;
            _agentManager = agentManager;
        }

        public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args)
        {
            var agentAction = _agentManager.GetAgentAction(game, PlayerId);
            game.UpdateGame(agentAction);
        }
    }
}