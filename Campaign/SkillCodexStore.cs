#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>On-disk shape of the skill codex. Names keep the file readable and safe against enum reordering.</summary>
    public sealed class SkillCodexData
    {
        public int Version { get; set; } = 1;
        public string[] Encountered { get; set; } = Array.Empty<string>();
        public string[] Read { get; set; } = Array.Empty<string>();
        public Dictionary<string, string> FirstSeen { get; set; } = new();
    }

    /// <summary>Persistence for the skill codex; paths are injectable for tests and future cloud saves.</summary>
    public sealed class SkillCodexStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public string Path { get; }

        public SkillCodexStore(string path) => Path = path;

        public SkillCodexData Load()
        {
            try
            {
                if (!File.Exists(Path)) return new SkillCodexData();
                return JsonSerializer.Deserialize<SkillCodexData>(File.ReadAllText(Path)) ?? new SkillCodexData();
            }
            catch
            {
                return new SkillCodexData();
            }
        }

        public bool Save(SkillCodexData data)
        {
            try
            {
                string? directory = System.IO.Path.GetDirectoryName(Path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(Path, JsonSerializer.Serialize(data, JsonOptions));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string DefaultPath() => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fida", "skill-codex.json");

        /// <summary>Names are the only stable cross-version key for skills.</summary>
        public static IReadOnlyList<SkillType> ParseNames(IEnumerable<string>? names)
        {
            var result = new List<SkillType>();
            if (names == null) return result;
            foreach (string name in names)
            {
                if (SkillCatalog.FromName(name) is { } type && SkillCatalog.CanCreate(type) && !result.Contains(type))
                    result.Add(type);
            }
            return result;
        }
    }
}
