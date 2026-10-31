#nullable enable
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>Cached, nine-sliced paper material. All visual depth is independent of hit geometry.</summary>
public static class UIPaper
{
    private sealed class Materials
    {
        public Texture2D Paper = null!;
        public Texture2D Shadow = null!;
        public Texture2D? Scenes;
        public Texture2D? Skins;
        public Texture2D? Background;
        public Texture2D? Battle;
        public Rectangle[] SkinRegions = new Rectangle[4];
    }
    private static readonly ConditionalWeakTable<GraphicsDevice, Materials> Cache = new();
    public static readonly Color Face = new(237, 232, 219);
    public static readonly Color Ink = new(24, 34, 44);
    public static readonly Color MutedInk = new(71, 84, 94);
    public static readonly Color Reward = new(226, 190, 127);

    private static Materials Get(GraphicsDevice device) => Cache.GetValue(device, Create);
    public static void RegisterScenes(GraphicsDevice device, Texture2D atlas) => Get(device).Scenes = atlas;
    public static void RegisterMaterials(GraphicsDevice device, Texture2D skins, Texture2D background, Texture2D? battle = null)
    {
        var material = Get(device); material.Skins = skins; material.Background = background; material.Battle = battle;
        var pixels = new Color[skins.Width * skins.Height]; skins.GetData(pixels);
        int w = skins.Width / 2, h = skins.Height / 2;
        for (int i = 0; i < 4; i++)
        {
            int ox = i % 2 * w, oy = i / 2 * h, left = w, top = h, right = 0, bottom = 0;
            // Ignore detached flecks in the atlas margin when locating the sheet.
            var visited = new bool[w * h];
            var queue = new int[w * h];
            int largest = 0;
            for (int seed = 0; seed < visited.Length; seed++)
            {
                if (visited[seed] || pixels[(oy + seed / w) * skins.Width + ox + seed % w].A <= 230) continue;
                int head = 0, count = 1, l = w, t = h, r = 0, b = 0;
                queue[0] = seed; visited[seed] = true;
                while (head < count)
                {
                    int point = queue[head++], x = point % w, y = point / w;
                    l = Math.Min(l,x); t = Math.Min(t,y); r = Math.Max(r,x); b = Math.Max(b,y);
                    Visit(x - 1,y); Visit(x + 1,y); Visit(x,y - 1); Visit(x,y + 1);
                }
                if (count > largest) { largest = count; left = l; top = t; right = r; bottom = b; }
                void Visit(int x, int y)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) return;
                    int p = y * w + x;
                    if (visited[p] || pixels[(oy+y)*skins.Width+ox+x].A <= 230) return;
                    visited[p] = true; queue[count++] = p;
                }
            }
            material.SkinRegions[i] = new Rectangle(ox + Math.Max(0,left - 12), oy + Math.Max(0,top - 12),
                Math.Min(w - Math.Max(0,left - 12), right - left + 25), Math.Min(h - Math.Max(0,top - 12), bottom - top + 25));
        }
    }

    private static Materials Create(GraphicsDevice device)
    {
        const int size = 128;
        var grain = new Color[size * size];
        var shadow = new Color[size * size];
        uint state = 7193;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            state = state * 1664525 + 1013904223;
            int value = 248 + (int)(state >> 28) / 3;
            bool cut = x + y < 10 || (127 - x) + y < 10 || x + (127 - y) < 10 || (127 - x) + (127 - y) < 10;
            grain[y * size + x] = cut ? Color.Transparent : new Color(value, value, value);
            float dx = Math.Max(0, Math.Max(20 - x, x - 107)), dy = Math.Max(0, Math.Max(20 - y, y - 107));
            float alpha = (float)Math.Exp(-(dx * dx + dy * dy) / 65f) * .38f;
            shadow[y * size + x] = Color.Black * alpha;
        }
        var result = new Materials { Paper = new Texture2D(device, size, size), Shadow = new Texture2D(device, size, size) };
        result.Paper.SetData(grain); result.Shadow.SetData(shadow);
        device.Disposing += (_, _) => { result.Paper.Dispose(); result.Shadow.Dispose(); };
        return result;
    }

    private static void Slices(SpriteBatch batch, Texture2D texture, Rectangle area, Color tint, int sourceEdge, int edge)
    {
        if (area.Width < 1 || area.Height < 1) return;
        edge = Math.Max(0, Math.Min(edge, Math.Min(area.Width, area.Height) / 2));
        int[] sx = { 0, sourceEdge, texture.Width - sourceEdge, texture.Width };
        int[] sy = { 0, sourceEdge, texture.Height - sourceEdge, texture.Height };
        int[] dx = { area.Left, area.Left + edge, area.Right - edge, area.Right };
        int[] dy = { area.Top, area.Top + edge, area.Bottom - edge, area.Bottom };
        for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
        {
            var target = new Rectangle(dx[col], dy[row], dx[col + 1] - dx[col], dy[row + 1] - dy[row]);
            if (target.Width > 0 && target.Height > 0)
                batch.Draw(texture, target, new Rectangle(sx[col], sy[row], sx[col + 1] - sx[col], sy[row + 1] - sy[row]), tint);
        }
    }

    public static void Sheet(SpriteBatch batch, Rectangle bounds, Color color, int depth = 3, int cut = 7, bool shadow = true)
    {
        if (bounds.Width < 1 || bounds.Height < 1) return;
        var material = Get(batch.GraphicsDevice);
        if (material.Skins != null)
        {
            float opacity = color.A / 255f;
            if(opacity <= 0) return;
            if(shadow && depth>0)
            {
                var shade=bounds; shade.Offset(1,depth+2); shade.Inflate(4,4);
                Slices(batch,material.Shadow,shade,Color.White*opacity,24,10);
            }
            float red = color.R / opacity, green = color.G / opacity, blue = color.B / opacity;
            int index = green > red * 1.25f && red > 65 ? 1
                : red > 140 ? 0 : red + green + blue > 100 ? 3 : 2;
            Rectangle source = material.SkinRegions[index];
            int sx = Math.Min(64, source.Width / 3), sy = Math.Min(64, source.Height / 3);
            int edge = Math.Min(Math.Max(14, cut * 3), Math.Min(bounds.Width, bounds.Height) / 3);
            int[] xs = { source.Left, source.Left + sx, source.Right - sx, source.Right };
            int[] ys = { source.Top, source.Top + sy, source.Bottom - sy, source.Bottom };
            int[] dx = { bounds.Left, bounds.Left + edge, bounds.Right - edge, bounds.Right };
            int[] dy = { bounds.Top, bounds.Top + edge, bounds.Bottom - edge, bounds.Bottom };
            Color tint = index == 0 && color.R > color.B * 1.3f ? new Color(255, 220, 155) : Color.White;
            tint *= color.A / 255f;
            for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
            {
                var target = new Rectangle(dx[col], dy[row], dx[col + 1] - dx[col], dy[row + 1] - dy[row]);
                if(target.Width > 0 && target.Height > 0)
                {
                    var patch = new Rectangle(xs[col], ys[row], xs[col + 1] - xs[col], ys[row + 1] - ys[row]);
                    batch.Draw(material.Skins, target, patch, tint);
                }
            }
            return;
        }
        if (shadow)
        {
            var shade = bounds; shade.Offset(2, depth + 3); shade.Inflate(7, 6);
            Slices(batch, material.Shadow, shade, Color.White, 24, 12);
        }
        if (depth > 0)
        {
            var backing = bounds; backing.Offset(1, depth);
            Slices(batch, material.Paper, backing, Color.Lerp(color, UITheme.BgTop, .38f), 16, cut);
        }
        Slices(batch, material.Paper, bounds, color, 16, cut);
    }

    private static void Tile(SpriteBatch batch, Texture2D texture, Rectangle target, Rectangle source, Color color, float scaleX, float scaleY)
    {
        int tileWidth = Math.Max(1, (int)(source.Width * scaleX)), tileHeight = Math.Max(1, (int)(source.Height * scaleY));
        for (int y = target.Top; y < target.Bottom; y += tileHeight)
        for (int x = target.Left; x < target.Right; x += tileWidth)
        {
            int width = Math.Min(tileWidth, target.Right - x), height = Math.Min(tileHeight, target.Bottom - y);
            batch.Draw(texture, new Rectangle(x,y,width,height), new Rectangle(source.Left,source.Top,
                Math.Max(1,(int)(width / scaleX)),Math.Max(1,(int)(height / scaleY))), color);
        }
    }

    public static void Background(SpriteBatch batch, Viewport viewport)
    {
        var material = Get(batch.GraphicsDevice);
        if(material.Skins != null)
        {
            var sheet = material.SkinRegions[2];
            batch.Draw(material.Skins, viewport.Bounds, new Rectangle(sheet.Center.X - 100, sheet.Center.Y - 100, 200, 200),
                new Color(175,185,195));
            return;
        }
        Sheet(batch, viewport.Bounds, UITheme.BgTop, 0, 0, false);
        int width = viewport.Width, height = viewport.Height;
        Sheet(batch, new Rectangle(width * 53 / 100, height * 8 / 100, width * 42 / 100, height * 82 / 100), UITheme.PanelDark, 2, 14);
        Sheet(batch, new Rectangle(width * 60 / 100, height * 15 / 100, width * 37 / 100, height * 72 / 100), UITheme.Panel, 2, 18);
    }

    public static void BattleBackground(SpriteBatch batch, Viewport viewport)
    {
        var material = Get(batch.GraphicsDevice);
        if (material.Battle != null) { batch.Draw(material.Battle, viewport.Bounds, Color.White); return; }
        if (material.Skins == null) { Background(batch, viewport); return; }
        var sheet = material.SkinRegions[2];
        var grain = new Rectangle(sheet.Center.X - 128, sheet.Center.Y - 128, 256, 256);
        Tile(batch, material.Skins, viewport.Bounds, grain, new Color(185, 195, 205), .8f, .8f);
    }

    public static int ChapterIndex(string? name)
    {
        if (name?.Contains("Finale", StringComparison.OrdinalIgnoreCase) == true || name?.Contains("Ferry", StringComparison.OrdinalIgnoreCase) == true) return 5;
        if (name?.Contains("Chapter III", StringComparison.OrdinalIgnoreCase) == true) return 4;
        if (name?.Contains("Chapter II", StringComparison.OrdinalIgnoreCase) == true) return 3;
        if (name?.Contains("Chapter I", StringComparison.OrdinalIgnoreCase) == true) return 2;
        if (name?.Contains("Long Road", StringComparison.OrdinalIgnoreCase) == true) return 1;
        return 0;
    }

    public static int MissionScene(string? name,string? chapter)
    {
        string title=(name ?? "").ToLowerInvariant();
        if(title.Contains("gate") || title.Contains("bridge")) return 3;
        if(title.Contains("arsenal") || title.Contains("forge")) return 2;
        if(title.Contains("hospice") || title.Contains("palace")) return 0;
        if(title.Contains("general") || title.Contains("stronghold")) return 4;
        return ChapterIndex(chapter);
    }

    public static void Scene(SpriteBatch batch, Rectangle area, int index, float opacity = 1)
    {
        Texture2D? atlas = Get(batch.GraphicsDevice).Scenes;
        if (atlas == null || area.Width < 1 || area.Height < 1) return;
        int w = atlas.Width / 3, h = atlas.Height / 2;
        index = Math.Clamp(index, 0, 5);
        var source = new Rectangle(index % 3 * w, index / 3 * h, w, h);
        float targetAspect = area.Width / (float)area.Height;
        if (targetAspect > w / (float)h) { int crop = (int)(w / targetAspect); source.Y += (h - crop) / 2; source.Height = Math.Max(1, crop); }
        else { int crop = (int)(h * targetAspect); source.X += (w - crop) / 2; source.Width = Math.Max(1, crop); }
        batch.Draw(atlas, area, source, Color.White * opacity);
    }
}
