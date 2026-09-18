using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fida.Campaign.Infrastructure
{
    /// <summary>从 Content/campaign/nodes.json 加载增量节点；失败时安全回退到内置数据。</summary>
    public sealed class JsonCampaignNodeProvider : ICampaignNodeProvider
    {
        private readonly string _path;

        public JsonCampaignNodeProvider(string path) => _path = path;

        public IReadOnlyList<CampaignNode> Load()
        {
            try
            {
                if (!File.Exists(_path)) return Array.Empty<CampaignNode>();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new JsonStringEnumConverter());
                var nodes = JsonSerializer.Deserialize<List<CampaignNode>>(File.ReadAllText(_path), options);
                return nodes ?? new List<CampaignNode>();
            }
            catch
            {
                return Array.Empty<CampaignNode>();
            }
        }

        public static JsonCampaignNodeProvider FromDefaultContent() => new(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Content", "campaign", "nodes.json"));
    }
}
