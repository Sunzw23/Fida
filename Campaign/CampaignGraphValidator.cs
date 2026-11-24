using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>保护数据驱动扩展：阻止重复 ID、失效父节点、环形依赖和自相矛盾的难度数据。</summary>
    public static class CampaignGraphValidator
    {
        public static bool IsValid(IReadOnlyList<CampaignNode> nodes) => Validate(nodes).Count == 0;

        /// <summary>All content problems, in the order the loader meets them.</summary>
        public static IReadOnlyList<string> Validate(IReadOnlyList<CampaignNode>? nodes)
        {
            var problems = new List<string>();
            if (nodes == null || nodes.Count == 0)
            {
                problems.Add("campaign has no nodes");
                return problems;
            }

            var byId = new Dictionary<string, CampaignNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                {
                    problems.Add("a node has no id");
                    continue;
                }
                if (!byId.TryAdd(node.Id, node))
                {
                    problems.Add($"duplicate node id '{node.Id}'");
                    continue;
                }
                if (node.RequiredCompletedParents < 0 || node.RequiredCompletedParents > node.Parents.Count)
                    problems.Add($"'{node.Id}' requires more parents than it lists");
                if (node.Enemies.Count == 0 || node.Enemies.Count + node.Reinforcements.Count > 4)
                    problems.Add($"'{node.Id}' must have 1-4 units in total");
                if (node.Rewards.Any(skill => !SkillFactory.SelectableSkills.Contains(skill)))
                    problems.Add($"'{node.Id}' rewards a skill that cannot be created");
                if (node.Companions is { } companions &&
                    companions.Any(id => CampaignCompanions.Get(id) == null))
                    problems.Add($"'{node.Id}' lists an unknown companion");
                if (node.Enemies.Concat(node.Reinforcements).SelectMany(enemy => enemy.Skills)
                    .Any(skill => !SkillFactory.SelectableSkills.Contains(skill)))
                    problems.Add($"'{node.Id}' uses a skill that cannot be created");
                foreach (string problem in CampaignDifficulty.Audit(node))
                    problems.Add($"'{node.Id}': {problem}");
            }

            foreach (var node in byId.Values)
                foreach (string parent in node.Parents)
                    if (!byId.ContainsKey(parent))
                        problems.Add($"'{node.Id}' lists missing parent '{parent}'");

            if (byId.Count > 0 && HasCycle(byId))
                problems.Add("campaign graph contains a cycle");

            foreach (var companion in CampaignCompanions.All)
            {
                if (string.IsNullOrWhiteSpace(companion.UnlockNode)) continue;
                if (!byId.ContainsKey(companion.UnlockNode))
                    problems.Add($"companion '{companion.Id}' unlocks from missing node '{companion.UnlockNode}'");
            }

            problems.AddRange(CampaignChapters.Audit(nodes));
            return problems;
        }

        private static bool HasCycle(IReadOnlyDictionary<string, CampaignNode> byId)
        {
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return byId.Values.Any(node => HasCycle(node, byId, visiting, visited));
        }

        private static bool HasCycle(CampaignNode node, IReadOnlyDictionary<string, CampaignNode> byId,
            HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(node.Id)) return false;
            if (!visiting.Add(node.Id)) return true;
            foreach (string parent in node.Parents)
                if (byId.TryGetValue(parent, out var parentNode) &&
                    HasCycle(parentNode, byId, visiting, visited)) return true;
            visiting.Remove(node.Id);
            visited.Add(node.Id);
            return false;
        }
    }
}
