// PaperGrainLab 鈥?compares the CPU-baked paper grain, the previous shader revision and
// the current one on flat paper swatches and on a real captured UI frame.
//
//   dotnet run --project tools/PaperGrainLab -- <scene.png> <outDir> [--swatches]
//
// It writes PNG comparisons, zoom crops, an amplified difference image and a timing
// report. It never touches game content or save data.
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PaperGrainLab;

internal static class Program
{
    private static int Main(string[] args)
    {
        string source = args.Length > 0 ? Path.GetFullPath(args[0]) : "artifacts/audio-ui/menu-1280x720.png";
        string destination = Path.GetFullPath(args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "artifacts/paper-shader");
        if (!File.Exists(source)) { Console.Error.WriteLine($"Missing scene: {source}"); return 1; }
        Directory.CreateDirectory(destination);
        using var game = new LabGame(source, destination);
        game.Run();
        return 0;
    }
}

internal sealed record Preset(string Name, string Technique, float Tooth, float Fiber, float Mottle, float Warmth, float Keep);

internal sealed class LabGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly string _source, _destination;
    private readonly VertexPositionTexture[] _quad =
    {
        new(new Vector3(-1, 1, 0), new Vector2(0, 0)),
        new(new Vector3(1, 1, 0), new Vector2(1, 0)),
        new(new Vector3(-1, -1, 0), new Vector2(0, 1)),
        new(new Vector3(1, -1, 0), new Vector2(1, 1))
    };

    private static readonly Preset[] Presets =
    {
        new("legacy", "Legacy", 1f, 1f, 1f, 0f, .25f),
        new("soft", "Procedural", .25f, .80f, .90f, .30f, .18f),
        new("paper", "Procedural", .70f, 1.00f, .70f, .35f, .22f),
        new("linen", "Procedural", 1.10f, 1.90f, .90f, .40f, .25f)
    };

    private Texture2D _scene = null!, _swatch = null!, _paperTile = null!;
    private Effect _effect = null!;
    private RenderTarget2D? _sceneTarget, _swatchTarget, _outTarget;
    private Color[] _scenePixels = Array.Empty<Color>(), _swatchPixels = Array.Empty<Color>();
    private readonly List<string> _report = new();
    private bool _done;

    public LabGame(string source, string destination)
    {
        _source = source; _destination = destination;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 960,
            PreferredBackBufferHeight = 640,
            GraphicsProfile = GraphicsProfile.HiDef,
            SynchronizeWithVerticalRetrace = false
        };
        IsMouseVisible = false;
        IsFixedTimeStep = false;
        Window.AllowUserResizing = false;
        Content.RootDirectory = "Content";
    }

    protected override void LoadContent()
    {
        using (var stream = File.OpenRead(_source)) _scene = Texture2D.FromStream(GraphicsDevice, stream);
        _effect = Content.Load<Effect>("PaperGrain");
        _scenePixels = new Color[_scene.Width * _scene.Height];
        _scene.GetData(_scenePixels);
        _sceneTarget = CreateTarget(_scene.Width, _scene.Height);
        Blit(_scene, _sceneTarget);

        _swatch = BuildSwatchTexture(1600, 620);
        _swatchPixels = new Color[_swatch.Width * _swatch.Height];
        _swatch.GetData(_swatchPixels);
        _swatchTarget = CreateTarget(_swatch.Width, _swatch.Height);
        Blit(_swatch, _swatchTarget);

        _paperTile = BuildTileTexture(1024);
    }

    private RenderTarget2D CreateTarget(int width, int height) => new(GraphicsDevice, width, height, false,
        SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

    private void Blit(Texture2D texture, RenderTarget2D target)
    {
        using var batch = new SpriteBatch(GraphicsDevice);
        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(Color.Black);
        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(texture, new Rectangle(0, 0, target.Width, target.Height), Color.White);
        batch.End();
        GraphicsDevice.SetRenderTarget(null);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (!_done) { _done = true; Run(); }
        GraphicsDevice.Clear(Color.Black);
        base.Draw(gameTime);
    }

    private void Run()
    {
        RunSwatches();
        RunFrame();
        File.WriteAllLines(Path.Combine(_destination, "report.txt"), _report);
        foreach (string line in _report) Console.WriteLine(line);
        Console.WriteLine($"wrote comparison images to {_destination}");
        Exit();
    }

    // 鈹€鈹€ flat paper swatches 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    private void RunSwatches()
    {
        const int cropW = 180, cropH = 100, scale = 4;
        var rows = new List<(string Name, Color[] Ivory, Color[] Navy)>();

        // Reference: the game's current CPU grain on the same flat sheet.
        var cpu = (Color[])_swatchPixels.Clone();
        AddBakedGrain(cpu, _swatch.Width, _swatch.Height);
        Save(cpu, _swatch.Width, _swatch.Height, "swatch-cpu");
        rows.Add(("cpu", Crop(cpu, _swatch.Width, 70, 210, cropW, cropH, scale),
            Crop(cpu, _swatch.Width, 620, 210, cropW, cropH, scale)));

        foreach (var preset in Presets)
        {
            var pixels = RenderVariant(_swatchTarget!, _swatch.Width, _swatch.Height, preset.Technique, effect =>
            {
                Set(effect, "RenderSize", new Vector2(_swatch.Width, _swatch.Height));
                Set(effect, "GrainAmount", 1f);
                Set(effect, "ToothAmount", preset.Tooth);
                Set(effect, "FiberAmount", preset.Fiber);
                Set(effect, "MottleAmount", preset.Mottle);
                Set(effect, "Warmth", preset.Warmth);
                Set(effect, "LumaKeep", preset.Keep);
                Set(effect, "Zoom", 1f);
            });
            Save(pixels, _swatch.Width, _swatch.Height, $"swatch-{preset.Name}");
            rows.Add((preset.Name,
                Crop(pixels, _swatch.Width, 70, 210, cropW, cropH, scale),
                Crop(pixels, _swatch.Width, 620, 210, cropW, cropH, scale)));
        }

        var grid = Grid(rows, cropW * scale, cropH * scale);
        Save(grid.Pixels, grid.Width, grid.Height, "swatch-grid-4x");

        // Luminance spread tells us whether a preset matches the current tooth without
        // eyeballing it: std is the perceived grain strength on a flat sheet.
        _report.Add("swatch luminance std (dark gains are amplified by eye, so navy std matters more)");
        foreach (var row in rows)
        {
            _report.Add($"  {row.Name,-8} ivory {Spread(row.Ivory):0.00}  navy {Spread(row.Navy):0.00}");
        }
        Console.WriteLine("swatch grid rows: " + string.Join(", ", rows.Select(r => r.Name)));

        static double Spread(Color[] pixels)
        {
            double sum = 0, sumSq = 0;
            foreach (var c in pixels)
            {
                double luma = (c.R * 0.2126 + c.G * 0.7152 + c.B * 0.0722) / 255.0;
                sum += luma; sumSq += luma * luma;
            }
            double mean = sum / pixels.Length;
            return Math.Sqrt(Math.Max(0, sumSq / pixels.Length - mean * mean)) * 255;
        }
    }

    // 鈹€鈹€ real captured frame 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    private void RunFrame()
    {
        int width = _scene.Width, height = _scene.Height;
        _report.Add($"scene            {_source}");
        _report.Add($"resolution       {width}x{height} ({width * height / 1000.0:0}K pixels)");

        // Current game pipeline: grain baked into every pixel on the CPU.
        var cpuPixels = (Color[])_scenePixels.Clone();
        var cpuWatch = Stopwatch.StartNew();
        const int cpuRuns = 3;
        for (int run = 0; run < cpuRuns; run++) AddBakedGrain(cpuPixels, width, height);
        cpuWatch.Stop();
        double cpuMs = cpuWatch.Elapsed.TotalMilliseconds / cpuRuns;
        Save(cpuPixels, width, height, "01-current-cpu");
        _report.Add($"cpu grain        {cpuMs:0.00} ms per full-screen bake");

        var legacy = RenderOverScene("Legacy", e => ApplyPreset(e, Presets[0], width, height));
        Save(legacy, width, height, "02-shader-legacy");

        var paper = RenderOverScene("Procedural", e => ApplyPreset(e, Presets[1], width, height));
        Save(paper, width, height, "03-shader-paper");

        var linen = RenderOverScene("Procedural", e => ApplyPreset(e, Presets[2], width, height));
        Save(linen, width, height, "04-shader-linen");

        var tiled = RenderVariant(_sceneTarget!, width, height, "Tiled", e =>
        {
            Set(e, "TileRepeat", new Vector2(width / 1024f, height / 1024f));
            Set(e, "GrainAmount", 1f); Set(e, "ToothAmount", .4f);
            Set(e, "FiberAmount", 1f); Set(e, "MottleAmount", .8f);
            Set(e, "Warmth", .3f); Set(e, "LumaKeep", .22f);
        });
        Save(tiled, width, height, "05-shader-tiled");

        // Detail crops: an ivory sheet and a navy sheet.
        CropInto(cpuPixels, width, "10-zoom-cpu-ivory", 170, 176, 220, 74, 6);
        CropInto(legacy, width, "11-zoom-legacy-ivory", 170, 176, 220, 74, 6);
        CropInto(paper, width, "12-zoom-paper-ivory", 170, 176, 220, 74, 6);
        CropInto(linen, width, "13-zoom-linen-ivory", 170, 176, 220, 74, 6);
        CropInto(cpuPixels, width, "14-zoom-cpu-navy", 760, 400, 260, 110, 6);
        CropInto(legacy, width, "15-zoom-legacy-navy", 760, 400, 260, 110, 6);
        CropInto(paper, width, "16-zoom-paper-navy", 760, 400, 260, 110, 6);

        var diff = new Color[paper.Length];
        for (int i = 0; i < diff.Length; i++)
        {
            diff[i] = new Color(
                Math.Clamp(Math.Abs(paper[i].R - legacy[i].R) * 12, 0, 255),
                Math.Clamp(Math.Abs(paper[i].G - legacy[i].G) * 12, 0, 255),
                Math.Clamp(Math.Abs(paper[i].B - legacy[i].B) * 12, 0, 255));
        }
        Save(diff, width, height, "20-difference-paper-vs-legacy-x12");

        double legacyMs = TimePass("Legacy", width, height);
        double fineMs = TimePass("Procedural", width, height);
        double tiledMs = TimePass("Tiled", width, height);
        double passMs = TimePass("Passthrough", width, height);
        _report.Add($"gpu legacy       {legacyMs:0.000} ms per full-screen pass");
        _report.Add($"gpu procedural   {fineMs:0.000} ms per full-screen pass ({(fineMs - passMs) * 1000:0} us over a bare blit)");
        _report.Add($"gpu tiled        {tiledMs:0.000} ms per full-screen pass ({(tiledMs - passMs) * 1000:0} us over a bare blit)");
        _report.Add($"gpu passthrough  {passMs:0.000} ms per full-screen pass");
        _report.Add($"vram cpu grain   {(long)width * height * 4 / 1024.0 / 1024.0:0.0} MiB per baked size (plus the shared 256 KiB grain tile)");
        _report.Add($"vram shader      {(long)_paperTile.Width * _paperTile.Height * 4 / 1024.0 / 1024.0:0.0} MiB tiled bake, 0 MiB procedural");
    }

    private static void ApplyPreset(Effect effect, Preset preset, int width, int height)
    {
        Set(effect, "RenderSize", new Vector2(width, height));
        Set(effect, "GrainAmount", 1f);
        Set(effect, "ToothAmount", preset.Tooth);
        Set(effect, "FiberAmount", preset.Fiber);
        Set(effect, "MottleAmount", preset.Mottle);
        Set(effect, "Warmth", preset.Warmth);
        Set(effect, "LumaKeep", preset.Keep);
        Set(effect, "Zoom", 1f);
    }

    private Color[] RenderOverScene(string technique, Action<Effect> configure) =>
        RenderVariant(_sceneTarget!, _scene.Width, _scene.Height, technique, configure);

    // 鈹€鈹€ rendering helpers 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    private Color[] RenderVariant(RenderTarget2D source, int width, int height, string technique, Action<Effect> configure)
    {
        if (_outTarget == null || _outTarget.Width != width || _outTarget.Height != height)
        {
            _outTarget?.Dispose();
            _outTarget = CreateTarget(width, height);
        }
        _effect.CurrentTechnique = _effect.Techniques[technique];
        configure(_effect);
        if (_effect.Parameters["Scene"] is { } scene) scene.SetValue(source);
        if (technique == "Tiled" && _effect.Parameters["PaperTile"] is { } tile) tile.SetValue(_paperTile);
        GraphicsDevice.SetRenderTarget(_outTarget);
        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
        GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, _quad, 0, 2);
        }
        GraphicsDevice.SetRenderTarget(null);
        var pixels = new Color[width * height];
        _outTarget.GetData(pixels);
        return pixels;
    }

    private double TimePass(string technique, int width, int height)
    {
        const int iterations = 60;
        _effect.CurrentTechnique = _effect.Techniques[technique];
        if (_effect.Parameters["RenderSize"] is { } size) size.SetValue(new Vector2(width, height));
        if (_effect.Parameters["Scene"] is { } scene) scene.SetValue(_sceneTarget);
        if (technique == "Tiled" && _effect.Parameters["PaperTile"] is { } tile) tile.SetValue(_paperTile);
        GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
        GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
        for (int i = 0; i < 5; i++) DrawPass();
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) DrawPass();
        var pixels = new Color[_outTarget!.Width * _outTarget.Height];
        _outTarget.GetData(pixels); // forces the GPU to finish the whole queue
        watch.Stop();
        return watch.Elapsed.TotalMilliseconds / iterations;

        void DrawPass()
        {
            GraphicsDevice.SetRenderTarget(_outTarget);
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, _quad, 0, 2);
            }
            GraphicsDevice.SetRenderTarget(null);
        }
    }

    // 鈹€鈹€ image helpers 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    private Color[] Crop(Color[] pixels, int width, int x0, int y0, int w, int h, int scale)
    {
        var crop = new Color[w * scale * h * scale];
        for (int y = 0; y < h * scale; y++)
        for (int x = 0; x < w * scale; x++)
            crop[y * w * scale + x] = pixels[(y0 + y / scale) * width + x0 + x / scale];
        return crop;
    }

    private void CropInto(Color[] pixels, int width, string name, int x0, int y0, int w, int h, int scale)
    {
        var crop = Crop(pixels, width, x0, y0, w, h, scale);
        Save(crop, w * scale, h * scale, name);
    }

    /// <summary>Stacks crops into one sheet: one row per preset, ivory then navy.</summary>
    private (Color[] Pixels, int Width, int Height) Grid(
        List<(string Name, Color[] Ivory, Color[] Navy)> rows, int cellW, int cellH)
    {
        const int gutter = 10;
        int width = cellW * 2 + gutter * 3, height = cellH * rows.Count + gutter * (rows.Count + 1);
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(12, 20, 28);
        for (int row = 0; row < rows.Count; row++)
        {
            int y = gutter + row * (cellH + gutter);
            Stamp(rows[row].Ivory, cellW, cellH, gutter, y);
            Stamp(rows[row].Navy, cellW, cellH, cellW + gutter * 2, y);
        }
        return (pixels, width, height);

        void Stamp(Color[] source, int w, int h, int x0, int y0)
        {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[(y0 + y) * width + x0 + x] = source[y * w + x];
        }
    }

    private void Save(Color[] pixels, int width, int height, string name)
    {
        using var texture = new Texture2D(GraphicsDevice, width, height);
        texture.SetData(pixels);
        using var stream = File.Create(Path.Combine(_destination, name + ".png"));
        texture.SaveAsPng(stream, width, height);
        Console.WriteLine($"wrote {name}.png");
    }

    /// <summary>Mirror of UI/UIPaperGrain.cs plus UIPaper.SizedPaper's grain application.</summary>
    private static void AddBakedGrain(Color[] pixels, int width, int height)
    {
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float grain = (GrainTile.Sample(x, y) - .96f) * 255;
            Color c = pixels[y * width + x];
            pixels[y * width + x] = new Color(
                (int)Math.Clamp(c.R + grain, 0, 255),
                (int)Math.Clamp(c.G + grain, 0, 255),
                (int)Math.Clamp(c.B + grain, 0, 255));
        }
    }

    private static void Set(Effect effect, string name, Vector2 value)
    {
        if (effect.Parameters[name] is { } parameter) parameter.SetValue(value);
    }

    private static void Set(Effect effect, string name, float value)
    {
        if (effect.Parameters[name] is { } parameter) parameter.SetValue(value);
    }

    // 鈹€鈹€ swatch canvas and baked tile 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    /// <summary>Flat ivory, flat navy and an ivory strip with ink strokes.</summary>
    private Texture2D BuildSwatchTexture(int width, int height)
    {
        var pixels = new Color[width * height];
        var ivory = new Color(237, 232, 219);
        var navy = new Color(27, 43, 57);
        var ink = new Color(24, 34, 44);
        int third = width / 3;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Color c = x < third ? ivory : x < third * 2 ? navy : ivory;
            if (x >= third * 2)
            {
                // Ink strokes of 1, 2 and 3 px plus a 2 px "text" run, to judge legibility.
                int local = x - third * 2;
                if (y is >= 150 and < 152 || y is >= 200 and < 203 || y is >= 260 and < 264) c = ink;
                if (y is >= 330 and < 420 && local > 40 && local < 460 && local % 7 < 2) c = ink;
            }
            pixels[y * width + x] = c;
        }
        var texture = new Texture2D(GraphicsDevice, width, height);
        texture.SetData(pixels);
        return texture;
    }

    /// <summary>Bakes a tileable noise atlas: R fibres, G mottle, B tooth.</summary>
    private Texture2D BuildTileTexture(int size)
    {
        var pixels = new Color[size * size];
        // One texel maps to one screen pixel, so the bake keeps the analytic scales:
        // fibres ~4 px, mottle ~85 px, tooth ~2 px. Kept isotropic, because a
        // directional bake turns into visible streaks whenever the repeat is not square.
        const int fibreCells = 256, toothCells = 512, mottleCells = 20;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)size, v = y / (float)size;
            float fibre = TileableFbm(u * fibreCells, v * fibreCells, fibreCells, fibreCells, 3);
            float mottle = TileableFbm(u * mottleCells, v * mottleCells, mottleCells, mottleCells, 3);
            float tooth = TileableFbm(u * toothCells, v * toothCells, toothCells, toothCells, 2);
            pixels[y * size + x] = new Color(fibre, mottle, tooth);
        }
        var texture = new Texture2D(GraphicsDevice, size, size);
        texture.SetData(pixels);
        return texture;
    }

    private static float TileableFbm(float x, float y, int px, int py, int octaves)
    {
        float sum = 0, amplitude = .65f, total = 0;
        for (int i = 0; i < octaves; i++)
        {
            sum += TileableNoise(x, y, px, py) * amplitude;
            total += amplitude;
            x *= 2; y *= 2; px *= 2; py *= 2; amplitude *= .5f;
        }
        return sum / total;
    }

    private static float TileableNoise(float x, float y, int px, int py)
    {
        int ix = (int)MathF.Floor(x), iy = (int)MathF.Floor(y);
        float fx = x - ix, fy = y - iy;
        fx = fx * fx * fx * (fx * (fx * 6 - 15) + 10);
        fy = fy * fy * fy * (fy * (fy * 6 - 15) + 10);
        static float At(int x, int y, int px, int py) =>
            Hash2(((x % px) + px) % px, ((y % py) + py) % py);
        return MathHelper.Lerp(
            MathHelper.Lerp(At(ix, iy, px, py), At(ix + 1, iy, px, py), fx),
            MathHelper.Lerp(At(ix, iy + 1, px, py), At(ix + 1, iy + 1, px, py), fx), fy);
    }

    private static float Hash2(int x, int y)
    {
        unchecked
        {
            uint n = (uint)x * 374761393u + (uint)y * 668265263u + 7193u;
            n = (n ^ (n >> 13)) * 1274126177u;
            return ((n ^ (n >> 16)) & 65535) / 65535f;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scene?.Dispose(); _swatch?.Dispose(); _paperTile?.Dispose();
            _sceneTarget?.Dispose(); _swatchTarget?.Dispose(); _outTarget?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>Verbatim port of UI/UIPaperGrain.cs so the CPU baseline matches the game.</summary>
internal static class GrainTile
{
    private const int Size = 256;
    private static readonly float[] Tile = Create();
    internal static float Sample(int x, int y) => Tile[(y & (Size - 1)) * Size + (x & (Size - 1))];

    private static float[] Create()
    {
        var random = new float[Size * Size];
        uint state = 7193;
        for (int i = 0; i < random.Length; i++)
        {
            state = unchecked(state * 1664525 + 1013904223);
            random[i] = (state >> 8) / 16777215f - .5f;
        }
        var tile = new float[random.Length];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float fiber = 0;
            for (int dx = -3; dx <= 3; dx++) fiber += random[y * Size + ((x + dx) & (Size - 1))];
            tile[y * Size + x] = .96f + random[y * Size + x] * .045f + fiber / 7 * .06f;
        }
        return tile;
    }
}

