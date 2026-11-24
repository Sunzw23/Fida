using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fida.Logic;

namespace Fida.Campaign
{
    public enum CampaignChapterKind
    {
        Main,
        Side
    }

    /// <summary>
    /// One entry of Content/campaign/chapter-index.json. Chapters own the presentation
    /// (kind, palette, art, summary); nodes inherit all of it from their chapter.
    /// </summary>
    public sealed class CampaignChapter
    {
        public string Id { get; init; } = "";
        public IReadOnlyList<string> Path { get; init; } = Array.Empty<string>();
        public string Title { get; init; } = "";
        public CampaignChapterKind Kind { get; init; } = CampaignChapterKind.Main;
        // Palette key; "paper" is the main-line stock. Unknown keys fail validation.
        public string Palette { get; init; } = "";
        // Sort order inside the parent level; entries without one keep file order after ordered ones.
        public int Order { get; init; }
        // One line shown by the chapter detail sheet.
        public string Summary { get; init; } = "";
        // Existing chapter plate to reuse ("bell tower", "courtyard", ...); empty falls back
        // to UIPaper's own keyword rule.
        public string Art { get; init; } = "";

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title)
            ? Path.Count > 0 ? Path[^1] : ""
            : Title;

        public bool IsSide => Kind == CampaignChapterKind.Side;
    }

    /// <summary>
    /// Chapter registry loaded from Content/campaign/chapter-index.json. Missing or malformed
    /// content degrades to "every chapter is main and paper", which is what the campaign used
    /// before the index existed; content tests call <see cref="Audit"/> instead.
    /// </summary>
    public static class CampaignChapters
    {
        public const string DefaultPalette = "paper";

        public static readonly IReadOnlyList<string> Palettes = new[] { "paper", "frost", "slate" };

        public static readonly IReadOnlyList<CampaignChapter> All = Load();

        public static CampaignChapter? Get(IReadOnlyList<string>? path)
        {
            if (path == null || path.Count == 0) return null;
            foreach (var chapter in All)
                if (SamePath(chapter.Path, path)) return chapter;
            return null;
        }

        /// <summary>Path a node belongs to: an explicit mapPath, else its chapter split on " - ".</summary>
        public static IReadOnlyList<string> PathOf(CampaignNode? node)
        {
            if (node == null) return Array.Empty<string>();
            if (node.MapPath is { Count: > 0 }) return node.MapPath;
            if (string.IsNullOrWhiteSpace(node.Chapter)) return Array.Empty<string>();
            return node.Chapter.Split(" - ", StringSplitOptions.RemoveEmptyEntries |
                                           StringSplitOptions.TrimEntries);
        }

        public static CampaignChapter? ForNode(CampaignNode? node) => Get(PathOf(node));

        public static CampaignChapterKind KindFor(CampaignNode? node) =>
            ForNode(node)?.Kind ?? CampaignChapterKind.Main;

        public static bool IsSide(CampaignNode? node) => KindFor(node) == CampaignChapterKind.Side;

        public static string PaletteFor(CampaignNode? node) => PaletteOf(ForNode(node));

        public static string PaletteOf(CampaignChapter? chapter)
        {
            string palette = chapter?.Palette?.Trim().ToLowerInvariant() ?? "";
            return Palettes.Contains(palette) ? palette : DefaultPalette;
        }

        public static string ArtFor(CampaignNode? node) => ForNode(node)?.Art ?? "";

        /// <summary>Chapter display name for the given node's path.</summary>
        public static string TitleFor(CampaignNode? node) =>
            ForNode(node)?.DisplayTitle ?? (PathOf(node) is { Count: > 0 } path ? path[^1] : "");

        public static IReadOnlyList<CampaignChapter> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<CampaignChapter>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            var file = JsonSerializer.Deserialize<ChapterFile>(json, options);
            return (file?.Chapters ?? new List<CampaignChapter>())
                .Where(chapter => chapter.Path is { Count: > 0 })
                .DistinctBy(chapter => string.Join(" / ", chapter.Path), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static IReadOnlyList<CampaignChapter> LoadFrom(string path)
        {
            try
            {
                return Parse(ContentSource.ReadText(path));
            }
            catch (Exception exception) when (exception is IOException or
                UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                return Array.Empty<CampaignChapter>();
            }
        }

        public static string DefaultPath() => System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Content", "campaign", "chapter-index.json");

        /// <summary>
        /// Content checks: paths resolve, palettes exist, side chapters never gate the main
        /// road and every main chapter actually leads somewhere (no "pretend mainline").
        /// </summary>
        public static IReadOnlyList<string> Audit(IReadOnlyList<CampaignNode>? nodes)
        {
            var problems = new List<string>();
            if (nodes == null) return problems;

            foreach (var chapter in All)
            {
                if (string.IsNullOrWhiteSpace(chapter.Id))
                    problems.Add($"chapter '{string.Join(" / ", chapter.Path)}' has no id");
                if (!string.IsNullOrWhiteSpace(chapter.Palette) &&
                    !Palettes.Contains(chapter.Palette.Trim().ToLowerInvariant()))
                    problems.Add($"chapter '{chapter.Id}' uses unknown palette '{chapter.Palette}'");
            }

            if (All.Count == 0) return problems;

            var byChapter = nodes.Where(node => PathOf(node).Count > 0)
                .GroupBy(node => string.Join(" / ", PathOf(node)), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

            foreach (var pair in byChapter)
            {
                if (Get(PathOf(pair.Value[0])) == null)
                    problems.Add($"chapter '{pair.Key}' is used by nodes but missing from the index");
            }

            // Side chapters may chain, but a main node may never sit behind a side node.
            var sideIds = new HashSet<string>(nodes.Where(IsSide).Select(node => node.Id),
                StringComparer.OrdinalIgnoreCase);
            foreach (var node in nodes)
            {
                if (KindFor(node) != CampaignChapterKind.Main) continue;
                foreach (string parent in node.Parents)
                    if (sideIds.Contains(parent))
                        problems.Add($"main node '{node.Id}' is gated by side node '{parent}'");
            }

            // Every main chapter except the deepest one must lead to another chapter.
            problems.AddRange(AuditMainline(nodes));
            return problems;
        }

        private static IEnumerable<string> AuditMainline(IReadOnlyList<CampaignNode> nodes)
        {
            var byId = nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
            var depths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int Depth(string id, HashSet<string> path)
            {
                if (depths.TryGetValue(id, out int known)) return known;
                if (!path.Add(id)) return 0;
                int depth = 0;
                if (byId.TryGetValue(id, out var node))
                    foreach (string parent in node.Parents)
                        depth = Math.Max(depth, Depth(parent, path) + 1);
                path.Remove(id);
                depths[id] = depth;
                return depth;
            }
            foreach (var node in nodes) Depth(node.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            var mainChapters = nodes.Where(node => !IsSide(node))
                .GroupBy(node => string.Join(" / ", PathOf(node)), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Any())
                .Select(group => new
                {
                    Key = group.Key,
                    Depth = group.Max(node => depths[node.Id])
                })
                .ToArray();
            if (mainChapters.Length == 0) yield break;
            int deepest = mainChapters.Max(chapter => chapter.Depth);

            var childrenByParent = nodes.Where(node => node.Parents.Count > 0)
                .SelectMany(node => node.Parents.Select(parent => (Parent: parent, Child: node)))
                .ToLookup(pair => pair.Parent, pair => pair.Child, StringComparer.OrdinalIgnoreCase);

            foreach (var chapter in mainChapters)
            {
                if (chapter.Depth == deepest) continue;
                var chapterNodes = nodes.Where(node =>
                        !IsSide(node) &&
                        string.Equals(string.Join(" / ", PathOf(node)), chapter.Key, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                bool leadsAhead = chapterNodes.Any(node => childrenByParent[node.Id]
                    .Any(child => !string.Equals(string.Join(" / ", PathOf(child)), chapter.Key,
                        StringComparison.OrdinalIgnoreCase)));
                if (!leadsAhead)
                    yield return $"main chapter '{chapter.Key}' leads nowhere (it is really a side chapter)";
            }
        }

        private sealed class ChapterFile
        {
            public List<CampaignChapter> Chapters { get; init; } = new();
        }

        private static bool SamePath(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
                if (!string.Equals(left[i], right[i], StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static IReadOnlyList<CampaignChapter> Load() => LoadFrom(DefaultPath());
    }
}
