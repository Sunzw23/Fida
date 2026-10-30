using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

/// <summary>Route browsing and the persistent detail action share these screen rectangles.</summary>
public sealed class CampaignMapLayout
{
    public Rectangle Map { get; }
    public Rectangle Details { get; }
    public Rectangle Action { get; }
    public Rectangle Header { get; }
    public Rectangle Back { get; }
    public bool Portrait { get; }
    public CampaignMapLayout(Viewport viewport) : this(viewport.Width, viewport.Height) { }
    public CampaignMapLayout(int width, int height)
    {
        Portrait = height > width;
        int margin = Math.Max(18, width / 40), header = Math.Clamp(height / 8, 92, 140);
        int bottom = Math.Clamp(height / 10, 75, 100), gap = Math.Max(20, width / 60);
        var shared = new UIEditorialLayout(width, height);
        Header = shared.Header;
        Back = shared.Back;
        if (Portrait)
        {
            int mapHeight = Math.Max(120, (height - header - bottom) * 48 / 100);
            Map = new Rectangle(margin, header, width - margin * 2, mapHeight);
            Details = new Rectangle(margin, Map.Bottom + gap, Map.Width, Math.Max(100, height - bottom - Map.Bottom - gap));
        }
        else
        {
            int sidebar = Math.Clamp(width * 32 / 100, 280, 620);
            Map = new Rectangle(margin, header, width - margin * 2 - sidebar - gap, height - header - bottom);
            Details = new Rectangle(Map.Right + gap, header, sidebar, Map.Height);
        }
        int inset = Math.Clamp(Details.Width / 14, 18, 40), buttonHeight = Math.Clamp(height / 12, 54, 82);
        Action = new Rectangle(Details.Left + inset, Details.Bottom - buttonHeight - 20, Details.Width - inset * 2, buttonHeight);
    }
}
