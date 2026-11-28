using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>The installed combination cards, stored beside the achievement record.</summary>
    public static class CombinationStore
    {
        private sealed record FileModel(int Version, List<string>? Equipped);

        private static bool _loaded;

        public static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Fida", "combinations.json");

        /// <summary>Merges the saved loadout in once; a card equipped this session wins.</summary>
        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            var saved = Load();
            if (saved.Count == 0) return;
            CombinationLoadout.SetEquipped(saved.Concat(CombinationLoadout.Equipped).Distinct());
        }

        public static IReadOnlyList<string> Load(string? path = null)
        {
            try
            {
                string file = path ?? FilePath;
                if (!File.Exists(file)) return [];
                var model = JsonSerializer.Deserialize<FileModel>(File.ReadAllText(file));
                return model?.Equipped ?? [];
            }
            catch
            {
                return [];
            }
        }

        public static void Save(string? path = null)
        {
            try
            {
                string file = path ?? FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, JsonSerializer.Serialize(
                    new FileModel(1, CombinationLoadout.Equipped.ToList()),
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // A lost loadout is not worth crashing a session over.
            }
        }
    }
}
