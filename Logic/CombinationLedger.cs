using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// Which rows of the effect table have been used at least once. In-memory only: the campaign
    /// layer owns the file. Row ids are exactly the table's own notation — <c>7</c>, <c>8</c>,
    /// <c>9</c>, <c>0</c>, <c>1&amp;6</c>, <c>5&amp;5</c>, <c>6&amp;6</c>, <c>0&amp;0</c>.
    /// </summary>
    public static class CombinationLedger
    {
        /// <summary>Every row, including the passive shield row <c>0</c>.</summary>
        public static readonly IReadOnlyList<string> Rows =
            ["7", "8", "9", "0", "1&6", "5&5", "6&6", "0&0"];

        private static readonly HashSet<string> Used = new(System.StringComparer.Ordinal);

        public static int Count => Used.Count;

        public static bool Complete => Rows.All(Used.Contains);

        public static IReadOnlyList<string> Missing => Rows.Where(row => !Used.Contains(row)).ToArray();

        public static bool Has(string rowId) => Used.Contains(rowId);

        /// <summary>Longest run of effects resolved from a single number change (the chain feat).</summary>
        public static int BestChain { get; private set; }

        /// <summary>Records a finished chain; only the longest one is kept.</summary>
        public static void RecordChain(int depth)
        {
            if (depth > BestChain) BestChain = depth;
        }

        /// <summary>Idempotent; unknown ids are ignored so callers can pass raw digits.</summary>
        public static void Record(string? rowId)
        {
            if (string.IsNullOrEmpty(rowId) || !Rows.Contains(rowId)) return;
            Used.Add(rowId);
        }

        /// <summary>Records the pair currently on a player's board, in ascending order.</summary>
        public static void RecordPair(int first, int second) =>
            Record(first <= second ? $"{first}&{second}" : $"{second}&{first}");

        public static IReadOnlyList<string> Snapshot() =>
            Used.OrderBy(row => row, System.StringComparer.Ordinal).ToArray();

        public static void Restore(IEnumerable<string>? rows)
        {
            Used.Clear();
            if (rows == null) return;
            foreach (string row in rows) Record(row);
        }

        /// <summary>Test hook: forget everything that was used this session.</summary>
        public static void Clear()
        {
            Used.Clear();
            BestChain = 0;
        }
    }
}
