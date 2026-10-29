using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>Original cut-angle numerals. Cached antialiased masks, no external font dependency.</summary>
public static class UIAngularNumber
{
    private const int W = 64, H = 100, Samples = 3;
    private static readonly ConditionalWeakTable<GraphicsDevice, Texture2D> Atlases = new();
    // The first polygon is the silhouette; further polygons cut out its counters.
    private static readonly int[][][] Glyphs =
    {
        new[] { new[] { 8,14,32,0,58,14,58,86,32,100,8,86 }, new[] { 24,24,32,19,42,25,42,76,32,82,24,76 } },
        new[] { new[] { 14,17,35,0,49,0,49,100,30,100,30,25,14,34 } },
        new[] { new[] { 8,14,32,0,58,14,58,42,25,77,25,82,58,82,58,100,7,100,7,73,40,36,40,23,31,18,18,26 } },
        new[] { new[] { 8,14,32,0,58,14,58,40,44,49,58,58,58,86,32,100,8,86,8,68,26,78,33,82,40,77,40,62,24,53,24,43,40,34,40,23,32,18,8,32 } },
        new[] { new[] { 34,0,54,0,54,59,62,59,62,76,54,76,54,100,36,100,36,76,5,76,5,58 }, new[] { 22,59,36,59,36,27 } },
        new[] { new[] { 8,0,60,0,46,18,26,18,26,40,42,33,58,43,58,85,32,100,8,86,8,67,31,81,40,76,40,54,32,50,8,60 } },
        new[] { new[] { 8,14,32,0,58,14,45,26,32,18,25,24,25,41,42,33,58,44,58,86,33,100,8,85 }, new[] { 25,55,34,50,41,55,41,77,33,82,25,77 } },
        new[] { new[] { 5,0,60,0,60,18,33,100,13,100,40,18,5,18 } },
        new[] { new[] { 8,13,32,0,58,13,58,37,46,49,58,61,58,87,32,100,8,87,8,62,20,49,8,37 }, new[] { 25,23,32,18,41,23,41,35,32,41,25,35 }, new[] { 25,65,32,58,41,65,41,77,32,83,25,77 } },
        new[] { new[] { 8,14,32,0,58,15,58,86,33,100,8,86,22,74,33,82,41,76,41,58,24,66,8,55 }, new[] { 25,24,33,19,41,24,41,44,32,49,25,44 } }
    };

    public static bool Draw(SpriteBatch batch, string text, Rectangle bounds, Color color)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text) if (c < '0' || c > '9') return false;
        Texture2D atlas = Atlases.GetValue(batch.GraphicsDevice, CreateAtlas);
        float scale = Math.Min(bounds.Height / (float)H, bounds.Width / (text.Length * 72f - 8));
        float x = bounds.Center.X - (text.Length * 72 - 8) * scale / 2;
        float y = bounds.Center.Y - H * scale / 2;
        foreach (char c in text)
        {
            batch.Draw(atlas, new Vector2(x, y), new Rectangle((c - '0') * W, 0, W, H),
                color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            x += 72 * scale;
        }
        return true;
    }

    private static Texture2D CreateAtlas(GraphicsDevice device)
    {
        var pixels = new Color[W * 10 * H];
        for (int digit = 0; digit < 10; digit++)
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            int covered = 0;
            for (int sy = 0; sy < Samples; sy++)
            for (int sx = 0; sx < Samples; sx++)
            {
                bool inside = false;
                foreach (int[] polygon in Glyphs[digit])
                    if (Contains(polygon, x + (sx + 0.5f) / Samples, y + (sy + 0.5f) / Samples)) inside = !inside;
                if (inside) covered++;
            }
            pixels[y * W * 10 + digit * W + x] = Color.White * (covered / (float)(Samples * Samples));
        }
        var atlas = new Texture2D(device, W * 10, H);
        atlas.SetData(pixels);
        device.Disposing += (_, _) => atlas.Dispose();
        return atlas;
    }

    private static bool Contains(int[] polygon, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 2; i < polygon.Length; j = i, i += 2)
            if ((polygon[i + 1] > y) != (polygon[j + 1] > y) &&
                x < (polygon[j] - polygon[i]) * (y - polygon[i + 1]) / (polygon[j + 1] - polygon[i + 1]) + polygon[i]) inside = !inside;
        return inside;
    }
}
