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
    }
}
