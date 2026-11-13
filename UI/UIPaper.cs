#nullable enable
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public enum UIPaperShape { Cut, Tag, Ticket }

/// <summary>Cut-paper silhouettes tinted by the atlas palette. Grain is applied by the paper shader pass.</summary>
public static class UIPaper
{
    private sealed class Materials
    {
        public Texture2D Solid = null!;
        public Texture2D? SkinMask;
        public Texture2D Seal = null!;
        public Texture2D Check = null!;
        public Texture2D Facet = null!;
        public Texture2D MenuPaper = null!;
        public Texture2D[] Masks = new Texture2D[3];
        public Color[] SkinColors = new Color[4];
        public Lazy<Texture2D>? ScenesSource;
        public Texture2D? Scenes => ScenesSource?.Value;
        public Lazy<Texture2D>? MissionScenesSource;
        public Texture2D? MissionScenes => MissionScenesSource?.Value;
        public Lazy<Texture2D>? CutpaperScenesSource;
        public Texture2D? CutpaperScenes => CutpaperScenesSource?.Value;
        public Lazy<Texture2D>? CutpaperScenesExtraSource;
        public Texture2D? CutpaperScenesExtra => CutpaperScenesExtraSource?.Value;
        public Lazy<Texture2D>[]? CampaignNodes;
        public Lazy<Texture2D>? RouteScenesSource;
        public Texture2D? RouteScenes => RouteScenesSource?.Value;
        public Texture2D[]? RoutePreviews;
        public Texture2D[]? RouteMiniatures;
        public Texture2D? Skins;
        public Texture2D? BaseSkins;
        public Lazy<Texture2D>? BackgroundSource;
        public Texture2D? Background => BackgroundSource?.Value;
        public Lazy<Texture2D>? BattleSource;
        public Texture2D? Battle => BattleSource?.Value;
        public Rectangle[] SkinRegions = new Rectangle[4];
    }
    private static readonly ConditionalWeakTable<GraphicsDevice, Materials> Cache = new();

    /// <summary>Cut silhouettes are kept at half the atlas resolution to save video memory.</summary>
    private const int Maskscale = 2;

    /// <summary>How much lighter the lit cut edge is than the sheet face.</summary>
    private const float EdgeHighlight = .22f;

    /// <summary>A cut edge catches warm light rather than neutral white.</summary>
    internal static Color LitEdge(Color face) => Color.Lerp(face, new Color(255, 250, 236), EdgeHighlight);

    private static Rectangle MaskRect(Rectangle atlas) =>
        new(atlas.X / Maskscale, atlas.Y / Maskscale,
            Math.Max(1, atlas.Width / Maskscale), Math.Max(1, atlas.Height / Maskscale));
    public static readonly Color Face = new(237, 232, 219);
    public static readonly Color Ink = new(24, 34, 44);
    public static readonly Color MutedInk = new(71, 84, 94);
    public static readonly Color Reward = new(226, 190, 127);

    private static Materials Get(GraphicsDevice device) => Cache.GetValue(device, Create);
    public static void RegisterScenes(GraphicsDevice device, Func<Texture2D> atlas) => Get(device).ScenesSource = new(atlas);
    public static void RegisterMissionScenes(GraphicsDevice device, Func<Texture2D> atlas) => Get(device).MissionScenesSource = new(atlas);
    public static void RegisterCutpaperScenes(GraphicsDevice device, Func<Texture2D> atlas, Func<Texture2D> extra)
    { Get(device).CutpaperScenesSource = new(atlas); Get(device).CutpaperScenesExtraSource = new(extra); }
    public static void RegisterCampaignNodes(GraphicsDevice device, Lazy<Texture2D>[] atlases) => Get(device).CampaignNodes = atlases;

    public static int CampaignNodeArtworkIndex(string? id) => id?.ToUpperInvariant() switch
    {
        "P" => 0, "T01" => 1, "T02" => 2, "T03" => 3, "A1" => 4, "A2" => 5,
        "A3" => 6, "AG" => 7, "B1" => 8, "B2" => 9, "B3" => 10, "BG" => 11,
        "C1" => 12, "C2" => 13, "C3" => 14, "CG" => 15, "B4" => 16, "B5" => 17,
        "C4" => 18, "C5" => 19, "D1" => 20, "D2" => 21, "E1" => 22, "E2" => 23,
        "D3" => 24, "D4" => 25, "E3" => 26, "E4" => 27, "F" => 28, _ => -1
    };

