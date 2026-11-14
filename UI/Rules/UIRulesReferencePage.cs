#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>Reference pages 0 (a turn) and 1 (number effects). Page 2 lives in the timing page.</summary>
public sealed class UIRulesReferencePage : UIControl
{
    private readonly ResourceManager _resources;
    private readonly int _page;

    private static readonly (string Number, string Effect)[] Singles =
    {
        ("7 · 8", "Attack for 1 damage."),
        ("9", "Heal 1 health."),
        ("0", "Shield: blocks one attack.")
    };

    private static readonly (string Number, string Effect)[] Pairs =
    {
        ("1 + 6", "Attack for 2 damage."),
        ("5 + 5", "Attack for 3 damage."),
        ("6 + 6", "Attack for 5 damage."),
        ("0 + 0", "Heal 10 health.")
    };

    private static readonly (string Number, string Title, string Body)[] Steps =
    {
        ("1", "Choose", "Pick one of your numbers."),
        ("2", "Add", "Choose a number from any player and add it."),
        ("3", "Read the pair", "Your two numbers decide the effect."),
        ("4", "Resolve", "Choose a target, then skills fire in order.")
    };

    public UIRulesReferencePage(ResourceManager resources, int page) : base(-61 + page)
    {
        _resources = resources;
        _page = page;
    }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var layout = new UIRulesLayout(batch.GraphicsDevice.Viewport);
        UIPaper.Sheet(batch, layout.Sheet, UIPaper.Face, 4, 10);
        if (_page == 0) DrawTurn(batch, layout);
        else DrawNumbers(batch, layout);
    }

    private void DrawTurn(SpriteBatch batch, UIRulesLayout layout)
    {
        Rectangle inner = layout.Inner;
        bool portrait = layout.Portrait;
        float scale = layout.Scale;
        UIRulesText.Line(batch, _resources.CaptionFont, "TURN · 01",
            new Rectangle(inner.Left, inner.Top, inner.Width, layout.D(26)), 24, scale, UIPaper.MutedInk);

        int gap = layout.D(portrait ? 12 : 18);
        int height = layout.D(portrait ? 160 : 200);
        int top = inner.Top + layout.D(portrait ? 50 : 74);
        int columnWidth = (inner.Width - gap * 3) / 4;
        for (int i = 0; i < Steps.Length; i++)
        {
            Rectangle step = portrait
                ? new Rectangle(inner.Left, top + i * height, inner.Width, height - gap)
                : new Rectangle(inner.Left + i * (columnWidth + gap), top, columnWidth, height);
            DrawStep(batch, layout, step, Steps[i], portrait, scale);
        }

        int exampleTop = portrait ? top + Steps.Length * height + layout.D(10) : top + height + layout.D(36);
        var example = new Rectangle(inner.Left, exampleTop, inner.Width, layout.D(portrait ? 96 : 100));
        UIRulesPaint.Example(batch, _resources.Pixel, example);
        UIRulesText.Line(batch, _resources.Font, "1 and 1 → add 5 → 1 and 6 → pair attack, 2 damage",
            new Rectangle(example.Left + layout.D(32), example.Top, example.Width - layout.D(64), example.Height),
            portrait ? 26 : 32, scale, UIPaper.Ink);

        var footnote = new Rectangle(inner.Left, example.Bottom + layout.D(portrait ? 22 : 26),
            inner.Width, layout.D(portrait ? 80 : 46));
        UIRulesText.Paragraph(batch, _resources.Font,
            "After an effect resolves, both numbers return to 1. A 0 is a shield and blocks one attack.",
            footnote, portrait ? 22 : 24, scale, UIPaper.MutedInk);
    }

    private void DrawStep(SpriteBatch batch, UIRulesLayout layout, Rectangle area,
        (string Number, string Title, string Body) step, bool portrait, float scale)
    {
        UIRulesPaint.Hairline(batch, _resources.Pixel, new Rectangle(area.Left, area.Top, area.Width, 1));

        if (portrait)
        {
            UIRulesText.Line(batch, _resources.TitleFont, step.Number,
                new Rectangle(area.Left, area.Top + layout.D(12), layout.D(54), layout.D(46)), 40, scale, UITheme.AccentBlue);
            UIRulesText.Line(batch, _resources.LabelFont, step.Title,
                new Rectangle(area.Left + layout.D(66), area.Top + layout.D(10), area.Width - layout.D(66), layout.D(40)),
                30, scale, UIPaper.Ink);
            UIRulesText.Paragraph(batch, _resources.Font, step.Body,
                new Rectangle(area.Left + layout.D(66), area.Top + layout.D(58),
                    area.Width - layout.D(66), area.Height - layout.D(64)),
                26, scale, UIPaper.MutedInk);
            return;
        }

        UIRulesText.Line(batch, _resources.TitleFont, step.Number,
            new Rectangle(area.Left, area.Top + layout.D(12), layout.D(60), layout.D(54)), 42, scale, UITheme.AccentBlue);
        UIRulesText.Line(batch, _resources.LabelFont, step.Title,
            new Rectangle(area.Left, area.Top + layout.D(70), area.Width, layout.D(40)), 32, scale, UIPaper.Ink);
        UIRulesText.Paragraph(batch, _resources.Font, step.Body,
            new Rectangle(area.Left, area.Top + layout.D(116), area.Width, area.Height - layout.D(120)),
            28, scale, UIPaper.MutedInk);
    }

    private void DrawNumbers(SpriteBatch batch, UIRulesLayout layout)
    {
        Rectangle inner = layout.Inner;
        bool portrait = layout.Portrait;
        float scale = layout.Scale;
        UIRulesText.Line(batch, _resources.CaptionFont, "NUMBERS · 02",
            new Rectangle(inner.Left, inner.Top, inner.Width, layout.D(26)), 24, scale, UIPaper.MutedInk);

        Rectangle footnote;
        if (portrait)
        {
            int y = inner.Top + layout.D(70);
            y = DrawNumberColumn(batch, layout,
                new Rectangle(inner.Left, y, inner.Width, layout.D(200)), "SINGLE", Singles) + layout.D(44);
            y = DrawNumberColumn(batch, layout,
                new Rectangle(inner.Left, y, inner.Width, layout.D(240)), "PAIRS", Pairs);
            footnote = new Rectangle(inner.Left, y + layout.D(48), inner.Width, layout.D(90));
        }
        else
        {
            int top = inner.Top + layout.D(96);
            DrawNumberColumn(batch, layout,
                new Rectangle(inner.Left, top, layout.D(735), layout.D(280)), "SINGLE", Singles);
            DrawNumberColumn(batch, layout,
                new Rectangle(inner.Left + layout.D(815), top, layout.D(715), layout.D(280)), "PAIRS", Pairs);
            footnote = new Rectangle(inner.Left, inner.Top + layout.D(420), inner.Width, layout.D(50));
        }

        UIRulesText.Paragraph(batch, _resources.Font,
            "A shield blocks one attack and skips defence skills. After an effect resolves, both numbers return to 1.",
            footnote, portrait ? 22 : 24, scale, UIPaper.MutedInk);
    }

    private int DrawNumberColumn(SpriteBatch batch, UIRulesLayout layout, Rectangle area, string heading,
        (string Number, string Effect)[] rows)
    {
        float scale = layout.Scale;
        int rowHeight = layout.D(layout.Portrait ? 64 : 66);
        int numberWidth = layout.D(layout.Portrait ? 190 : 230);
        UIRulesText.Line(batch, _resources.CaptionFont, heading,
            new Rectangle(area.Left, area.Top, area.Width, layout.D(26)), 24, scale, UIPaper.MutedInk);

        for (int i = 0; i < rows.Length; i++)
        {
            int top = area.Top + layout.D(40) + i * rowHeight;
            UIRulesText.Line(batch, _resources.TitleFont, rows[i].Number,
                new Rectangle(area.Left, top, numberWidth, rowHeight - layout.D(12)), 34, scale, UIPaper.Ink);
            UIRulesText.Line(batch, _resources.Font, rows[i].Effect,
                new Rectangle(area.Left + numberWidth, top, area.Width - numberWidth, rowHeight - layout.D(12)),
                layout.Portrait ? 25 : 27, scale, UIPaper.MutedInk);
            UIRulesPaint.Hairline(batch, _resources.Pixel,
                new Rectangle(area.Left, top + rowHeight - 1, area.Width, 1));
        }

        return area.Top + layout.D(40) + rows.Length * rowHeight;
    }
}