#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>
/// Geometry for the rules book. Every value is a design pixel on the 1920x1200 (landscape) or
/// 900x1600 (portrait) canvas and is converted through <see cref="Rect"/>; page code must never
/// add a design offset to a screen rectangle directly, hence <see cref="D"/>.
/// </summary>
public sealed class UIRulesLayout
{
    public UIEditorialLayout Canvas { get; }
    public bool Portrait => Canvas.Portrait;
    public float Scale => Canvas.Scale;

    public UIRulesLayout(Viewport viewport) => Canvas = new UIEditorialLayout(viewport);
    public UIRulesLayout(int width, int height) => Canvas = new UIEditorialLayout(width, height);

    /// <summary>Design pixels to screen pixels, for offsets added to an already-scaled rectangle.</summary>
    public int D(float designPixels) => (int)MathF.Round(designPixels * Scale);

    public Rectangle Back => Canvas.Back;
    public Rectangle Header => Canvas.Header;

    /// <summary>Four chapter chips: three reference pages and the skill index.</summary>
    public Rectangle Chip(int index) => Portrait
        ? Canvas.Rect(45 + index % 2 * 405, 200 + index / 2 * 86, 390, 76)
        : Canvas.Rect(120 + index * 400, 205, 380, 64);

    public Rectangle Sheet => Portrait
        ? Canvas.Rect(45, 380, 810, 980)
        : Canvas.Rect(120, 310, 1680, 640);

    public Rectangle Inner => Portrait
        ? Canvas.Rect(80, 415, 740, 910)
        : Canvas.Rect(195, 385, 1530, 490);

    public Rectangle Previous => Portrait ? Canvas.Rect(500, 1440, 90, 80) : Canvas.Rect(1400, 1020, 90, 80);
    public Rectangle Next => Portrait ? Canvas.Rect(730, 1440, 90, 80) : Canvas.Rect(1660, 1020, 90, 80);
    public Rectangle PageLabel => Portrait ? Canvas.Rect(590, 1440, 140, 80) : Canvas.Rect(1490, 1020, 170, 80);

    // ── Skill page ────────────────────────────────────────────────────────────
    public Rectangle Kicker => Portrait
        ? Canvas.Rect(80, 540, 650, 26)
        : Canvas.Rect(195, 385, 900, 26);

    public Rectangle Medal => Portrait
        ? Canvas.Rect(80, 576, 150, 150)
        : Canvas.Rect(195, 421, 160, 160);

    public Rectangle Name => Portrait
        ? Canvas.Rect(250, 576, 570, 150)
        : Canvas.Rect(411, 421, 900, 160);

    public Rectangle Effect => Portrait
        ? Canvas.Rect(80, 776, 740, 150)
        : Canvas.Rect(195, 611, 1250, 96);

    public Rectangle Example => Portrait
        ? Canvas.Rect(80, 966, 740, 96)
        : Canvas.Rect(195, 735, 1250, 90);

    public Rectangle Footnote => Portrait
        ? Canvas.Rect(80, 1090, 740, 60)
        : Canvas.Rect(195, 845, 1450, 26);

    public Rectangle Watermark => Portrait
        ? Canvas.Rect(560, 1140, 240, 200)
        : Canvas.Rect(1470, 660, 240, 210);

    // ── Timing page ───────────────────────────────────────────────────────────
    public Rectangle TimingRow(int stage) => Portrait
        ? Canvas.Rect(80, 465 + stage * 92, 740, 86)
        : Canvas.Rect(195, 435 + stage * 48, 1530, 44);

    public Rectangle TimingLabel(int stage) => Portrait
        ? Canvas.Rect(80, 465 + stage * 92, 285, 32)
        : Canvas.Rect(195, 435 + stage * 48, 320, 40);

    public Rectangle TimingLinks(int stage) => Portrait
        ? Canvas.Rect(370, 465 + stage * 92, 450, 80)
        : Canvas.Rect(545, 435 + stage * 48, 1180, 40);

    public Rectangle TimingFootnote => Portrait
        ? Canvas.Rect(80, 1225, 740, 70)
        : Canvas.Rect(195, 833, 1530, 34);

    // ── Index page ────────────────────────────────────────────────────────────
    public Rectangle IndexTitle => Portrait
        ? Canvas.Rect(80, 415, 300, 34)
        : Canvas.Rect(195, 385, 500, 36);

    public Rectangle IndexCount => Portrait
        ? Canvas.Rect(430, 415, 290, 32)
        : Canvas.Rect(1400, 385, 325, 36);

    public Rectangle IndexLegend => Portrait
        ? Canvas.Rect(80, 1190, 210, 72)
        : Canvas.Rect(195, 872, 620, 64);

    public int IndexColumns => Portrait ? 2 : 4;
    public int IndexRows => Portrait ? 5 : 3;

    public Rectangle IndexTileArea => Portrait
        ? Canvas.Rect(80, 465, 740, 700)
        : Canvas.Rect(195, 435, 1530, 424);

    public Rectangle Tile(int slot)
    {
        int gap = Portrait ? 12 : 18;
        int columns = IndexColumns, rows = IndexRows;
        Rectangle area = Portrait ? new Rectangle(80, 465, 740, 700) : new Rectangle(195, 435, 1530, 424);
        int width = (area.Width - gap * (columns - 1)) / columns;
        int height = (area.Height - gap * (rows - 1)) / rows;
        int column = slot % columns, row = slot / columns;
        return Canvas.Rect(area.Left + column * (width + gap), area.Top + row * (height + gap), width, height);
    }

    public Rectangle IndexPrevious => Portrait ? Canvas.Rect(298, 1190, 72, 72) : Canvas.Rect(840, 872, 64, 64);
    public Rectangle IndexNext => Portrait ? Canvas.Rect(530, 1190, 72, 72) : Canvas.Rect(1016, 872, 64, 64);
    public Rectangle IndexLabel => Portrait ? Canvas.Rect(390, 1190, 120, 72) : Canvas.Rect(916, 872, 88, 64);
}
