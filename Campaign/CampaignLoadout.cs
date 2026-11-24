using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    public enum CampaignSkillAvailability
    {
        Available,
        Selected,
        Locked,
        OverBudget,
        Conflicting
    }

    /// <summary>无 UI 的配装规则，菜单和未来其他界面共用。</summary>
    public static class CampaignLoadout
    {
        public static bool HasSelectableSkills(CampaignProgress progress, CampaignNode node) =>
            SkillFactory.SelectableSkills.Any(skill => CanSelect(progress, node, [], skill));

        public static bool HasCompanionChoices(CampaignNode? node, CampaignProgress? progress = null) =>
            CampaignCompanions.Available(node, progress).Count > 0;

        /// <summary>The companion id may join this node and the party still has room.</summary>
        public static bool CanSelectCompanion(CampaignNode? node, IReadOnlyCollection<string>? selected,
            string? candidate)
        {
            var current = selected ?? Array.Empty<string>();
            var match = Match(CampaignCompanions.Available(node), candidate);
            if (match == null) return false;
            if (current.Any(id => Same(id, match.Id))) return true;
            return current.Count < CampaignCompanions.PartyLimitFor(node);
        }

        /// <summary>
        /// Normalises a requested party: unknown or forbidden ids are dropped, duplicates are
        /// merged, and the result never exceeds the node's party limit. An empty list is a
        /// valid choice - the player fights alone.
        /// </summary>
        public static IReadOnlyList<string> SanitizeCompanions(CampaignNode? node,
            IEnumerable<string>? requested)
        {
            var result = new List<string>();
            if (requested == null) return result;
            var available = CampaignCompanions.Available(node);
            int limit = CampaignCompanions.PartyLimitFor(node);
            foreach (string candidate in requested)
            {
                if (result.Count >= limit) break;
                var match = Match(available, candidate);
                if (match == null || result.Any(id => Same(id, match.Id))) continue;
                result.Add(match.Id);
            }
            return result;
        }

        /// <summary>Adds or removes one party member, returning the next party.</summary>
        public static IReadOnlyList<string> ToggleCompanion(CampaignNode? node,
            IEnumerable<string>? selected, string? candidate)
        {
            var current = (selected ?? Array.Empty<string>()).ToList();
            var match = Match(CampaignCompanions.Available(node), candidate);
            if (match == null) return SanitizeCompanions(node, current);
            if (current.Any(id => Same(id, match.Id)))
                return SanitizeCompanions(node, current.Where(id => !Same(id, match.Id)));
            return SanitizeCompanions(node, current.Append(match.Id));
        }

        public static bool CanSelect(CampaignProgress progress, CampaignNode node,
            IReadOnlyCollection<SkillType> selected, SkillType candidate) =>
            Evaluate(progress, node, selected, candidate) is
                CampaignSkillAvailability.Available or CampaignSkillAvailability.Selected;

        public static CampaignSkillAvailability Evaluate(CampaignProgress progress, CampaignNode node,
            IReadOnlyCollection<SkillType> selected, SkillType candidate)
        {
            IReadOnlyCollection<SkillType> current = selected ?? [];
            if (current.Contains(candidate)) return CampaignSkillAvailability.Selected;
            if (progress == null || node == null || !progress.UnlockedSkills.Contains(candidate))
                return CampaignSkillAvailability.Locked;
            if (CampaignSkillBalance.Conflicts(current, candidate))
                return CampaignSkillAvailability.Conflicting;
            if (CampaignSkillBalance.TotalCost(current) + CampaignSkillBalance.Cost(candidate) > node.SkillBudget)
                return CampaignSkillAvailability.OverBudget;
            return CampaignSkillAvailability.Available;
        }

        public static HashSet<SkillType> Sanitize(CampaignProgress progress, CampaignNode node,
            IEnumerable<SkillType> requested)
        {
            var result = new HashSet<SkillType>();
            if (requested == null) return result;
            foreach (var skill in requested.OrderBy(CampaignSkillBalance.Cost))
                if (CanSelect(progress, node, result, skill)) result.Add(skill);
            return result;
        }

        private static CampaignCompanion? Match(IReadOnlyList<CampaignCompanion> available,
            string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return null;
            string trimmed = candidate.Trim();
            return available.FirstOrDefault(companion => Same(companion.Id, trimmed));
        }

        private static bool Same(string? left, string? right) =>
            string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
