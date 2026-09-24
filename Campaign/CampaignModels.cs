using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    public enum CampaignAgentType { Rule, Scripted }

    public sealed class CampaignEnemyDefinition
    {
        public string Name { get; init; } = "Enemy";
        public string PortraitKey { get; init; } = "enemy_fallback";
        public int Health { get; init; } = 5;
        public int Number0 { get; init; } = 1;
        public int Number1 { get; init; } = 1;
        public CampaignAgentType AgentType { get; init; } = CampaignAgentType.Rule;
        public IReadOnlyList<SkillType> Skills { get; init; } = Array.Empty<SkillType>();
    }

    public sealed class CampaignNode
    {
        public string Id { get; init; } = "";
        public string Chapter { get; init; } = "";
        // Optional folder path for the campaign browser. Existing content falls back to Chapter.
        public IReadOnlyList<string> MapPath { get; init; } = Array.Empty<string>();
        public string Name { get; init; } = "";
        public string Story { get; init; } = "";
        public int SkillBudget { get; init; }
        public int MapX { get; init; }
        public int MapY { get; init; }
        public int RequiredCompletedParents { get; init; } = 1;
        public IReadOnlyList<string> Parents { get; init; } = Array.Empty<string>();
        public IReadOnlyList<SkillType> Rewards { get; init; } = Array.Empty<SkillType>();
        public IReadOnlyList<CampaignEnemyDefinition> Enemies { get; init; } = Array.Empty<CampaignEnemyDefinition>();
        public IReadOnlyList<CampaignEnemyDefinition> Reinforcements { get; init; } = Array.Empty<CampaignEnemyDefinition>();
        public int ReinforcementRound { get; init; }
        public int ReinforcementAfterEnemyDeaths { get; init; }
        public bool IsBoss { get; init; }
        public bool IsTrueEndingRequired { get; init; }

        public string RewardText => Rewards.Count == 0
            ? "Story Fragment"
            : string.Join(", ", Rewards.Select(GameText.SkillName));
    }

    public sealed class CampaignProgress
    {
        public HashSet<string> CompletedNodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<SkillType> UnlockedSkills { get; set; } = new();

        public bool IsCompleted(string nodeId) => CompletedNodes.Contains(nodeId);

        public void Complete(CampaignNode node)
        {
            if (node == null) return;
            CompletedNodes.Add(node.Id);
            foreach (var reward in node.Rewards) UnlockedSkills.Add(reward);
        }
    }

    /// <summary>纯数据传输对象，由战役模块生成，由场景模块消费。</summary>
    public sealed class CampaignBattleSetup
    {
        public CampaignNode Node { get; init; }
        public List<Player> Players { get; init; } = [];
        public List<Group> Groups { get; init; } = [];
        public List<Player> Reinforcements { get; init; } = [];
        public Dictionary<int, CampaignAgentType> AgentTypes { get; init; } = new();
    }
}
