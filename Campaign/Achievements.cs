using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>One row of the achievements screen.</summary>
    public sealed record Achievement(string Id, string Name, string Description);

    /// <summary>
    /// Achievements are derived, not stored: everything except the effect-table one reads existing
    /// progress, so old saves need no migration. The easter egg is the effect-table row.
    /// </summary>
    public static class Achievements
    {
        public const string AllCombinationsId = "all_combinations";

        private static bool _loaded;

        public static IReadOnlyList<Achievement> All { get; } =
        [
            new("first_battle", "First Blood", "Clear any mission once."),
            new("bare_hands", "Empty-Handed", "Clear a battle that carries no skills."),
            new("alone", "No One With You", "Clear a battle that forbids companions."),
            new("wounded", "Open Wound", "Clear a battle that starts you wounded."),
            new("codex_ten", "Field Reader", "See ten skills in the field guide."),
            new("chain", "Chain", "Resolve two effects from a single number change."),
            new(AllCombinationsId, "Every Combination", "Use every row of the effect table at least once.")
        ];

        /// <summary>
        /// Merges the saved effect-table rows into the in-memory ledger. Additive on purpose: a
        /// battle can record rows before this ever runs, and loading must not erase them.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            var saved = AchievementStore.Load();
            foreach (string row in saved.Rows) CombinationLedger.Record(row);
            CombinationLedger.RecordChain(saved.Chain);
        }

        public static bool Earned(Achievement achievement, CampaignProgress progress)
        {
            EnsureLoaded();
            return achievement.Id switch
            {
                "first_battle" => progress.CompletedNodes.Count > 0,
                "bare_hands" => ClearedWithVow(progress, CampaignVow.BareHands),
                "alone" => ClearedWithVow(progress, CampaignVow.Alone),
                "wounded" => ClearedWithVow(progress, CampaignVow.Wounded),
                "codex_ten" => SkillCodex.Encountered.Count >= 10,
                "chain" => CombinationLedger.BestChain >= 2,
                AllCombinationsId => CombinationLedger.Complete,
                _ => false
            };
        }

        /// <summary>Right-hand detail for a row: a counter where one exists, otherwise the state.</summary>
        public static string Detail(Achievement achievement, CampaignProgress progress)
        {
            if (achievement.Id == AllCombinationsId)
                return $"{CombinationLedger.Count} / {CombinationLedger.Rows.Count} rows";
            if (achievement.Id == "chain")
                return CombinationLedger.BestChain > 0 ? $"best {CombinationLedger.BestChain}" : "—";
            return Earned(achievement, progress) ? "earned" : "not yet";
        }

        public static int EarnedCount(CampaignProgress progress) =>
            All.Count(achievement => Earned(achievement, progress));

        /// <summary>True once every effect-table row has been used at least once.</summary>
        public static bool EasterEggUnlocked(CampaignProgress progress) =>
            Earned(All.First(achievement => achievement.Id == AllCombinationsId), progress);

        /// <summary>Flushes the effect-table record. Cheap; call when leaving the screen or the app.</summary>
        public static void Save()
        {
            EnsureLoaded();
            AchievementStore.Save(CombinationLedger.Snapshot(), CombinationLedger.BestChain);
        }

        private static bool ClearedWithVow(CampaignProgress progress, CampaignVow vow) =>
            CampaignCatalog.Nodes.Any(node => node.Vows.Contains(vow) && progress.IsCompleted(node.Id));
    }

    /// <summary>What the achievement file holds: the used rows and the best chain.</summary>
    public readonly record struct AchievementRecord(IReadOnlyList<string> Rows, int Chain);

    /// <summary>JSON file for the effect-table rows. Failures are silent: a missing file is empty progress.</summary>
    public static class AchievementStore
    {
        private sealed record FileModel(int Version, List<string>? Rows, int Chain);

        public static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Fida", "achievements.json");

        /// <summary><paramref name="path"/> lets tests and previews use a scratch file.</summary>
        public static AchievementRecord Load(string? path = null)
        {
            try
            {
                string file = path ?? FilePath;
                if (!File.Exists(file)) return new AchievementRecord([], 0);
                var model = JsonSerializer.Deserialize<FileModel>(File.ReadAllText(file));
                return new AchievementRecord(model?.Rows ?? [], model?.Chain ?? 0);
            }
            catch
            {
                return new AchievementRecord([], 0);
            }
        }

        public static void Save(IEnumerable<string> rows, int chain, string? path = null)
        {
            try
            {
                string file = path ?? FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, JsonSerializer.Serialize(
                    new FileModel(1, rows.ToList(), chain),
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // Losing the record is not worth crashing a session over.
            }
        }
    }
}
