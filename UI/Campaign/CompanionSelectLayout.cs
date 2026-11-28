using System;
using Microsoft.Xna.Framework;

namespace Fida.UI.Campaign;

/// <summary>
/// Geometry for the companion preparation screen. It mirrors
/// <see cref="SkillSelectionLayout"/> so the route, companion and loadout screens keep one
/// header/back affordance, and it pages the roster grid instead of assuming a fixed roster
/// size: cards per page come from <see cref="Columns"/> and <see cref="Rows"/>.
/// </summary>
public sealed class CompanionSelectLayout
{
    private readonly int _width, _height;
    private readonly int _cardCount;
    public Rectangle Panel { get; }
    public Rectangle Header { get; }
    public Rectangle Back { get; }
    public Rectangle Roster { get; }
    public Rectangle Party { get; }
    public Rectangle Previous { get; }
    public Rectangle Next { get; }
    public Rectangle Details { get; }
    public Rectangle Continue { get; }
    public Rectangle Footer { get; }
    public bool Portrait { get; }
    public float Scale { get; }

    public int Columns => Portrait ? 2 : _cardCount is > 0 and <= 2 ? 2 : 4;
    public int Rows => 2;
    public int PageSize => Columns * Rows;

    public CompanionSelectLayout(int width, int height)
        : this(width, height, null, null, -1) { }

    /// <summary>
    /// <paramref name="header"/> and <paramref name="back"/> carry the Gum-authored campaign
    /// header geometry; layout tests can pass it directly instead of loading the Gum project.
    /// <paramref name="cardCount"/> centres the cards when the roster does not fill the page;
    /// a negative value reserves the full grid. <paramref name="bottomInset"/> is the device's
    /// system-bar height, so the continue action never lands under the gesture bar.
    /// </summary>
    public CompanionSelectLayout(int width, int height, Rectangle? header, Rectangle? back,
        int cardCount = -1, int bottomInset = 0)
    {
        _width = width;
        _height = height;
        _cardCount = cardCount;
        Portrait = height > width;
        Scale = Portrait ? Math.Min(width / 1080f, height / 1920f) : UIDesktopLayout.Scale(width, height);
        if (!Portrait)
        {
            Rectangle R(int x, int y, int w, int h) =>
                UIDesktopLayout.MapRectangle(new Rectangle(x, y, w, h), width, height);
            Panel = R(48, 30, 1824, 1140);
            if (header is { } sharedHeader && back is { } sharedBack)
            {
                Header = sharedHeader;
                Back = sharedBack;
            }
            else
            {
                var campaign = new CampaignMapLayout(width, height);
                Header = new Rectangle(campaign.Header.Left, campaign.Header.Top,
                    R(150, 35, 1710, 85).Right - campaign.Header.Left, campaign.Header.Height);
                Back = campaign.Back;
            }
            Roster = R(60, 175, 1160, 730);
            Party = R(60, 925, 1160, 110);
            Previous = R(1010, 946, 62, 48);
            Next = R(1096, 946, 62, 48);
            Details = R(1260, 175, 610, 690);
            Continue = R(1260, 890, 610, 90);
            Footer = R(1260, 1000, 610, 140);
            // The roster band is two rows tall but a four-person roster only uses one, so the
            // cards sit centred with a hole underneath. Pull the party bar up to the card row;
            // the remaining air then sits below the group instead of splitting it.
            int gap = Math.Max(6, (int)(18 * Scale));
            int columns = Columns;
            int cardWidth = (Roster.Width - (columns - 1) * gap) / columns;
            int rows = UsedRows(columns);
            int cardHeight = CardHeight(cardWidth, rows, gap);
            int cardsBottom = Roster.Center.Y + (rows * cardHeight + (rows - 1) * gap) / 2;
            int shift = cardsBottom + gap - Party.Top;
            if (shift < 0)
            {
                Party = new Rectangle(Party.X, Party.Y + shift, Party.Width, Party.Height);
                Previous = new Rectangle(Previous.X, Previous.Y + shift, Previous.Width, Previous.Height);
                Next = new Rectangle(Next.X, Next.Y + shift, Next.Width, Next.Height);
            }
            return;
        }

        int pad = Math.Max(12, (int)(24 * Scale));
        int headerHeight = Math.Max(94, (int)(140 * Scale));
        int footerHeight = Math.Max(100, (int)(110 * Scale));
        // Keep every action clear of the system gesture bar / navigation strip.
        int safeBottom = Math.Max(0, height - Math.Max(0, bottomInset));
        Panel = new Rectangle(pad, pad, width - pad * 2, Math.Max(1, safeBottom - pad * 2));
        Back = new Rectangle(pad, pad, Math.Max(40, (int)(64 * Scale)), Math.Max(40, (int)(64 * Scale)));
        Header = new Rectangle(Back.Right + pad, pad, Panel.Width - Back.Width - pad, headerHeight);
        // No separate party band: the cards themselves change colour when invited, so the
        // roster takes the space and the detail sheet sits above the continue footer.
        Party = Rectangle.Empty;
        Footer = new Rectangle(pad, safeBottom - pad - footerHeight, Panel.Width, footerHeight);
        Continue = new Rectangle(pad, Footer.Bottom - Math.Max(52, (int)(80 * Scale)), Panel.Width,
            Math.Max(52, (int)(80 * Scale)));
        int detailsHeight = Math.Clamp((int)(Panel.Width * .52f), 400, 560);
        Details = new Rectangle(pad, Footer.Top - pad / 2 - detailsHeight, Panel.Width, detailsHeight);
        // The pager only exists when the roster actually needs a second page; it never
        // steals space on the common four-card screen.
        bool paged = _cardCount > PageSize;
        int pagerHeight = paged ? Math.Max(36, (int)(44 * Scale)) : 0;
        int detailsTop = Details.Top - (paged ? pagerHeight + pad / 2 : 0);
        int rosterTop = pad + headerHeight + pad / 2;
        Roster = new Rectangle(pad, rosterTop, Panel.Width,
            Math.Max(1, detailsTop - pad / 2 - rosterTop));
        Previous = paged
            ? new Rectangle(width / 2 - (int)(60 * Scale), Details.Top - pad / 2 - pagerHeight,
                Math.Max(28, (int)(48 * Scale)), pagerHeight)
            : Rectangle.Empty;
        Next = paged
            ? new Rectangle(width / 2 + (int)(12 * Scale), Previous.Y, Previous.Width, Previous.Height)
            : Rectangle.Empty;
    }

