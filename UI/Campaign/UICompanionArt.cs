#nullable enable
using System;
using Fida.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

/// <summary>
/// Resolves a companion's large art for the companion screen.
///
/// Companion content stays data-driven: `artKey` names a registered texture, `artPath`
/// registers one on first use, and `atlasCell` falls back to the shared 3x2 roster atlas.
/// Anything unresolved still draws a cell from that atlas, so a bad key degrades to "no
/// dedicated art" instead of an exception on the preparation screen.
/// </summary>
public static class UICompanionArt
{
    public static Texture2D? Resolve(ResourceManager resources, CampaignCompanion companion)
    {
        if (resources is null || companion is null) return null;
        string key = string.IsNullOrWhiteSpace(companion.ArtKey) ? companion.Id : companion.ArtKey.Trim();
        return resources.TryGetTexture(key, companion.ArtPath);
    }

    /// <summary>Atlas cell for a companion without dedicated art.</summary>
    public static int AtlasCell(CampaignCompanion companion)
    {
        if (companion.AtlasCell >= 0) return companion.AtlasCell;
        string key = (companion.PortraitKey ?? "").ToLowerInvariant();
        string name = (companion.Name ?? "").ToLowerInvariant();
        return key.Contains("withered") || name.Contains("king") ? 3
            : key.Contains("tutorial_ally") || key.Contains("mara") || name.Contains("mara") ? 1
            : key.Contains("sentinel") || name.Contains("sentinel") ? 2
            : key.Contains("echo") || name.Contains("echo") ? 5
            : 4;
    }

    /// <summary>Large art when the data provides it, otherwise the atlas bust.</summary>
    public static void Draw(SpriteBatch batch, ResourceManager resources, CampaignCompanion companion,
        Rectangle bounds)
    {
        if (bounds.Width < 4 || bounds.Height < 4) return;
        if (Resolve(resources, companion) is { } art)
        {
            // Bottom-centred, aspect fit: the cut-paper figures are cropped at the waist.
            float scale = Math.Min(bounds.Width / (float)art.Width, bounds.Height / (float)art.Height);
            batch.Draw(art, new Vector2(bounds.Center.X, bounds.Bottom), null, Color.White, 0f,
                new Vector2(art.Width / 2f, art.Height), new Vector2(scale, scale),
                SpriteEffects.None, 0f);
            return;
        }

        UIPaperAvatar.Draw(batch, resources.GetTexture("cold_roster"), bounds, AtlasCell(companion));
    }
}
