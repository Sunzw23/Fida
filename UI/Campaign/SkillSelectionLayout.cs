using System;
using Microsoft.Xna.Framework;

namespace Fida.UI.Campaign;

/// <summary>Shared drawing and hit geometry, with a stacked phone layout.</summary>
public sealed class SkillSelectionLayout
{
    public Rectangle Panel { get; }
    public Rectangle Header { get; }
    public Rectangle Back { get; }
    public Rectangle Tabs { get; }
    public Rectangle Grid { get; }
    public Rectangle Details { get; }
    public Rectangle Action { get; }
    public Rectangle Footer { get; }
    public Rectangle Chips { get; }
    /// <summary>Read-only party summary on the loadout screen; the companion screen owns the choice.</summary>
    public Rectangle Party { get; }
    public Rectangle Start { get; }
    public Rectangle Previous { get; }
    public Rectangle Next { get; }
    public bool Portrait { get; }
    public int Columns => Portrait ? 3 : 4;
    public int PageSize => Columns * 3;
    public float Scale { get; }

    public SkillSelectionLayout(int width, int height)
        : this(width, height, null, null) { }

    /// <summary>
    /// <paramref name="header"/> and <paramref name="back"/> carry the route screen's Gum-authored
    /// header geometry so both screens stay aligned. Callers that only need the grid/detail geometry
    /// (layout checks) can supply it directly instead of loading the Gum project.
    /// </summary>
    public SkillSelectionLayout(int width, int height, Rectangle? header, Rectangle? back)
    {
        Portrait = height > width;
        Scale = Portrait ? Math.Min(width / 1080f, height / 1920f) : UIDesktopLayout.Scale(width, height);
        if (!Portrait)
        {
            Rectangle R(int x, int y, int w, int h) => UIDesktopLayout.MapRectangle(new Rectangle(x, y, w, h), width, height);
            Panel = R(48, 30, 1824, 1140);
            if (header is { } sharedHeader && back is { } sharedBack)
            {
                Header = sharedHeader;
                Back = sharedBack;
            }
            else
            {
                var campaign = new CampaignMapLayout(width, height);
                Header = new Rectangle(campaign.Header.Left, campaign.Header.Top, R(150, 35, 1710, 85).Right - campaign.Header.Left, campaign.Header.Height);
                Back = campaign.Back;
            }
            Tabs = R(60, 155, 1160, 58);
            // The party strip takes the band under the grid; three rows of tiles stay large.
            Grid = R(60, 235, 1160, 700);
            Party = R(60, 940, 1160, 110);
            Details = R(1260, 185, 610, 670);
            Action = R(1282, 755, 566, 80);
            Footer = R(1260, 870, 610, 265);
            Chips = R(1260, 925, 610, 80);
            Start = R(1260, 1040, 610, 90);
            Previous = R(480, 1070, 62, 48);
            Next = R(740, 1070, 62, 48);
        }
        else
        {
            int pad = Math.Max(12, (int)(24 * Scale));
            int headerHeight = Math.Max(94, (int)(140 * Scale));
            int tabHeight = Math.Max(48, (int)(70 * Scale));
            int footerHeight = Math.Max(160, (int)(245 * Scale));
            Panel = new Rectangle(pad, pad, width - pad * 2, height - pad * 2);
            Back = new Rectangle(pad, pad, (int)(64 * Scale), (int)(64 * Scale));
            Header = new Rectangle(Back.Right + pad, pad, Panel.Width - Back.Width - pad, headerHeight);
            Tabs = new Rectangle(pad, pad + headerHeight, Panel.Width, tabHeight);
            Grid = new Rectangle(pad, Tabs.Bottom + pad / 2, Panel.Width, (int)(height * 0.36f));
            Previous = new Rectangle(width / 2 - (int)(130 * Scale), Grid.Bottom + pad / 2, (int)(60 * Scale), (int)(48 * Scale));
            Next = new Rectangle(width / 2 + (int)(70 * Scale), Previous.Y, Previous.Width, Previous.Height);
            Footer = new Rectangle(pad, height - pad - footerHeight, Panel.Width, footerHeight);
            int partyHeight = Math.Max(64, (int)(120 * Scale));
            Party = new Rectangle(pad, Previous.Bottom + pad / 2, Panel.Width, partyHeight);
            Details = new Rectangle(pad, Party.Bottom + pad / 2, Panel.Width,
                Math.Max(1, Footer.Top - Party.Bottom - pad * 2));
            Action = new Rectangle(Details.Left + pad, Details.Bottom - (int)(70 * Scale) - pad,
                Details.Width - pad * 2, (int)(70 * Scale));
            Start = new Rectangle(pad, Footer.Bottom - (int)(80 * Scale), Panel.Width, (int)(80 * Scale));
            Chips = new Rectangle(pad, Footer.Top + (int)(42 * Scale), Panel.Width, (int)(64 * Scale));
        }
    }
    public Rectangle Tile(int index)
    {
        int gap = Math.Max(6, (int)(18 * Scale));
        int width = (Grid.Width - (Columns - 1) * gap) / Columns;
        int height = (Grid.Height - 2 * gap) / 3;
        return new Rectangle(Grid.X + index % Columns * (width + gap), Grid.Y + index / Columns * (height + gap), width, height);
    }
    public Rectangle Tab(int index) => new(Tabs.Left + index * Tabs.Width / 4, Tabs.Top, Tabs.Width / 4, Tabs.Height);
}
