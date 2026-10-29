using System;
using System.IO;
using System.Text.Json;

namespace Fida.Campaign
{
    /// <summary>存档基础设施。路径可注入，便于测试与未来接入云存档。</summary>
    public sealed class CampaignProgressStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        public string Path { get; }

        public CampaignProgressStore(string path) => Path = path;

        public CampaignProgress Load()
        {
            try
            {
                if (!File.Exists(Path)) return new CampaignProgress();
                return JsonSerializer.Deserialize<CampaignProgress>(File.ReadAllText(Path)) ?? new CampaignProgress();
            }
            catch { return new CampaignProgress(); }
        }

        public bool Save(CampaignProgress progress)
        {
            try
            {
                string? directory = System.IO.Path.GetDirectoryName(Path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(Path, JsonSerializer.Serialize(progress, JsonOptions));
                return true;
            }
            catch { return false; }
        }

    public static string DefaultPath() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fida", "campaign-save.json");

    public static string OnlinePath() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fida", "campaign-online-save.json");
}
}
