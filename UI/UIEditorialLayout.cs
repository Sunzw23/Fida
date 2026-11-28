using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>Shared draw/input coordinates for the sparse menu screens, including portrait windows.</summary>
public sealed class UIEditorialLayout
{
    public bool Portrait { get; }
    public float Scale { get; }
    /// <summary>Viewport this layout was built for; type sizes are derived from it.</summary>
    public int Width => _width;
    public int Height => _height;
    private readonly Vector2 _offset;
    private readonly int _width, _height;
    public UIEditorialLayout(Viewport viewport) : this(viewport.Width, viewport.Height) { }
    public UIEditorialLayout(int width, int height)
    {
        _width = width; _height = height;
        Portrait = height > width;
        int designWidth = Portrait ? 900 : 1920, designHeight = Portrait ? 1600 : 1200;
        Scale = Math.Max(.01f, Math.Min(width / (float)designWidth, height / (float)designHeight));
        _offset = new Vector2((width - designWidth * Scale) / 2, (height - designHeight * Scale) / 2);
    }
    public Rectangle Element(string screen, string name) => UIGumLayout.Bounds(screen, name, _width, _height);
    public Rectangle Rect(int x, int y, int w, int h) => new(
        (int)Math.Round(_offset.X + x * Scale), (int)Math.Round(_offset.Y + y * Scale),
        Math.Max(1, (int)Math.Round(w * Scale)), Math.Max(1, (int)Math.Round(h * Scale)));
    // Main menu uses the artwork's 16:9 canvas independently of modal layouts.
    public Rectangle MenuButton(int row, bool connected)
    {
        if(Portrait) return Rect(80, connected ? 590 + row * 115 : 710 + row * 115, 740, 94);
        float scale=Math.Min(_width/1600f,_height/900f);
        float x=(_width-1600*scale)/2, y=(_height-900*scale)/2;
        int top=connected ? 220+row*83 : 225+row*110;
        int height=connected ? 70 : 92;
        return new Rectangle((int)Math.Round(x+76*scale),(int)Math.Round(y+top*scale),
            Math.Max(1,(int)Math.Round(530*scale)),Math.Max(1,(int)Math.Round(height*scale)));
    }
    public Rectangle Back => Rect(Portrait ? 45 : 70, 60, 72, 72);
    public Rectangle Header => Rect(Portrait ? 150 : 175, 60, 600, 80);
    public Rectangle Ready => Portrait ? Rect(100, 1200, 700, 100) : Rect(150, 1030, 920, 100);
    public Rectangle Leave => Portrait ? Rect(220, 1330, 460, 82) : Rect(1130, 1030, 640, 100);
    public Rectangle SettingControl(int row, int segment = 0, int segments = 1) => Portrait
        ? Rect(80 + 740 * segment / segments, 370 + row * 225, 740 / segments - 6, 90)
        : Rect(1050 + 700 * segment / segments, 315 + row * 145, 700 / segments - 6, 90);
}
