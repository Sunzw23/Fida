using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    public enum CampaignAgentType { Rule, Scripted }

    public sealed class CampaignStoryPage
    {
        public string Speaker { get; init; } = "The Long Road";
        public string Text { get; init; } = "";
        public string RequiredCompletedNode { get; init; } = "";
    }

    public sealed class CampaignEnding
    {
        public string Id { get; init; } = "";
        public string Label { get; init; } = "";
        public IReadOnlyList<CampaignStoryPage> Pages { get; init; } = Array.Empty<CampaignStoryPage>();
    }

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
        public IReadOnlyList<CampaignStoryPage> BeforeStory { get; init; } = Array.Empty<CampaignStoryPage>();
        public IReadOnlyList<CampaignStoryPage> AfterStory { get; init; } = Array.Empty<CampaignStoryPage>();
        public IReadOnlyList<CampaignEnding> Endings { get; init; } = Array.Empty<CampaignEnding>();
        // A narrative action constraint, independent of the model's number/skill decisions.
        public string SelfTargetingEnemy { get; init; } = "";
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
        public string TutorialBriefing { get; init; } = "";
        public string TutorialRule { get; init; } = "";
        // Optional narrowing of the companion roster for this battle. Absent means every
        // companion in Content/campaign/companions.json may join; an empty array means nobody.
        public IReadOnlyList<string>? Companions { get; init; }
        // Difficulty band. Nodes without a tier are unclassified: the difficulty contract
        // skips them, so old content keeps working until it is reviewed.
        public int? Tier { get; init; }
        // Conditions the requester imposes; each vow is mirrored by the battle data and
        // checked by CampaignDifficulty.Audit.
        public IReadOnlyList<CampaignVow> Vows { get; init; } = Array.Empty<CampaignVow>();
        // Health the local player starts with (the wounded vow lowers it).
        public int PlayerHealth { get; init; } = 11;

        public bool IsTutorial => !string.IsNullOrWhiteSpace(TutorialBriefing);

        public string VowText => CampaignVows.Labels(Vows);

        public string RewardText
        {
            get
            {
                var parts = Rewards.Select(GameText.SkillName).ToList();
                foreach (var companion in CampaignCompanions.ForNode(Id))
                    parts.Add(companion.Name + " joins");
                return parts.Count == 0 ? "Story Fragment" : string.Join(", ", parts);
            }
        }
    }

    public sealed class CampaignProgress
    {
        public HashSet<string> CompletedNodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<SkillType> UnlockedSkills { get; set; } = new();
        public HashSet<string> SeenEndings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

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
        public required CampaignNode Node { get; init; }
        public List<Player> Players { get; init; } = [];
        public List<Group> Groups { get; init; } = [];
        public List<Player> Reinforcements { get; init; } = [];
        public Dictionary<int, CampaignAgentType> AgentTypes { get; init; } = new();
    }
}
