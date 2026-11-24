using Fida.Logic;

namespace Fida.Campaign.Battle
{
    /// <summary>战役 Agent 类型到控制器实现的唯一映射点。</summary>
    public static class CampaignControllerFactory
    {
        // 保留 type 参数兼容既有节点 JSON；敌人统一由 value table 决策。
        // Adaptive（Mara）与召唤师（Sable）是同伴专用控制器。
        public static IPlayerController Create(int playerId, CampaignAgentType type,
            CampaignBattleRules? rules = null)
        {
            if (type == CampaignAgentType.Adaptive) return new AdaptiveCompanionController(playerId);
            if (rules != null && rules.IsSummoner(playerId)) return new SummonerCompanionController(playerId, rules);
            return new Fida.Agent.LightweightAgentController(playerId);
        }
    }
}
