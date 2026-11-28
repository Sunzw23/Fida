using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// Which combination cards are currently installed. In-memory here; the campaign layer owns
    /// the file (see <c>Campaign/CombinationStore</c>). The effect table reads this every check.
    /// </summary>
    public static class CombinationLoadout
    {
        private static readonly HashSet<string> Installed = new(StringComparer.Ordinal);

        /// <summary>Ids of the installed cards, in catalogue order.</summary>
        public static IReadOnlyList<string> Equipped =>
            CombinationTable.Cards.Where(card => Installed.Contains(card.Id))
                .Select(card => card.Id).ToArray();

        /// <summary>Installed cards resolved to their definitions.</summary>
        public static IReadOnlyList<CombinationCard> Active =>
            CombinationTable.Cards.Where(card => Installed.Contains(card.Id)).ToArray();

        public static bool IsEquipped(string id) => Installed.Contains(id);

        /// <summary>
        /// One card per covered row: installing a card evicts whichever card already covers it.
        /// Returns the id that was evicted, or null.
        /// </summary>
        public static string? Toggle(string id)
        {
            var card = CombinationTable.Cards.FirstOrDefault(entry => entry.Id == id);
            if (card == null) return null;
            if (Installed.Remove(id)) return null;

            string? evicted = CombinationTable.Cards
                .FirstOrDefault(entry => entry.ReplacesDigit == card.ReplacesDigit
                                         && Installed.Contains(entry.Id))?.Id;
            if (evicted != null) Installed.Remove(evicted);
            Installed.Add(id);
            return evicted;
        }

        public static void SetEquipped(IEnumerable<string>? ids)
        {
            Installed.Clear();
            if (ids == null) return;
            foreach (string id in ids)
                if (CombinationTable.Cards.Any(card => card.Id == id)) Installed.Add(id);
        }

        /// <summary>
        /// Cycles the variants authored for one digit: default, then each card, then back to
        /// default. Returns the id now installed on that row, or null for the printed rule.
        /// Rows with no authored card (the pair rows) return null and change nothing.
        /// </summary>
        public static string? CycleRow(int digit)
        {
            var variants = CombinationTable.Cards.Where(card => card.ReplacesDigit == digit).ToList();
            if (variants.Count == 0) return null;

            int current = variants.FindIndex(card => Installed.Contains(card.Id));
            foreach (var variant in variants) Installed.Remove(variant.Id);
            if (current + 1 >= variants.Count) return null;

            Installed.Add(variants[current + 1].Id);
            return variants[current + 1].Id;
        }

        /// <summary>The variants authored for a digit, in catalogue order.</summary>
        public static IReadOnlyList<CombinationCard> Variants(int digit) =>
            CombinationTable.Cards.Where(card => card.ReplacesDigit == digit).ToArray();
    }
}
