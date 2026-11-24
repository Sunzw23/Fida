using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>An ally the player may invite for a single campaign battle.</summary>
    public sealed class CampaignCompanion
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        // Battle-card portrait key, resolved like the local player's key.
        public string PortraitKey { get; init; } = "tutorial_ally";
        // Large character art for the companion screen. A registered texture name wins;
        // otherwise ArtPath registers it on first use. Without either, the roster atlas
        // cell is drawn instead.
        public string ArtKey { get; init; } = "";
        public string ArtPath { get; init; } = "";
        public int AtlasCell { get; init; } = -1;
        // One or two lines shown beside the art on the companion screen.
        public string Description { get; init; } = "";
        public int Health { get; init; } = 8;
        public CampaignAgentType AgentType { get; init; } = CampaignAgentType.Rule;
        public IReadOnlyList<SkillType> Skills { get; init; } = Array.Empty<SkillType>();
        // Node that must be cleared before this companion appears in the roster. Empty means
        // the companion is available from the start.
        public string UnlockNode { get; init; } = "";

        public bool IsPlayable => !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Name) &&
            Health > 0;
    }

    /// <summary>
    /// Companion roster loaded from Content/campaign/companions.json. The roster is global:
    /// every campaign battle offers it unless the node narrows it with `companions`.
    /// Adding a companion is a data edit; no code or node changes are required.
    /// </summary>
    public static class CampaignCompanions
    {
        /// <summary>
        /// How many companions may travel at once. Three allies still fit the battle card
        /// layout, which draws at most four units per side; raise this together with balance.
        /// </summary>
        public const int PartyLimit = 2;

        public static readonly IReadOnlyList<CampaignCompanion> All = Load();

        public static CampaignCompanion? Get(string? id) => string.IsNullOrWhiteSpace(id)
            ? null
            : All.FirstOrDefault(companion =>
                string.Equals(companion.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// `companions` is absent on most nodes, which means the whole roster may join.
        /// An explicit empty array means nobody may join this battle (used by the prologue,
        /// where Arith has not met Mara yet). A non-null progress also hides companions the
        /// player has not met yet; null progress skips that filter (tests, previews).
        /// </summary>
        public static IReadOnlyList<CampaignCompanion> Available(CampaignNode? node,
            CampaignProgress? progress = null)
        {
            var allowed = Allowed(node);
            if (progress == null) return allowed;
            return allowed.Where(companion => IsUnlocked(progress, companion)).ToArray();
        }

        private static IReadOnlyList<CampaignCompanion> Allowed(CampaignNode? node)
        {
            if (node?.Companions is not { } ids) return All;
            if (ids.Count == 0) return Array.Empty<CampaignCompanion>();
            return ids.Select(Get).OfType<CampaignCompanion>().ToArray();
        }

        /// <summary>A companion joins once its unlock node is cleared (or immediately).</summary>
        public static bool IsUnlocked(CampaignProgress? progress, CampaignCompanion companion) =>
            progress == null || string.IsNullOrWhiteSpace(companion.UnlockNode) ||
            progress.IsCompleted(companion.UnlockNode);

        /// <summary>Companions recruited by clearing the given node.</summary>
        public static IReadOnlyList<CampaignCompanion> ForNode(string? nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return Array.Empty<CampaignCompanion>();
            string id = nodeId.Trim();
            return All.Where(companion =>
                string.Equals(companion.UnlockNode?.Trim(), id, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        /// <summary>Party size allowed for this battle: never more than the roster offers.</summary>
        public static int PartyLimitFor(CampaignNode? node, CampaignProgress? progress = null) =>
            Math.Min(PartyLimit, Available(node, progress).Count);

        /// <summary>Pure JSON parse used by the loader and by content tests.</summary>
        public static IReadOnlyList<CampaignCompanion> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<CampaignCompanion>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            var companions = JsonSerializer.Deserialize<List<CampaignCompanion>>(json, options)
                ?? new List<CampaignCompanion>();
            return companions.OfType<CampaignCompanion>()
                .Where(companion => companion.IsPlayable)
                .DistinctBy(companion => companion.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static IReadOnlyList<CampaignCompanion> LoadFrom(string path)
        {
            // The roster is optional content: a missing or malformed file means "no allies
            // may be invited", never a broken campaign.
            try
            {
                return Parse(ContentSource.ReadText(path));
            }
            catch (Exception exception) when (exception is IOException or
                UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                return Array.Empty<CampaignCompanion>();
            }
        }

        public static string DefaultPath() => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Content", "campaign", "companions.json");

        private static IReadOnlyList<CampaignCompanion> Load() => LoadFrom(DefaultPath());
    }
}
