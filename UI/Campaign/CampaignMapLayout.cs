using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

/// <summary>Route browsing and the persistent detail action share these screen rectangles.</summary>
public sealed class CampaignMapLayout
{
    private readonly int _width, _height;
    public float Scale { get; }
    public Rectangle Map { get; }
    public Rectangle Details { get; }
    public Rectangle Action { get; }
    public Rectangle Header { get; }
    public Rectangle Back { get; }
    public bool Portrait { get; }
    public CampaignMapLayout(Viewport viewport) : this(viewport.Width, viewport.Height) { }
    public CampaignMapLayout(int width, int height)
    {
        _width = width; _height = height;
        Portrait = height > width;
        Scale = Math.Max(.01f, Portrait ? Math.Min(width / 900f, height / 1600f)
            : Math.Min(width / 1600f, height / 960f));
        Map = Element("Map"); Details = Element("Details"); Action = Element("Action");
        Header = Element("Header"); Back = Element("Back");
    }

    public Rectangle Element(string name)
    {
        Rectangle bounds = UIGumLayout.Bounds("CampaignRoute", name, _width, _height, Scale);
        // The header background spans the viewport; its content stays on the centered canvas.
        return name == "HeaderStrip"
            ? new Rectangle(0, 0, _width, bounds.Bottom)
            : bounds;
    }
}
