using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>
    /// The difficulty contract. A node declares a tier and optional vows; this class checks
    /// that the battle data actually matches them, so a "hard" side quest cannot ship as
    /// three harmless enemies (or as something unwinnable).
    /// </summary>
    public static class CampaignDifficulty
    {
        public readonly record struct Band(int MinUnits, int MaxUnits, int MinHealth, int MaxHealth,
            int MinVows, int MaxVows);

        public const int DefaultPlayerHealth = 11;

        /// <summary>Unit/health/vow window for each tier; null means "unclassified".</summary>
        public static Band? BandFor(int? tier) => tier switch
        {
            1 => new Band(2, 3, 6, 12, 0, 0),
            2 => new Band(3, 3, 12, 17, 0, 1),
            3 => new Band(3, 4, 15, 21, 1, 2),
            4 => new Band(4, 4, 21, 28, 2, 3),
            _ => null
        };

        public static int Units(CampaignNode node) => node.Enemies.Count + node.Reinforcements.Count;

        public static int TotalHealth(CampaignNode node) =>
            node.Enemies.Concat(node.Reinforcements).Sum(enemy => Math.Max(0, enemy.Health));

        public static bool IsClassified(CampaignNode? node) => node?.Tier is >= 1 and <= 4;

        /// <summary>True when the node declares a tier or a vow and therefore must be audited.</summary>
        public static bool IsAudited(CampaignNode? node) => node != null && (node.Tier.HasValue || node.Vows.Count > 0);

        public static IReadOnlyList<string> Audit(CampaignNode? node)
        {
            var problems = new List<string>();
            if (node == null) return problems;

            if (node.Tier is { } tier && BandFor(tier) == null)
                problems.Add($"tier {tier} is outside 1-4");
            if (node.Vows.Count > 0 && !node.Tier.HasValue)
                problems.Add("vows require a tier");
            if (node.PlayerHealth is < 1 or > DefaultPlayerHealth)
                problems.Add($"playerHealth {node.PlayerHealth} is outside 1-{DefaultPlayerHealth}");
            if (node.SkillBudget is < 0 or > 6)
                problems.Add($"skillBudget {node.SkillBudget} is outside 0-6");

            bool bare = node.Vows.Contains(CampaignVow.BareHands);
            bool wounded = node.Vows.Contains(CampaignVow.Wounded);
            int units = Units(node);
            int health = TotalHealth(node);

            if (bare)
            {
                if (node.SkillBudget != 0) problems.Add("bare hands requires skillBudget 0");
                if (units > 3) problems.Add($"bare hands allows at most 3 units, found {units}");
                if (health > 18) problems.Add($"bare hands allows at most 18 total health, found {health}");
                if (wounded) problems.Add("bare hands cannot be combined with wounded");
            }
            if (node.Vows.Contains(CampaignVow.Alone) && node.Companions is not { Count: 0 })
                problems.Add("alone requires an explicit empty companions array");
            if (wounded && node.PlayerHealth is < 5 or > 9)
                problems.Add($"wounded requires playerHealth 5-9, found {node.PlayerHealth}");
            if (node.Vows.Contains(CampaignVow.Outnumbered) &&
                (node.Enemies.Count != 4 || node.Reinforcements.Count != 0))
                problems.Add("outnumbered requires exactly four starting enemies and no reinforcements");

            if (BandFor(node.Tier) is { } band && !bare)
            {
                if (units < band.MinUnits || units > band.MaxUnits)
                    problems.Add($"tier {node.Tier} expects {band.MinUnits}-{band.MaxUnits} units, found {units}");
                if (health < band.MinHealth || health > band.MaxHealth)
                    problems.Add($"tier {node.Tier} expects {band.MinHealth}-{band.MaxHealth} total health, found {health}");
                if (node.Vows.Count < band.MinVows || node.Vows.Count > band.MaxVows)
                    problems.Add($"tier {node.Tier} expects {band.MinVows}-{band.MaxVows} vows, found {node.Vows.Count}");
            }

            // Sustain and rewrite skills on more than two enemies turn a fight into a grind.
            foreach (var skill in new[] { SkillType.Xue, SkillType.Shi, SkillType.Shen })
            {
                int carriers = node.Enemies.Count(enemy => enemy.Skills.Contains(skill));
                if (carriers > 2)
                    problems.Add($"{skill} appears on {carriers} enemies (max 2)");
            }

            if (node.ReinforcementRound is > 0 and < 2)
                problems.Add("reinforcements cannot arrive before round 2");

            return problems;
        }
    }
}
