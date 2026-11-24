using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;

namespace Fida.UI.Campaign
{
    /// <summary>Groups missions into browsable folders without changing unlock rules.</summary>
    internal sealed class CampaignMapHierarchy
    {
        private readonly IReadOnlyList<CampaignNode> _nodes;
        private readonly string[][] _paths;

        public CampaignMapHierarchy(IReadOnlyList<CampaignNode> nodes)
        {
            _nodes = nodes;
            _paths = nodes.Select(node => node.MapPath != null && node.MapPath.Count > 0
                ? node.MapPath.Where(part => !string.IsNullOrWhiteSpace(part)).ToArray()
                : node.Chapter.Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToArray();
        }

        public IReadOnlyList<string> Children(IReadOnlyList<string> scope) =>
            _paths.Where(path => HasPrefix(path, scope) && path.Length > scope.Count)
                .Select(path => path[scope.Count]).Distinct(StringComparer.Ordinal)
                // The chapter index owns the reading order; undeclared folders follow by name.
                .OrderBy(name => CampaignChapters.Get(scope.Concat(new[] { name }).ToArray())?.Order
                                 ?? int.MaxValue)
                .ThenBy(name => name, StringComparer.Ordinal)
                .ToArray();

        public IReadOnlyList<int> Missions(IReadOnlyList<string> scope) =>
            Enumerable.Range(0, _nodes.Count)
                .Where(index => _paths[index].Length == scope.Count && HasPrefix(_paths[index], scope))
                .ToArray();

        public IReadOnlyList<int> Descendants(IReadOnlyList<string> scope) =>
            Enumerable.Range(0, _nodes.Count).Where(index => HasPrefix(_paths[index], scope)).ToArray();

        private static bool HasPrefix(IReadOnlyList<string> path, IReadOnlyList<string> scope)
        {
            if (path.Count < scope.Count) return false;
            for (int i = 0; i < scope.Count; i++)
                if (!string.Equals(path[i], scope[i], StringComparison.Ordinal)) return false;
            return true;
        }
    }
}