    /// <summary>Card rectangle for a page-local index.</summary>
    public Rectangle Card(int index)
    {
        int gap = Math.Max(6, (int)(18 * Scale));
        int columns = Columns;
        int width = (Roster.Width - (columns - 1) * gap) / columns;
        int rows = UsedRows(columns);
        int height = CardHeight(width, rows, gap);
        int usedColumns = _cardCount < 0 ? columns : Math.Clamp(_cardCount, 1, columns);
        int blockWidth = usedColumns * width + (usedColumns - 1) * gap;
        int left = Roster.X + Math.Max(0, (Roster.Width - blockWidth) / 2);
        int block = rows * height + (rows - 1) * gap;
        int top = Roster.Y + Math.Max(0, (Roster.Height - block) / 2);
        return new Rectangle(left + index % columns * (width + gap),
            top + index / columns * (height + gap), width, height);
    }

    private int UsedRows(int columns) => _cardCount < 0
        ? Rows
        : Math.Clamp((_cardCount + columns - 1) / columns, 1, Rows);

    /// <summary>
    /// A short roster keeps its cards large: the used rows may grow toward a poster
    /// proportion, which keeps one or two companions from looking lost in the grid band.
    /// A full page falls back to the equal grid.
    /// </summary>
    private int CardHeight(int width, int rows, int gap)
    {
        int gridHeight = (Roster.Height - (rows - 1) * gap) / rows;
        if (_cardCount < 0) return gridHeight;
        float proportion = Portrait
            ? _cardCount switch { 1 => 1.55f, 2 => 1.25f, _ => 1.05f }
            : 1.25f;
        int preferred = (int)(width * proportion);
        return Math.Min(gridHeight, Math.Max(64, preferred));
    }

    public Rectangle CardArt(int index)
    {
        Rectangle card = Card(index);
        Rectangle name = CardName(index);
        int pad = Math.Max(6, (int)(16 * Scale));
        return new Rectangle(card.Left + pad, card.Top + pad, card.Width - pad * 2,
            Math.Max(1, name.Top - card.Top - pad * 2));
    }

    public Rectangle CardName(int index)
    {
        Rectangle card = Card(index);
        int pad = Math.Max(6, (int)(16 * Scale));
        int nameHeight = Math.Max(18, (int)LineHeight(UITextRole.Name));
        int captionHeight = Math.Max(14, (int)LineHeight(UITextRole.Small));
        return new Rectangle(card.Left + pad, card.Bottom - pad - captionHeight - nameHeight,
            card.Width - pad * 2, nameHeight);
    }

    public Rectangle CardCaption(int index)
    {
        Rectangle card = Card(index);
        int pad = Math.Max(6, (int)(16 * Scale));
        int captionHeight = Math.Max(14, (int)LineHeight(UITextRole.Small));
        return new Rectangle(card.Left + pad, card.Bottom - pad - captionHeight,
            card.Width - pad * 2, captionHeight);
    }

    /// <summary>Large art inside the detail sheet: above the text on desktop, beside it on phones.</summary>
    public Rectangle DetailArt()
    {
        int pad = Math.Max(12, (int)(22 * Scale));
        if (Portrait)
        {
            // A square bust takes about a third of the sheet; the copy keeps the rest.
            int side = Math.Min(Details.Height - pad * 2, (int)(Details.Width * 0.36f));
            side = Math.Max(1, side);
            return new Rectangle(Details.Left + pad, Details.Center.Y - side / 2, side, side);
        }
        int height = (int)(Details.Height * 0.52f);
        return new Rectangle(Details.Left + pad, Details.Top + pad, Details.Width - pad * 2,
            Math.Max(1, height));
    }

    public Rectangle DetailText()
    {
        int pad = Math.Max(12, (int)(22 * Scale));
        Rectangle art = DetailArt();
        if (Portrait)
            // The copy column may run a little past the bust: the sheet has the room and
            // the description then fits three readable lines instead of two cramped ones.
            return new Rectangle(art.Right + pad, art.Top,
                Math.Max(1, Details.Right - pad - art.Right - pad),
                Math.Max(1, Details.Bottom - pad - art.Top));
        int top = art.Bottom + pad;
        return new Rectangle(Details.Left + pad, top, Details.Width - pad * 2,
            Math.Max(1, Details.Bottom - pad - top));
    }

    /// <summary>
    /// Matches the loadout screen's type scale for the same header and card sizes. The route
    /// canvas scale (1600x960) is computed directly so layout tests stay headless.
    /// </summary>
    public float LineHeight(UITextRole role) => Portrait
        ? UITypography.LineHeight(role, _width, _height) * (role == UITextRole.Heading ? 1f : 1.15f)
        : DesignHeight(role) * Math.Max(.01f, Math.Min(_width / 1600f, _height / 960f));

    private static float DesignHeight(UITextRole role) => role switch
    {
        UITextRole.Heading => 51f,
        UITextRole.Name => 40f,
        UITextRole.Body => 42f,
        UITextRole.Caption => 34f,
        _ => 30f
    };
}
