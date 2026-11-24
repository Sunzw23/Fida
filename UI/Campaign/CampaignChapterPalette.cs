#nullable enable
using System;
using Fida.Campaign;
using Microsoft.Xna.Framework;

namespace Fida.UI.Campaign;

/// <summary>
/// Presentation-only chapter stock. Cool palettes by decision: the main line keeps the
/// existing cream sheet, side chapters use frost, the final approach uses slate.
/// </summary>
public readonly record struct ChapterStyle(Color Face, Color Ink, Color MutedInk, Color Accent,
    bool Side, string ArtKey)
{
    /// <summary>Current look; every draw call without an explicit style keeps this.</summary>
    public static readonly ChapterStyle Default =
        new(UIPaper.Face, UIPaper.Ink, UIPaper.MutedInk, new Color(110, 178, 190), false, "");

    public static ChapterStyle For(CampaignNode? node) => For(CampaignChapters.ForNode(node),
        CampaignChapters.ArtFor(node));

    public static ChapterStyle For(CampaignChapter? chapter, string? artKey = null)
    {
        string palette = CampaignChapters.PaletteOf(chapter);
        string art = string.IsNullOrWhiteSpace(artKey) ? chapter?.Art ?? "" : artKey;
        return palette switch
        {
            "frost" => new ChapterStyle(new Color(203, 216, 218), new Color(26, 42, 51),
                new Color(79, 102, 112), new Color(94, 110, 158), true, art),
            "slate" => new ChapterStyle(new Color(174, 186, 194), new Color(22, 35, 43),
                new Color(73, 91, 101), new Color(124, 116, 168), true, art),
            _ => Default with { ArtKey = art }
        };
    }

    /// <summary>Style for the folder at the given path (chapters are declared by path).</summary>
    public static ChapterStyle ForPath(System.Collections.Generic.IReadOnlyList<string> path) =>
        For(CampaignChapters.Get(path));
}
