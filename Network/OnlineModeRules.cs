using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Network
{
    /// <summary>
    /// Central balance contract for online modes. Draft and co-op map variants can
    /// evolve without leaking tuning constants into session/network code.
    /// </summary>
    public static class OnlineModeRules
    {
        public const int DuelSkillBudget = 8;
        public const int DuelBanCountPerPlayer = 1;

        /// <summary>
        /// Symmetrical launch preset used until the interactive draft protocol locks
        /// both players' selections. Cost: Attack Up (2) + Double Attack (3) + God (3) = 8 SP.
        /// </summary>
        public static IReadOnlyList<SkillType> DuelDefaultLoadout { get; } =
            [SkillType.PlusAttack, SkillType.DoubleAttack, SkillType.Shen];

        private static readonly Dictionary<int, IReadOnlyList<SkillType>> DuelLoadouts = new();

        public static bool IsLegalDuelLoadout(IEnumerable<SkillType> skills,
            IReadOnlyCollection<SkillType> banned)
        {
            var list = (skills ?? []).Distinct().ToList();
            if (list.Any(s => !SkillFactory.SelectableSkills.Contains(s))) return false;
            if (list.Any(s => banned?.Contains(s) == true)) return false;
            if (global::Fida.Campaign.CampaignSkillBalance.TotalCost(list) != DuelSkillBudget) return false;
            return !list.Any(candidate => global::Fida.Campaign.CampaignSkillBalance.Conflicts(
                list.Where(s => s != candidate), candidate));
        }

        public static void ConfigureDuelLoadouts(IEnumerable<SkillType> player0,
            IEnumerable<SkillType> player1)
        {
            DuelLoadouts[0] = (player0 ?? DuelDefaultLoadout).Distinct().ToArray();
            DuelLoadouts[1] = (player1 ?? DuelDefaultLoadout).Distinct().ToArray();
        }

        public static IReadOnlyList<SkillType> GetDuelLoadout(int playerId) =>
            DuelLoadouts.TryGetValue(playerId, out var result) ? result : DuelDefaultLoadout;
    }
}
