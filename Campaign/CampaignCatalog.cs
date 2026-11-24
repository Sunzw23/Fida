using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fida.Campaign.Infrastructure;

namespace Fida.Campaign
{
    /// <summary>Validated, data-driven campaign catalog loaded from Content/campaign/chapters.</summary>
    public static class CampaignCatalog
    {
        public static readonly IReadOnlyList<CampaignNode> Nodes = BuildNodes();

        public static CampaignNode? GetNode(string id) =>
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
            var nodes = JsonCampaignNodeProvider.FromDefaultContent().Load();
            var problems = CampaignGraphValidator.Validate(nodes);
            if (problems.Count > 0)
                throw new InvalidDataException(
                    "Campaign data is missing or invalid: " + string.Join(" | ", problems.Take(4)));
            return nodes;
        }
    }
}
