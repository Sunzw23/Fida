#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>Index page: the discovered skills as paper tiles, one page of slots at a time.</summary>
public sealed class UIRulesIndexPage : UIControl
{
    private readonly ResourceManager _resources;
    private readonly RulesBook _book;
    private readonly int _indexPage;
    private readonly int _pageCount;

    public int IndexPage => _indexPage;
    public int PageCount => _pageCount;

    public UIRulesIndexPage(ResourceManager resources, RulesBook book, int indexPage, int pageCount) : base(-61)
    {
        _resources = resources;
        _book = book;
        _indexPage = indexPage;
        _pageCount = pageCount;
    }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var layout = new UIRulesLayout(batch.GraphicsDevice.Viewport);
        UIPaper.Sheet(batch, layout.Sheet, UIPaper.Face, 4, 10);
        float scale = layout.Scale;

        UIRulesText.Line(batch, _resources.LabelFont, "SKILLS", layout.IndexTitle, 34, scale, UIPaper.Ink);
        UIRulesText.Line(batch, _resources.CaptionFont,
            _book.Entries.Count == 0 ? "none yet" : $"{_book.Entries.Count} of {_book.TotalSkills}",
            layout.IndexCount, 24, scale, UIPaper.MutedInk, rightAligned: true);

        if (_book.Entries.Count == 0)
        {
            Rectangle empty = layout.Inner;
            empty = new Rectangle(empty.Left, layout.IndexTileArea.Top, empty.Width, layout.D(240));
            UIRulesText.Paragraph(batch, _resources.Font,
                "Skills appear here once you have owned or faced them. Play a battle, or finish a campaign mission.",
                empty, 30, scale, UIPaper.MutedInk);
        }

        if (_pageCount > 1)
            UIRulesText.Line(batch, _resources.CaptionFont, $"{_indexPage + 1} / {_pageCount}",
                layout.IndexLabel, 22, scale, UIPaper.MutedInk, centered: true);
    }
}
