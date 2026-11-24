using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fida.Campaign
{
    /// <summary>
    /// A condition a side chapter's requester imposes on the battle. Vows are authored as
    /// snake_case strings and are always mirrored by the battle data they describe;
    /// <see cref="CampaignDifficulty.Audit"/> rejects nodes where the two disagree.
    /// </summary>
    public enum CampaignVow
    {
        BareHands,
        Alone,
        Wounded,
        Outnumbered
    }

    /// <summary>Single source of truth for vow names, display labels and violation text.</summary>
    public static class CampaignVows
    {
        public static readonly IReadOnlyList<CampaignVow> All =
            (CampaignVow[])Enum.GetValues(typeof(CampaignVow));

        /// <summary>Parses the JSON spelling (`bare_hands`, `alone`, ...); unknown values return null.</summary>
        public static CampaignVow? Parse(string? value) => value?.Trim().ToLowerInvariant() switch
        {
            "bare_hands" or "barehands" or "bare hands" => CampaignVow.BareHands,
            "alone" => CampaignVow.Alone,
            "wounded" => CampaignVow.Wounded,
            "outnumbered" => CampaignVow.Outnumbered,
            _ => null
        };

        public static string Name(CampaignVow vow) => vow switch
        {
            CampaignVow.BareHands => "bare_hands",
            CampaignVow.Alone => "alone",
            CampaignVow.Wounded => "wounded",
            CampaignVow.Outnumbered => "outnumbered",
            _ => ""
        };

        /// <summary>Uppercase stamp used on the map card and the loadout banner.</summary>
        public static string Label(CampaignVow vow) => vow switch
        {
            CampaignVow.BareHands => "BARE HANDS",
            CampaignVow.Alone => "ALONE",
            CampaignVow.Wounded => "WOUNDED",
            CampaignVow.Outnumbered => "OUTNUMBERED",
            _ => ""
        };

        public static string Labels(IEnumerable<CampaignVow>? vows) =>
            string.Join(", ", (vows ?? Array.Empty<CampaignVow>()).Select(Label));

        /// <summary>One short English sentence per vow, used by the pre-battle briefing page.</summary>
        public static string Sentence(CampaignVow vow) => vow switch
        {
            CampaignVow.BareHands => "No skills may be carried into this battle.",
            CampaignVow.Alone => "No companion may join this battle.",
            CampaignVow.Wounded => "You begin this battle wounded.",
            CampaignVow.Outnumbered => "You begin this battle surrounded.",
            _ => ""
        };
    }

    /// <summary>Reads vows from snake_case strings and writes them back unchanged.</summary>
    public sealed class CampaignVowJsonConverter : JsonConverter<CampaignVow>
    {
        public override CampaignVow Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("A vow must be written as a string.");
            string? value = reader.GetString();
            return CampaignVows.Parse(value)
                ?? throw new JsonException($"Unknown campaign vow '{value}'.");
        }

        public override void Write(Utf8JsonWriter writer, CampaignVow value,
            JsonSerializerOptions options) => writer.WriteStringValue(CampaignVows.Name(value));
    }
}
