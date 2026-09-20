using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fida.Logic;

namespace Fida.Campaign.Infrastructure
{
    /// <summary>Loads either one campaign JSON file or every JSON file in a directory.</summary>
    public sealed class JsonCampaignNodeProvider : ICampaignNodeProvider
    {
        private readonly string _path;

        public JsonCampaignNodeProvider(string path) => _path = path;

        public IReadOnlyList<CampaignNode> Load()
        {
            try
            {
                var files = ContentSource.List(_path, ".json");
                if (files.Count > 0)
                {
                    var nodes = new List<CampaignNode>();
                    foreach (string file in files)
                        nodes.AddRange(LoadFile(file));
                    return nodes;
                }
                if (ContentSource.ReadText(_path) == null) return Array.Empty<CampaignNode>();
                return LoadFile(_path);
            }
            catch (Exception exception) when (exception is IOException or JsonException or NotSupportedException)
            {
                throw new InvalidDataException($"Could not load campaign data from '{_path}'.", exception);
            }
        }

        private static IReadOnlyList<CampaignNode> LoadFile(string path)
        {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new JsonStringEnumConverter());
                string json = ContentSource.ReadText(path) ?? "[]";
                var nodes = JsonSerializer.Deserialize<List<CampaignNode>>(json, options);
                return nodes ?? new List<CampaignNode>();
        }

        public static JsonCampaignNodeProvider FromDefaultContent() => new(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Content", "campaign", "chapters"));
    }
}
