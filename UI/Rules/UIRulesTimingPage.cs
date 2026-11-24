#nullable enable
using System;
using System.Collections.Generic;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>Reference page 2: the eight stages, with the discovered skills that fire at each.</summary>
public sealed class UIRulesTimingPage : UIControl
{
    private readonly ResourceManager _resources;
    private readonly RulesBook _book;

    public const int StageCount = 8;

    public UIRulesTimingPage(ResourceManager resources, RulesBook book) : base(-60)
    {
        _resources = resources;
        _book = book;
    }

    public static string StageName(int stage) => SkillCodexText.Stage((SkillTrigger)stage);

    /// <summary>Hit regions and order for the stage links; shared by the page and the builder.</summary>
    public static IReadOnlyList<(SkillType Skill, Rectangle Bounds)> Links(
        RulesBook book, UIRulesLayout layout, SpriteFont font)
    {
        var byStage = new List<SkillType>[StageCount];
        for (int i = 0; i < StageCount; i++) byStage[i] = new List<SkillType>();
        foreach (var skill in book.Entries)
        {
            int trigger = (int)SkillFactory.CreateSkill(skill).Trigger;
            if (trigger >= 0 && trigger < StageCount) byStage[trigger].Add(skill);
        }

        var links = new List<(SkillType, Rectangle)>();
        float lineStep = (layout.Portrait ? 26 : 30) * layout.Scale;
        float gap = (layout.Portrait ? 18 : 26) * layout.Scale;
        for (int stage = 0; stage < StageCount; stage++)
        {
            Rectangle area = layout.TimingLinks(stage);
            float x = area.Left, y = area.Top;
            foreach (var skill in byStage[stage])
            {
                float width = UIRulesText.Width(font, GameText.SkillName(skill), layout.Scale, 23);
                if (x > area.Left + 1 && x + width > area.Right)
                {
                    x = area.Left;
                    y += lineStep;
                }
                var bounds = new Rectangle((int)MathF.Round(x), (int)MathF.Round(y),
                    (int)MathF.Ceiling(width) + 2, (int)MathF.Ceiling(30 * layout.Scale));
                links.Add((skill, bounds));
                x += width + gap;
            }
        }
        return links;
    }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var layout = new UIRulesLayout(batch.GraphicsDevice.Viewport);
        UIPaper.Sheet(batch, layout.Sheet, UIPaper.Face, 4, 10);
        Rectangle inner = layout.Inner;
        float scale = layout.Scale;
        UIRulesText.Line(batch, _resources.CaptionFont, "TIMING · 03",
            new Rectangle(inner.Left, inner.Top, inner.Width, layout.D(26)), 24, scale, UIPaper.MutedInk);

        var populated = new HashSet<int>();
        foreach (var (skill, _) in Links(_book, layout, _resources.CaptionFont))
            populated.Add((int)SkillFactory.CreateSkill(skill).Trigger);

        for (int stage = 0; stage < StageCount; stage++)
        {
            Rectangle row = layout.TimingRow(stage);
            UIRulesPaint.Hairline(batch, _resources.Pixel, new Rectangle(row.Left, row.Bottom - 1, row.Width, 1));
            UIRulesText.Line(batch, _resources.Font, StageName(stage), layout.TimingLabel(stage),
                26, scale, UIPaper.Ink);
            if (!populated.Contains(stage))
                UIRulesText.Line(batch, _resources.Font, "—", layout.TimingLinks(stage), 22, scale,
                    UIPaper.MutedInk);
        }

        UIRulesText.Paragraph(batch, _resources.Font,
            "Skills resolve in the order shown within a stage.",
            layout.TimingFootnote, layout.Portrait ? 22 : 24, scale, UIPaper.MutedInk);
    }
}