    public static void CampaignNodeLocation(SpriteBatch batch, Rectangle area, string? id, string? name, string? chapter, bool miniature = false)
    {
        int index = CampaignNodeArtworkIndex(id);
        var atlases = Get(batch.GraphicsDevice).CampaignNodes;
        if (index >= 0 && atlases != null && index / 6 < atlases.Length)
        { DrawScene(batch, atlases[index / 6].Value, area, index % 6, 1); return; }
        RouteLocation(batch, area, name, chapter, miniature);
    }
    public static void RegisterRouteScenes(GraphicsDevice device, Func<Texture2D> atlas) =>
        Get(device).RouteScenesSource = new(atlas);

    private static void PrepareRouteScenes(GraphicsDevice device, Materials material)
    {
        var atlas = material.RouteScenes!;
        var pixels = new Color[atlas.Width * atlas.Height];
        atlas.GetData(pixels);
        int width = atlas.Width / 2, cellHeight = atlas.Height / 2;
        int height = width / 2;
        material.RoutePreviews = new Texture2D[4];
        material.RouteMiniatures = new Texture2D[4];
        for (int index = 0; index < 4; index++)
        {
            var cutout = new Color[width * height];
            int ox = index % 2 * width, oy = index / 2 * cellHeight + (cellHeight - height) / 2;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float top = (1 - x / (float)width) * height * .18f;
                float alpha = Math.Clamp(y - top + .5f, 0, 1);
                cutout[y * width + x] = pixels[(oy + y) * atlas.Width + ox + x] * alpha;
            }
            var preview = new Texture2D(device, width, height);
            preview.SetData(cutout); material.RoutePreviews[index] = preview;
            var miniature = new Color[width * cellHeight];
            for (int y = 0; y < cellHeight; y++)
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[(index / 2 * cellHeight + y) * atlas.Width + index % 2 * width + x];
                float brightness = (pixel.R + pixel.G + pixel.B) / 3f;
                float alpha = Math.Clamp((145-brightness)/25f,0,1);
                miniature[y * width + x] = pixel * alpha;
            }
            var mini = new Texture2D(device,width,cellHeight);
            mini.SetData(miniature); material.RouteMiniatures[index] = mini;
        }
        device.Disposing += (_, _) => { foreach (var preview in material.RoutePreviews) preview.Dispose();
            foreach (var mini in material.RouteMiniatures) mini.Dispose(); };
    }

    public static void RouteLocation(SpriteBatch batch, Rectangle area, string? name, string? chapter, bool miniature = false)
    {
        var material = Get(batch.GraphicsDevice);
        string title = (name ?? "").ToLowerInvariant();
        if (DrawCutpaperLocation(batch, area, title, 1)) return;
        int index = title.Contains("arsenal") || title.Contains("forge") ? 0 :
            title.Contains("gate") || title.Contains("bridge") ? 1 : title.Contains("hospice") ? 2 :
            title.Contains("general") ? 3 : -1;
        if (index >= 0 && material.RouteScenes != null && material.RoutePreviews == null)
            PrepareRouteScenes(batch.GraphicsDevice,material);
        if (index < 0 || material.RouteScenes == null || material.RoutePreviews == null)
        { Location(batch, area, name, chapter); return; }
        if (!miniature) { batch.Draw(material.RoutePreviews[index], area, Color.White); return; }
        batch.Draw(material.RouteMiniatures![index], area, Color.White);
    }
    public static void RegisterMaterials(GraphicsDevice device, Texture2D skins, Func<Texture2D> background, Func<Texture2D>? battle = null)
    {
        var material = Get(device); material.BaseSkins = skins; material.BackgroundSource = new(background); material.BattleSource = battle == null ? null : new(battle);
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
        // Retain just the cut silhouettes. The shader adds the tooth, so the sheets
        // themselves are drawn straight from these masks and the atlas palette.
        // Masks are half the atlas resolution: a full-size mask is 6 MiB each and the
        // extra detail only survives on the largest sheets, where a 2 px edge is fine.
        int maskWidth = skins.Width / Maskscale, maskHeight = skins.Height / Maskscale;
        for (int shape = 0; shape < 3; shape++)
        {
        var mask = new Color[maskWidth * maskHeight];
        for (int i = 0; i < 4; i++)
        {
            Rectangle region = material.SkinRegions[i];
            region.Inflate(-12, -12);
            int left = region.Left / Maskscale, right = region.Right / Maskscale;
            int top = region.Top / Maskscale, bottom = region.Bottom / Maskscale;
            for (int y = top; y < bottom; y++)
            for (int x = left; x < right; x++)
            {
                float ax = x * Maskscale + .5f, ay = y * Maskscale + .5f;
                float dx = Math.Min(ax - region.Left, region.Right - 1 - ax) / Maskscale;
                float dy = Math.Min(ay - region.Top, region.Bottom - 1 - ay) / Maskscale;
                float coverage = Math.Clamp((dx + dy - 20f / Maskscale) / 1.4142f + .5f, 0, 1);
                if (shape == (int)UIPaperShape.Tag)
                {
                    float edge = (region.Right - 1 - ax) / Maskscale;
                    float point = Math.Abs(ay - region.Center.Y) / Maskscale * .14f;
                    coverage = Math.Min(Math.Clamp(edge - point + .5f, 0, 1),
                        Math.Clamp((ax - region.Left + dy * Maskscale - 10) / Maskscale / 1.4142f + .5f, 0, 1));
                }
                else if (shape == (int)UIPaperShape.Ticket)
                {
                    float corner = (ax < region.Center.X) == (ay < region.Center.Y) ? 34f : 9f;
                    coverage = Math.Clamp((dx + dy - corner / Maskscale) / 1.4142f + .5f, 0, 1);
                }
                mask[y * maskWidth + x] = Color.White * coverage;
            }
        }
        material.Masks[shape]?.Dispose();
        // SpriteBatch expects premultiplied RGBA masks. Alpha8 sampling differs
        // between graphics backends and can turn transparent corners opaque black.
        material.Masks[shape] = new Texture2D(device, maskWidth, maskHeight,false,SurfaceFormat.Color);
        material.Masks[shape].SetData(mask);
        }
        // The paper shader owns grain, so each sheet only needs its atlas palette plus a
        // cut silhouette. This replaces the per-size baked textures and their cache.
        for (int i = 0; i < 4; i++)
        {
            Rectangle cell = material.SkinRegions[i];
            int left = cell.Left + cell.Width / 4, right = cell.Right - cell.Width / 4;
            int top = cell.Top + cell.Height / 4, bottom = cell.Bottom - cell.Height / 4;
            double r = 0, g = 0, b = 0;
            int count = 0;
            for (int y = top; y < bottom; y++)
            for (int x = left; x < right; x++)
            {
                Color pixel = pixels[y * skins.Width + x];
                if (pixel.A < 250) continue;
                r += pixel.R; g += pixel.G; b += pixel.B;
                count++;
            }
            material.SkinColors[i] = count == 0 ? Face
                : new Color((int)(r / count), (int)(g / count), (int)(b / count));

        }
        material.Skins = skins;
        material.SkinMask = material.Masks[0];
    }

    private static Materials Create(GraphicsDevice device)
    {
        var result = new Materials
        {
            Solid = new Texture2D(device, 1, 1),
            Seal = CreateSeal(device),
            Check = CreateCheck(device),
            Facet = CreateFacet(device),
            MenuPaper = CreateMenuPaper(device)
        };
        result.Solid.SetData(new[] { Color.White });
        device.Disposing += (_, _) =>
        {
            foreach (var mask in result.Masks) mask?.Dispose();
            result.Solid.Dispose();
            result.Seal.Dispose();
            result.Check.Dispose();
            result.Facet.Dispose();
            result.MenuPaper.Dispose();
        };
        return result;
    }
    private static Texture2D CreateMenuPaper(GraphicsDevice device)
    {
        const int width = 512, height = 128;
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float left = Math.Max(0, 14 - Math.Min(y, height - 1 - y));
            float right = 32 * Math.Abs(2f * y / (height - 1) - 1);
            float coverage = Math.Clamp(Math.Min(x - left, width - 1 - right - x) + 1, 0, 1);
            pixels[y * width + x] = new Color(.91f, .91f, .91f) * coverage;
        }
        var texture = new Texture2D(device, width, height);
        texture.SetData(pixels);
        return texture;
    }

    public static void MenuPaper(SpriteBatch batch, Rectangle bounds, Color color, bool lit = true)
    {
        if(bounds.Width<1 || bounds.Height<1)return;
        var texture=Get(batch.GraphicsDevice).MenuPaper;
        if(lit)
        {
            var edge=bounds; edge.Offset(-1,-1);
            batch.Draw(texture,edge,LitEdge(color));
        }
        batch.Draw(texture,bounds,color);
    }

    private static Texture2D CreateFacet(GraphicsDevice device)
    {
        const int size = 256;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = x / (float)size, py = y / (float)size;
            float coverage = Math.Clamp(Math.Min(Math.Min(px - .10f * (1 - py), 1 - px - .14f * py),
                Math.Min(py - .06f * px, .85f + .15f * px - py)) * size, 0, 1);
            pixels[y * size + x] = new Color(.91f, .91f, .91f) * coverage;
        }
        var texture = new Texture2D(device, size, size); texture.SetData(pixels); return texture;
    }

    public static void Backdrop(SpriteBatch batch, Viewport viewport, GameTime time)
    {
        Texture2D texture = Get(batch.GraphicsDevice).Facet;
        float w = viewport.Width, h = viewport.Height;
        float drift = UIMotion.Reduced ? 0 : MathF.Sin((float)time.TotalGameTime.TotalSeconds * .22f) * .008f;
        void Layer(float x, float y, float width, float height, float angle, Color color)
        {
            var center = new Vector2(x * w, y * h);
            var scale = new Vector2(width * w / texture.Width, height * h / texture.Height);
            var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            batch.Draw(texture, center + new Vector2(1, 2), null, Color.Black * .18f, angle + drift, origin, scale, SpriteEffects.None, 0);
            batch.Draw(texture, center, null, color, angle + drift, origin, scale, SpriteEffects.None, 0);
        }
        Layer(.18f, .78f, .68f, .62f, -.24f, new Color(19, 37, 51));
        Layer(.78f, .54f, .57f, 1.20f, .18f, new Color(24, 44, 59));
        Layer(.65f, .95f, .75f, .33f, .12f, new Color(31, 51, 65));
    }

    private static Texture2D CreateSeal(GraphicsDevice device)
    {
        const int size = 512;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int covered = 0;
            for (int sy = 0; sy < 2; sy++)
            for (int sx = 0; sx < 2; sx++)
            {
                float px = (x + .25f + sx * .5f) / size, py = (y + .25f + sy * .5f) / size;
                float inset = py < .2f ? .5f - py * 2.5f : (py - .2f) * .055f;
                bool notch = py > .83f && Math.Abs(px - .5f) < (py - .83f) * 3.4f;
                if (px >= inset && px <= 1 - inset && !notch) covered++;
            }
            pixels[y * size + x] = new Color(.94f, .94f, .94f) * (covered / 4f);
        }
        var texture = new Texture2D(device, size, size);
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateCheck(GraphicsDevice device)
    {
        const int size = 256;
        // One continuous cut-paper silhouette keeps the inner and outer joins aligned.
        Vector2[] outline = { new(.27f, .43f), new(.43f, .59f), new(.70f, .27f),
            new(.79f, .36f), new(.44f, .78f), new(.18f, .52f) };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int covered = 0;
            for (int sy = 0; sy < 4; sy++)
            for (int sx = 0; sx < 4; sx++)
            {
                float px = (x + (sx + .5f) / 4) / size;
                float py = (y + (sy + .5f) / 4) / size;
                bool inside = false;
                for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
                {
                    Vector2 a = outline[i], b = outline[j];
                    if ((a.Y > py) != (b.Y > py) && px < (b.X - a.X) * (py - a.Y) / (b.Y - a.Y) + a.X)
                        inside = !inside;
                }
                if (inside) covered++;
            }
            pixels[y * size + x] = Color.White * (covered / 16f);
        }
        var texture = new Texture2D(device, size, size);
        texture.SetData(pixels);
        return texture;
    }

    public static void Checkmark(SpriteBatch batch, Rectangle bounds) =>
        batch.Draw(Get(batch.GraphicsDevice).Check, bounds, Ink);

    private static void Slices(SpriteBatch batch, Texture2D texture, Rectangle area, Color tint, int sourceEdge, int edge)
        => Slices(batch, texture, area, texture.Bounds, tint, sourceEdge, sourceEdge, edge);

    private static void Slices(SpriteBatch batch, Texture2D texture, Rectangle area, Rectangle source, Color tint, int sourceX, int sourceY, int edge)
    {
        if (area.Width < 1 || area.Height < 1) return;
        edge = Math.Max(0, Math.Min(edge, Math.Min(area.Width, area.Height) / 2));
        int[] sx = { source.Left, source.Left + sourceX, source.Right - sourceX, source.Right };
        int[] sy = { source.Top, source.Top + sourceY, source.Bottom - sourceY, source.Bottom };
        int[] dx = { area.Left, area.Left + edge, area.Right - edge, area.Right };
        int[] dy = { area.Top, area.Top + edge, area.Bottom - edge, area.Bottom };
        for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
        {
            var target = new Rectangle(dx[col], dy[row], dx[col + 1] - dx[col], dy[row + 1] - dy[row]);
            if (target.Width > 0 && target.Height > 0)
                batch.Draw(texture, target, new Rectangle(sx[col], sy[row], sx[col + 1] - sx[col], sy[row + 1] - sy[row]), tint);
        }
    }

    // Offset copies soften the actual cut edge in screen pixels, without a rectangular halo.
    private static void PaperShadow(Action<Vector2, float> draw, float depth, float opacity = 1)
    {
        Vector2 offset = new(.35f, Math.Min(1.5f, depth * .35f));
        draw(offset, .13f * opacity);
        for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
            draw(offset + new Vector2(x, y) * .65f, .018f * opacity);
    }

    /// <summary>Three square sheets stacked with slight offsets; the top sheet stays upright.</summary>
    public static void StackedSquare(SpriteBatch batch, Rectangle bounds, Color color)
    {
        if (bounds.Width < 1 || bounds.Height < 1) return;
        var material = Get(batch.GraphicsDevice);
        float opacity = color.A / 255f;
        if (opacity <= 0) return;
        float red = color.R / opacity, green = color.G / opacity, blue = color.B / opacity;
        int index = green > red * 1.25f && red > 65 ? 1
            : red > 140 ? 0 : red + green + blue > 100 ? 3 : 2;
        Color face = material.SkinColors[index] * (Color.White * opacity);
        Vector2 origin = new(.5f, .5f);
        Vector2 scale = new(bounds.Width, bounds.Height);
        Vector2 center = bounds.Center.ToVector2();

        void Sheet(Vector2 offset, float rotation, float shade)
        {
            Color tint = shade < 0
                ? Color.Lerp(face, new Color(4, 10, 16), -shade)
                : Color.Lerp(face, Color.White, shade);
            Vector2 at = center + offset;
            batch.Draw(material.Solid, at + new Vector2(0, 3), null, Color.Black * (.18f * opacity),
                rotation, origin, scale, SpriteEffects.None, 0);
            batch.Draw(material.Solid, at + new Vector2(0, 1), null, Color.Black * (.08f * opacity),
                rotation, origin, scale, SpriteEffects.None, 0);
            // Lit cut edge: one pixel of the same paper nudged up and left.
            batch.Draw(material.Solid, at - new Vector2(1, 1), null, LitEdge(tint),
                rotation, origin, scale, SpriteEffects.None, 0);
            batch.Draw(material.Solid, at, null, tint, rotation, origin, scale, SpriteEffects.None, 0);
        }

        // Back to front: two loose sheets under the clean top one.
        Sheet(new Vector2(-18, 20), -.028f, -.38f);
        Sheet(new Vector2(11, 10), .016f, .10f);
        Sheet(Vector2.Zero, 0f, 0f);
    }
    public static void Medal(SpriteBatch batch, Rectangle bounds)
    {
        var texture = Get(batch.GraphicsDevice).Seal;
        PaperShadow((offset, alpha) => batch.Draw(texture, bounds.Location.ToVector2() + offset,
            null, Color.Black * alpha, 0, Vector2.Zero, new Vector2(bounds.Width / (float)texture.Width,
            bounds.Height / (float)texture.Height), SpriteEffects.None, 0), 3);
        var lit = bounds; lit.Offset(-1, -1);
        batch.Draw(texture, lit, LitEdge(Face));
        batch.Draw(texture, bounds, Face);
    }

    public static void Button(SpriteBatch batch, Rectangle bounds, Color color, float hover, float press,
        bool enabled = true, float opacity = 1, int cut = 7, UIPaperShape shape = UIPaperShape.Cut)
    {
        Sheet(batch, bounds, color * opacity, enabled && press > .5f ? 0 : 3, cut, shape: shape);
        if (!enabled || opacity <= 0) return;
        var material = Get(batch.GraphicsDevice);
        Color highlight = Color.Lerp(UITheme.AccentBlue, Color.White, .55f) *
            (Math.Clamp(hover, 0, 1) * .28f * opacity);
        Color pressure = Color.Black * (Math.Clamp(press, 0, 1) * .09f * opacity);
        int index = color.G > color.R * 1.25f && color.R > 65 ? 1
            : color.R > 140 ? 0 : color.R + color.G + color.B > 100 ? 3 : 2;
        Rectangle source = material.SkinRegions[index];
        int edge = Math.Min(Math.Max(14, cut * 3), Math.Min(bounds.Width, bounds.Height) / 3);
        int sx = Math.Min(64, source.Width / 3), sy = Math.Min(64, source.Height / 3);
        Rectangle maskSource = MaskRect(source);
        int msx = Math.Max(1, sx / Maskscale), msy = Math.Max(1, sy / Maskscale);
        Texture2D mask = material.Masks[(int)shape];
        // Same rule as Sheet: asymmetric silhouettes are stretched, never sliced.
        if (shape == UIPaperShape.Cut)
        {
            Slices(batch, mask, bounds, maskSource, highlight, msx, msy, edge);
            Slices(batch, mask, bounds, maskSource, pressure, msx, msy, edge);
        }
        else
        {
            batch.Draw(mask, bounds, maskSource, highlight);
            batch.Draw(mask, bounds, maskSource, pressure);
        }
    }

    public static void Sheet(SpriteBatch batch, Rectangle bounds, Color color, int depth = 3, int cut = 7, bool shadow = true, UIPaperShape shape = UIPaperShape.Cut)
    {
        if (bounds.Width < 1 || bounds.Height < 1) return;
        var material = Get(batch.GraphicsDevice);
        Texture2D? mask = material.Masks[(int)shape];
        if (mask == null) return;
        float opacity = color.A / 255f;
        if (opacity <= 0) return;
        float red = color.R / opacity, green = color.G / opacity, blue = color.B / opacity;
        int index = green > red * 1.25f && red > 65 ? 1
            : red > 140 ? 0 : red + green + blue > 100 ? 3 : 2;
        Rectangle source = material.SkinRegions[index];
        int sx = Math.Min(64, source.Width / 3), sy = Math.Min(64, source.Height / 3);
        int edge = Math.Min(Math.Max(14, cut * 3), Math.Min(bounds.Width, bounds.Height) / 3);
        Color tint = index == 0 && color.R > color.B * 1.3f ? new Color(255, 220, 155) : Color.White;
        tint *= opacity;
        Rectangle maskSource = MaskRect(source);
        int msx = Math.Max(1, sx / Maskscale), msy = Math.Max(1, sy / Maskscale);
        // Only the symmetric cut is nine-slice safe. Tag and Ticket carry a 34 px corner
        // on one side and 9 px on the other, so slicing them tears the diagonal open;
        // those shapes are drawn as a single stretched silhouette instead.
        bool sliced = shape == UIPaperShape.Cut;
        void Piece(Rectangle dest, Color pieceTint)
        {
            if (sliced) Slices(batch, mask, dest, maskSource, pieceTint, msx, msy, edge);
            else batch.Draw(mask, dest, maskSource, pieceTint);
        }
        if (shadow && depth > 0)
        {
            PaperShadow((offset, alpha) =>
            {
                var shade = bounds; shade.Offset((int)Math.Round(offset.X), (int)Math.Round(offset.Y));
                Piece(shade, Color.Black * alpha);
            }, depth, opacity);
        }
        if (depth > 0)
        {
            var backing = bounds; backing.Offset(0, 1);
            Piece(backing, Color.Lerp(color, UITheme.BgTop, .38f));
        }
        // Palette colour plus the cut silhouette; the paper pass supplies the grain.
        Color face = material.SkinColors[index] * tint;
        // The cut edge catches light: the same silhouette, one pixel up and left.
        var lit = bounds; lit.Offset(-1, -1);
        Piece(lit, LitEdge(face));
        Piece(bounds, face);
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
        // One flat sheet; the paper pass adds tooth over the whole frame.
        var material = Get(batch.GraphicsDevice);
        batch.Draw(material.Solid, viewport.Bounds, material.SkinColors[2] * new Color(175, 185, 195));
    }

    public static void BattleBackground(SpriteBatch batch, Viewport viewport)
    {
        var material = Get(batch.GraphicsDevice);
        if (material.Battle != null) { batch.Draw(material.Battle, viewport.Bounds, Color.White); return; }
        var sheet = material.SkinRegions[2];
        var grain = new Rectangle(sheet.Center.X - 128, sheet.Center.Y - 128, 256, 256);
        Tile(batch, material.BaseSkins!, viewport.Bounds, grain, new Color(185, 195, 205), .8f, .8f);
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

    public static void ChapterLocation(SpriteBatch batch, Rectangle area, string? name)
    {
        var material = Get(batch.GraphicsDevice);
        string title = (name ?? "").ToLowerInvariant();
        int index = title switch
        {
            "bell tower" => 3, "courtyard" => 4, "dragon tomb" => 0, "blank archive" => 1, _ => -1
        };
        Texture2D? atlas = title == "bell tower" || title == "courtyard"
            ? material.CutpaperScenes : material.CutpaperScenesExtra;
        if (index >= 0 && atlas != null) { DrawScene(batch, atlas, area, index, 1); return; }
        Scene(batch, area, ChapterIndex(name));
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

    public static void Location(SpriteBatch batch, Rectangle area, string? name, string? chapter, float opacity = 1)
    {
        string title = (name ?? "").ToLowerInvariant();
        if (DrawCutpaperLocation(batch, area, title, opacity)) return;
        int index = title.Contains("arsenal") || title.Contains("forge") ? 0 :
            title.Contains("gate") || title.Contains("bridge") ? 1 :
            title.Contains("hospice") ? 2 : title.Contains("general") ? 3 :
            title.Contains("bell") || title.Contains("tower") ? 4 : title.Contains("court") ? 5 : -1;
        Texture2D? atlas = Get(batch.GraphicsDevice).MissionScenes;
        if (index < 0 || atlas == null) { Scene(batch, area, MissionScene(name, chapter), opacity); return; }
        DrawScene(batch, atlas, area, index, opacity);
    }

    private static bool DrawCutpaperLocation(SpriteBatch batch, Rectangle area, string title, float opacity)
    {
        var material = Get(batch.GraphicsDevice);
        int extra = title.Contains("dragon") || title.Contains("wyrm") ? 0 :
            title.Contains("archive") || title.Contains("scribe") || title.Contains("unwritten") ? 1 :
            title.Contains("ferry") ? 2 : title.Contains("oracle") ? 3 :
            title.Contains("mirror") || title.Contains("reflection") ? 4 :
            title.Contains("watch") ? 5 : -1;
        if (extra >= 0 && material.CutpaperScenesExtra != null)
        { DrawScene(batch, material.CutpaperScenesExtra, area, extra, opacity); return true; }
        int index = title.Contains("arsenal") || title.Contains("forge") ? 0 :
            title.Contains("gate") || title.Contains("bridge") || title.Contains("crossing") ? 1 :
            title.Contains("hospice") ? 2 : title.Contains("bell") || title.Contains("tower") ? 3 :
            title.Contains("altar") || title.Contains("idol") ? 4 :
            title.Contains("court") || title.Contains("emperor") || title.Contains("general") ? 5 : -1;
        if (index < 0 || material.CutpaperScenes == null) return false;
        DrawScene(batch, material.CutpaperScenes, area, index, opacity); return true;
    }

    public static void Scene(SpriteBatch batch, Rectangle area, int index, float opacity = 1)
    {
        Texture2D? atlas = Get(batch.GraphicsDevice).Scenes;
        if (atlas == null || area.Width < 1 || area.Height < 1) return;
        DrawScene(batch, atlas, area, index, opacity);
    }

    private static void DrawScene(SpriteBatch batch, Texture2D atlas, Rectangle area, int index, float opacity)
    {
        if (area.Width < 1 || area.Height < 1) return;
        int w = atlas.Width / 3, h = atlas.Height / 2;
        index = Math.Clamp(index, 0, 5);
        var source = new Rectangle(index % 3 * w, index / 3 * h, w, h);
        float targetAspect = area.Width / (float)area.Height;
        if (targetAspect > w / (float)h) { int crop = (int)(w / targetAspect); source.Y += (h - crop) / 2; source.Height = Math.Max(1, crop); }
        else { int crop = (int)(h * targetAspect); source.X += (w - crop) / 2; source.Width = Math.Max(1, crop); }
        batch.Draw(atlas, area, source, Color.White * opacity);
    }

}













