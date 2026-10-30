using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>Shared draw/input coordinates for the sparse menu screens, including portrait windows.</summary>
public sealed class UIEditorialLayout
{
    public bool Portrait { get; }
    public float Scale { get; }
    private readonly Vector2 _offset;
    public UIEditorialLayout(Viewport viewport) : this(viewport.Width, viewport.Height) { }
    public UIEditorialLayout(int width, int height)
    {
        Portrait = height > width;
        int designWidth = Portrait ? 900 : 1920, designHeight = Portrait ? 1600 : 1200;
        Scale = Math.Max(.01f, Math.Min(width / (float)designWidth, height / (float)designHeight));
        _offset = new Vector2((width - designWidth * Scale) / 2, (height - designHeight * Scale) / 2);
    }
    public Rectangle Rect(int x, int y, int w, int h) => new(
        (int)Math.Round(_offset.X + x * Scale), (int)Math.Round(_offset.Y + y * Scale),
        Math.Max(1, (int)Math.Round(w * Scale)), Math.Max(1, (int)Math.Round(h * Scale)));
    public Rectangle MenuButton(int row, bool connected) => Portrait
        ? Rect(80, connected ? 590 + row * 115 : 710 + row * 115, 740, 94)
        : Rect(150, connected ? 280 + row * 105 : 280 + row * 125, 620, connected ? 86 : 100);
    public Rectangle Back => Rect(Portrait ? 45 : 70, 60, 72, 72);
    public Rectangle Header => Rect(Portrait ? 150 : 175, 60, 600, 80);
    public Rectangle Ready => Portrait ? Rect(100, 1200, 700, 100) : Rect(600, 920, 720, 100);
    public Rectangle Leave => Portrait ? Rect(220, 1330, 460, 82) : Rect(730, 1040, 460, 80);
    public Rectangle SettingControl(int row, int segment = 0, int segments = 1) => Portrait
        ? Rect(80 + 740 * segment / segments, 440 + row * 250, 740 / segments, 96)
        : Rect(1050 + 700 * segment / segments, 335 + row * 230, 700 / segments, 100);
}
