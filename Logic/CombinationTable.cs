using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>What a number pair resolves to.</summary>
    public enum CombinationKind
    {
        None,
        Attack,
        Heal
    }

    /// <summary>
    /// One resolution of the effect table: what fires, how hard, and which digits it consumes.
    /// A single-digit effect reports only <see cref="FirstIndex"/>; a pair reports both.
    /// </summary>
    public readonly record struct CombinationOutcome(
        CombinationKind Kind, int Amount, int FirstIndex, int SecondIndex, string SourceId)
    {
        public static readonly CombinationOutcome None = new(CombinationKind.None, 0, -1, -1, "");

        public bool IsNone => Kind == CombinationKind.None;

        public bool ConsumesBoth => FirstIndex >= 0 && SecondIndex >= 0;
    }

    /// <summary>One pair rule printed on a card, e.g. <c>1&amp;7 → attack 2</c>.</summary>
    public sealed record CombinationRule(int A, int B, CombinationKind Kind, int Amount);

    /// <summary>
    /// A card is its rule. It covers the single-digit row in <see cref="ReplacesDigit"/> and installs
    /// its pair rules. Cards are deliberately unnamed: the rule is the entire content.
    /// </summary>
    public sealed record CombinationCard(string Id, int ReplacesDigit, IReadOnlyList<CombinationRule> Rules);

    /// <summary>
    /// The effect table as data. The eight baseline rows never change; a card can cover one
    /// single-digit row and add pair rules that resolve before everything else.
    /// <para>
    /// This is the parallel implementation validated against <c>ApplyingEffectState</c>; the live
    /// pipeline still uses the hard-coded table until the switch-over step.
    /// </para>
    /// </summary>
    public static class CombinationTable
    {
        public const string BaseSource = "base";

        /// <summary>
        /// Authored cards. The rule is the whole card: each one covers the single-digit row named
        /// by <see cref="CombinationCard.ReplacesDigit"/> and installs its pair rules.
        /// </summary>
        public static IReadOnlyList<CombinationCard> Cards { get; } =
        [
            new("7:1&7:-2", 7, [new CombinationRule(1, 7, CombinationKind.Attack, 2)]),
            new("7:7&7:-4", 7, [new CombinationRule(7, 7, CombinationKind.Attack, 4)]),
            new("8:5&8:-3", 8, [new CombinationRule(5, 8, CombinationKind.Attack, 3)]),
            new("9:1&9:-2", 9, [new CombinationRule(1, 9, CombinationKind.Attack, 2)]),
            new("9:9&9:-7", 9, [new CombinationRule(9, 9, CombinationKind.Attack, 7)])
        ];

        /// <summary>Resolves equipped card ids against <see cref="Cards"/>, dropping unknown ids.</summary>
        public static IReadOnlyList<CombinationCard> Resolve(IEnumerable<string>? equippedIds)
        {
            if (equippedIds == null) return [];
            var wanted = new HashSet<string>(equippedIds);
            return wanted.Count == 0 ? [] : Cards.Where(card => wanted.Contains(card.Id)).ToArray();
        }

        public static CombinationOutcome Match(IReadOnlyList<int> numbers, IEnumerable<string>? equippedIds = null) =>
            Match(numbers, Resolve(equippedIds));

        public static CombinationOutcome Match(IReadOnlyList<int> numbers, IReadOnlyList<CombinationCard> active)
        {
            if (numbers == null || numbers.Count < 2) return CombinationOutcome.None;

            // 1. Card pairs win over everything. A card exists to be harder to trigger and bigger,
            //    so it has to be read before the baseline rows it competes with.
            foreach (var card in active)
                foreach (var rule in card.Rules)
                    if (Matches(rule, numbers[0], numbers[1]))
                        return new CombinationOutcome(rule.Kind, rule.Amount, 0, 1, card.Id);

            // 2. Baseline pairs.
            if (Pair(numbers, 1, 6)) return Both(CombinationKind.Attack, 2);
            if (Pair(numbers, 5, 5)) return Both(CombinationKind.Attack, 3);
            if (Pair(numbers, 6, 6)) return Both(CombinationKind.Attack, 5);
            if (Pair(numbers, 0, 0)) return Both(CombinationKind.Heal, 10);

            // 3. Baseline singles, in the original order: 7/8 first, then 9. A covered row is inert,
            //    which is what lets a player hoard a 9 behind a "9&9" card.
            var covered = new HashSet<int>(active.Select(card => card.ReplacesDigit));
            for (int i = 0; i < 2; i++)
                if (!covered.Contains(numbers[i]) && numbers[i] is 7 or 8)
                    return new CombinationOutcome(CombinationKind.Attack, 1, i, -1, BaseSource);

            for (int i = 0; i < 2; i++)
                if (!covered.Contains(numbers[i]) && numbers[i] == 9)
                    return new CombinationOutcome(CombinationKind.Heal, 1, i, -1, BaseSource);

            return CombinationOutcome.None;
        }

        /// <summary>The digit currently acting as a shield, or -1. 0 is a resource, not a table row.</summary>
        public static int ShieldIndex(IReadOnlyList<int> numbers) =>
            numbers == null || numbers.Count < 2 ? -1
            : numbers[0] == 0 ? 0
            : numbers[1] == 0 ? 1
            : -1;

        private static CombinationOutcome Both(CombinationKind kind, int amount) =>
            new(kind, amount, 0, 1, BaseSource);

        private static bool Pair(IReadOnlyList<int> numbers, int a, int b) =>
            (numbers[0] == a && numbers[1] == b) || (numbers[0] == b && numbers[1] == a);

        private static bool Matches(CombinationRule rule, int x, int y) =>
            (x == rule.A && y == rule.B) || (x == rule.B && y == rule.A);
    }
}
