using Fida.Logic;

namespace Fida.Campaign.Battle
{
    /// <summary>战役 Agent 类型到控制器实现的唯一映射点。</summary>
    public static class CampaignControllerFactory
    {
        // 保留 type 参数兼容既有节点 JSON；所有敌人统一由 value table 决策。
        public static IPlayerController Create(int playerId, CampaignAgentType type) =>
            new Fida.Agent.LightweightAgentController(playerId);
    }
}
