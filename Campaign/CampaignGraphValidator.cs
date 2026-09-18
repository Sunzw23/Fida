using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>保护数据驱动扩展：阻止重复 ID、失效父节点和环形依赖进入地图。</summary>
    public static class CampaignGraphValidator
    {
        public static bool IsValid(IReadOnlyList<CampaignNode> nodes)
        {
            if (nodes == null || nodes.Count == 0) return false;
            var byId = new Dictionary<string, CampaignNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || !byId.TryAdd(node.Id, node)) return false;
                if (node.RequiredCompletedParents < 0 || node.RequiredCompletedParents > node.Parents.Count) return false;
                if (node.Enemies.Count == 0 || node.Enemies.Count + node.Reinforcements.Count > 4) return false;
                if (node.Rewards.Any(skill => !SkillFactory.SelectableSkills.Contains(skill))) return false;
                if (node.Enemies.Concat(node.Reinforcements).SelectMany(enemy => enemy.Skills)
                    .Any(skill => !SkillFactory.SelectableSkills.Contains(skill))) return false;
            }
            if (nodes.SelectMany(n => n.Parents).Any(parent => !byId.ContainsKey(parent))) return false;

            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return nodes.All(node => !HasCycle(node, byId, visiting, visited));
        }

        private static bool HasCycle(CampaignNode node, IReadOnlyDictionary<string, CampaignNode> byId,
            HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(node.Id)) return false;
            if (!visiting.Add(node.Id)) return true;
            foreach (string parent in node.Parents)
                if (HasCycle(byId[parent], byId, visiting, visited)) return true;
            visiting.Remove(node.Id);
            visited.Add(node.Id);
            return false;
        }
    }
}
