using Fida.Logic;

namespace Fida.Campaign.Battle
{
    /// <summary>战役 Agent 类型到控制器实现的唯一映射点。</summary>
    public static class CampaignControllerFactory
    {
        public static IPlayerController Create(int playerId, CampaignAgentType type) => type switch
        {
            CampaignAgentType.Scripted => new ScriptedCampaignController(playerId),
            _ => new RuleBasedCampaignController(playerId)
        };
    }
}
