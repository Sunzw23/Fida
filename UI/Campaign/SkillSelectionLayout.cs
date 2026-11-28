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
        : this(width, height, null, null, 0) { }

    /// <summary>
    /// <paramref name="header"/> and <paramref name="back"/> carry the route screen's Gum-authored
    /// header geometry so both screens stay aligned. Callers that only need the grid/detail geometry
    /// (layout checks) can supply it directly instead of loading the Gum project.
    /// <paramref name="bottomInset"/> is the device's system-bar height, so the start action never
    /// lands under the gesture bar.
    /// </summary>
    public SkillSelectionLayout(int width, int height, Rectangle? header, Rectangle? back,
        int bottomInset = 0, bool hasParty = true)
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
            return;
        }

        int pad = Math.Max(12, (int)(24 * Scale));
        int headerHeight = Math.Max(94, (int)(132 * Scale));
        int tabHeight = Math.Max(48, (int)(62 * Scale));
        int pagerHeight = Math.Max(40, (int)(48 * Scale));
        int safeBottom = Math.Max(pad * 2, height - Math.Max(0, bottomInset));
        Panel = new Rectangle(pad, pad, width - pad * 2, Math.Max(1, safeBottom - pad * 2));
        Back = new Rectangle(pad, pad, (int)(64 * Scale), (int)(64 * Scale));
        Header = new Rectangle(Back.Right + pad, pad, Panel.Width - Back.Width - pad, headerHeight);
        Tabs = new Rectangle(pad, pad + headerHeight + pad / 2, Panel.Width, tabHeight);
        // The phone footer is one row: the party summary and the start ticket. The party
        // ticket is the "change party" affordance; there is no separate back button.
        int footerHeight = Math.Max(76, (int)(110 * Scale));
        Footer = new Rectangle(pad, safeBottom - pad - footerHeight, Panel.Width, footerHeight);
        if (hasParty)
        {
            int startWidth = Math.Max(210, (int)(Panel.Width * .36f));
            Start = new Rectangle(Footer.Right - startWidth, Footer.Top, startWidth, footerHeight);
            Party = new Rectangle(Footer.Left, Footer.Top,
                Math.Max(1, Start.Left - pad / 2 - Footer.Left), footerHeight);
        }
        else
        {
            // No companion step: the start ticket owns the whole row.
            Start = Footer;
            Party = Rectangle.Empty;
        }
        int chipsHeight = Math.Max(44, (int)(64 * Scale));
        int detailsHeight = Math.Clamp((int)(width * 0.54f), 380, 560);
        Details = new Rectangle(pad, Footer.Top - pad / 2 - detailsHeight, Panel.Width, detailsHeight);
        int actionHeight = Math.Max(50, (int)(62 * Scale));
        int actionWidth = Math.Max(150, (int)(190 * Scale));
        // The equip ticket sits in the sheet header, so it no longer competes with the
        // full-width start action at the foot of the page.
        Action = new Rectangle(Details.Right - pad - actionWidth, Details.Top + pad,
            actionWidth, actionHeight);
        Chips = new Rectangle(Details.Left + pad, Details.Bottom - pad - chipsHeight,
            Details.Width - pad * 2, chipsHeight);
        Previous = new Rectangle(width / 2 - (int)(130 * Scale), Details.Top - pad / 2 - pagerHeight,
            Math.Max(48, (int)(60 * Scale)), pagerHeight);
        Next = new Rectangle(width / 2 + (int)(70 * Scale), Previous.Y, Previous.Width, Previous.Height);
        int gridTop = Tabs.Bottom + pad / 2;
        Grid = new Rectangle(pad, gridTop, Panel.Width, Math.Max(1, Previous.Top - pad / 2 - gridTop));
    }
    /// <summary>Tile rectangle for a full page.</summary>
    public Rectangle Tile(int index) => TileInPage(index, -1);

    /// <summary>
    /// Tile rectangle for a page-local index. <paramref name="count"/> is the number of
    /// skills on the page; a partly filled page (a filtered category) centres its used rows
    /// instead of hanging them from the top of the grid.
    /// </summary>
    public Rectangle TileInPage(int index, int count)
    {
        int gap = Math.Max(6, (int)(18 * Scale));
        int width = (Grid.Width - (Columns - 1) * gap) / Columns;
        int height = (Grid.Height - 2 * gap) / 3;
        int items = count < 0 ? Columns * 3 : Math.Clamp(count, 1, Columns * 3);
        int rows = (items + Columns - 1) / Columns;
        int block = rows * height + Math.Max(0, rows - 1) * gap;
        int top = Grid.Y + Math.Max(0, (Grid.Height - block) / 2);
        return new Rectangle(Grid.X + index % Columns * (width + gap),
            top + index / Columns * (height + gap), width, height);
    }
    public Rectangle Tab(int index) => new(Tabs.Left + index * Tabs.Width / 4, Tabs.Top, Tabs.Width / 4, Tabs.Height);
}
