using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;
using Fida.Campaign.Infrastructure;

namespace Fida.Campaign
{
    /// <summary>聚合各章节节点；新增章节只需新增一个 partial 数据文件。</summary>
    public static partial class CampaignCatalog
    {
        public static readonly IReadOnlyList<CampaignNode> Nodes = BuildNodes();

        public static CampaignNode GetNode(string id) =>
            Nodes.FirstOrDefault(n => string.Equals(n.Id, id, StringComparison.OrdinalIgnoreCase));

        public static bool IsUnlocked(CampaignProgress progress, CampaignNode node)
        {
            if (node == null) return false;
            if (node.Parents.Count == 0) return true;
            int completed = node.Parents.Count(progress.IsCompleted);
            return completed >= Math.Min(node.RequiredCompletedParents, node.Parents.Count);
        }

        private static IReadOnlyList<CampaignNode> BuildNodes()
        {
            var nodes = new List<CampaignNode>();
            AddPrologue(nodes);
            AddFirstChapter(nodes);
            AddBellTowerBranch(nodes);
            AddCourtyardBranch(nodes);
            AddHighTierBranches(nodes);
            AddFinale(nodes);
            var builtIn = nodes.ToList();
            MergeExtensions(nodes, JsonCampaignNodeProvider.FromDefaultContent().Load());
            return CampaignGraphValidator.IsValid(nodes) ? nodes : builtIn;
        }

        private static void MergeExtensions(List<CampaignNode> builtIn, IReadOnlyList<CampaignNode> extensions)
        {
            foreach (var node in extensions)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id)) continue;
                int existing = builtIn.FindIndex(n => string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase));
                if (existing >= 0) builtIn[existing] = node;
                else builtIn.Add(node);
            }
        }

        private static CampaignEnemyDefinition Enemy(string name, int hp,
            CampaignAgentType agent = CampaignAgentType.Rule, int n0 = 1, int n1 = 1,
            params SkillType[] skills) => new()
        {
            Name = name,
            Health = hp,
            Number0 = n0,
            Number1 = n1,
            AgentType = agent,
            Skills = skills
        };

        private static CampaignNode Node(string id, string chapter, string name, string story,
            int budget, int x, int y, string[] parents, int required, SkillType[] rewards,
            CampaignEnemyDefinition[] enemies, CampaignEnemyDefinition[] reinforcements = null,
            int reinforcementRound = 0, int reinforcementDeaths = 0, bool boss = false,
            bool trueEndingRequired = false) => new()
        {
            Id = id,
            Chapter = chapter,
            Name = name,
            Story = story,
            SkillBudget = budget,
            MapX = x,
            MapY = y,
            Parents = parents,
            RequiredCompletedParents = required,
            Rewards = rewards,
            Enemies = enemies,
            Reinforcements = reinforcements ?? Array.Empty<CampaignEnemyDefinition>(),
            ReinforcementRound = reinforcementRound,
            ReinforcementAfterEnemyDeaths = reinforcementDeaths,
            IsBoss = boss,
            IsTrueEndingRequired = trueEndingRequired
        };

        static partial void AddPrologue(List<CampaignNode> nodes);
        static partial void AddFirstChapter(List<CampaignNode> nodes);
        static partial void AddBellTowerBranch(List<CampaignNode> nodes);
        static partial void AddCourtyardBranch(List<CampaignNode> nodes);
        static partial void AddHighTierBranches(List<CampaignNode> nodes);
        static partial void AddFinale(List<CampaignNode> nodes);
    }
}
