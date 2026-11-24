using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>
/// Neutral lobby identities, drawn from the portrait atlas the battle screen already uses.
///
/// An earlier version hand-authored two 512x640 figures from a dozen polygons. At any size
/// they read as crude mannequins standing next to the generated character art: broad plinth
/// shoulders, a blob head and no silhouette of their own. Reusing two atlas cells keeps the
/// lobby on-model with the rest of the game and adds no asset to the content pipeline.
/// </summary>
public static class UIPaperAvatar
{
    /// <summary>Cell for the local player: Arith, the roster's first slot.</summary>
    public const int PlayerCell = 0;

    /// <summary>
    /// Cell for the opponent. Echo is the atlas's least specific identity - a hooded figure
    /// with a void face - which suits a stranger you have not met yet, and its tall narrow
    /// silhouette balances Arith. The Warden cell was tried first but its broad pauldrons and
    /// chest chain read as a much bigger character beside Arith.
    /// </summary>
    public const int OpponentCell = 5;

    /// <summary>
    /// Draws one atlas cell inside <paramref name="bounds"/>, aspect fit and bottom centred,
    /// which is how <see cref="UIPlayer"/> frames the same art on the battle cards.
    /// </summary>
    public static void Draw(SpriteBatch batch, Texture2D atlas, Rectangle bounds, int cell)
    {
        if (atlas == null || atlas.Width < 6 || atlas.Height < 4) return;

        int cellWidth = atlas.Width / 3;
        int cellHeight = atlas.Height / 2;
        var source = new Rectangle(Math.Abs(cell) % 3 * cellWidth, Math.Abs(cell) / 3 * cellHeight,
            cellWidth, cellHeight);
        float scale = Math.Min(bounds.Width / (float)source.Width, bounds.Height / (float)source.Height);

        batch.Draw(atlas, new Vector2(bounds.Center.X, bounds.Bottom), source, Color.White, 0f,
            new Vector2(source.Width / 2f, source.Height), new Vector2(scale, scale),
            SpriteEffects.None, 0f);
    }
}
