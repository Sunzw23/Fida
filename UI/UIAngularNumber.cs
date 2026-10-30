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
    private static readonly ConditionalWeakTable<GraphicsDevice, Texture2D> DisplayAtlases = new();
    // The first polygon is the silhouette; further polygons cut out its counters.
    private static readonly int[][][] Glyphs =
    {
        new[] { new[] { 8,0,56,0,64,8,64,92,56,100,8,100,0,92,0,8 }, new[] { 20,20,44,20,44,80,20,80 } },
        new[] { new[] { 12,14,32,0,48,0,48,82,62,82,62,100,6,100,6,82,28,82,28,26,12,36 } },
        new[] { new[] { 8,0,56,0,64,8,64,46,22,82,64,82,64,100,0,100,0,72,44,36,44,20,20,20,20,34,0,34,0,8 } },
        new[] { new[] { 8,0,56,0,64,8,64,42,52,50,64,58,64,92,56,100,8,100,0,92,0,70,20,70,20,82,44,82,44,60,24,60,24,42,44,42,44,20,20,20,20,32,0,32,0,8 } },
        new[] { new[] { 34,0,54,0,54,59,64,59,64,78,54,78,54,100,34,100,34,78,0,78,0,58 }, new[] { 20,59,34,59,34,28 } },
        new[] { new[] { 0,0,64,0,64,20,20,20,20,40,56,40,64,48,64,92,56,100,8,100,0,92,0,70,20,70,20,82,44,82,44,60,0,60 } },
        new[] { new[] { 8,0,56,0,64,8,64,28,44,28,44,20,20,20,20,40,56,40,64,48,64,92,56,100,8,100,0,92,0,8 }, new[] { 20,60,44,60,44,82,20,82 } },
        new[] { new[] { 0,0,64,0,64,20,32,100,10,100,42,20,0,20 } },
        new[] { new[] { 8,0,56,0,64,8,64,40,54,50,64,60,64,92,56,100,8,100,0,92,0,60,10,50,0,40,0,8 }, new[] { 20,20,44,20,44,40,20,40 }, new[] { 20,60,44,60,44,80,20,80 } },
        new[] { new[] { 8,0,56,0,64,8,64,92,56,100,8,100,0,92,0,72,20,72,20,80,44,80,44,60,8,60,0,52,0,8 }, new[] { 20,20,44,20,44,40,20,40 } }
    };

    public static bool Draw(SpriteBatch batch, string text, Rectangle bounds, Color color, bool displaySize = false)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text) if (c < '0' || c > '9') return false;
        int resolution = displaySize ? 6 : 1;
        Texture2D atlas = displaySize
            ? DisplayAtlases.GetValue(batch.GraphicsDevice, device => CreateAtlas(device, 6))
            : Atlases.GetValue(batch.GraphicsDevice, device => CreateAtlas(device, 1));
        float scale = Math.Min(bounds.Height / (float)H, bounds.Width / (text.Length * 72f - 8));
        float x = bounds.Center.X - (text.Length * 72 - 8) * scale / 2;
        float y = bounds.Center.Y - H * scale / 2;
        foreach (char c in text)
        {
            batch.Draw(atlas, new Vector2(x, y), new Rectangle((c - '0') * W * resolution, 0, W * resolution, H * resolution),
                color, 0, Vector2.Zero, scale / resolution, SpriteEffects.None, 0);
            x += 72 * scale;
        }
        return true;
    }

    private static Texture2D CreateAtlas(GraphicsDevice device, int resolution)
    {
        int width = W * resolution, height = H * resolution, samples = resolution > 1 ? 2 : Samples;
        var pixels = new Color[width * 10 * height];
        for (int digit = 0; digit < 10; digit++)
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int covered = 0;
            for (int sy = 0; sy < samples; sy++)
            for (int sx = 0; sx < samples; sx++)
            {
                bool inside = false;
                foreach (int[] polygon in Glyphs[digit])
                    if (Contains(polygon, (x + (sx + 0.5f) / samples) / resolution, (y + (sy + 0.5f) / samples) / resolution)) inside = !inside;
                if (inside) covered++;
            }
            pixels[y * width * 10 + digit * width + x] = Color.White * (covered / (float)(samples * samples));
        }
        var atlas = new Texture2D(device, width * 10, height);
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
