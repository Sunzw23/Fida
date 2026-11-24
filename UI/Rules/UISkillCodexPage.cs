#nullable enable
using System;
using Fida.Campaign;
using Fida.Logic;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>
/// One page per discovered skill. Four blocks only: kicker, medal and name, the effect sentence,
/// one worked example, and a single footnote line.
/// </summary>
public sealed class UISkillCodexPage : UIControl
{
    private readonly ResourceManager _resources;
    private readonly SkillType _skill;
    private readonly bool _owned;
    private readonly bool _isNew;

    public UISkillCodexPage(ResourceManager resources, SkillType skill, bool owned, bool isNew) : base(-61)
    {
        _resources = resources;
        _skill = skill;
        _owned = owned;
        _isNew = isNew;
    }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var layout = new UIRulesLayout(batch.GraphicsDevice.Viewport);
        UIPaper.Sheet(batch, layout.Sheet, UIPaper.Face, 4, 10);
        float scale = layout.Scale;
        var entry = SkillCodexText.For(_skill);

        DrawWatermark(batch, layout);
        DrawKicker(batch, layout, entry, scale);
        DrawMedal(batch, layout, scale);

        Color ink = _owned ? UIPaper.Ink : UIPaper.MutedInk;
        UIRulesText.Line(batch, _resources.TitleFont, GameText.SkillName(_skill), layout.Name,
            64, scale, ink);
        UIRulesText.Paragraph(batch, _resources.Font, entry.Effect, layout.Effect,
            layout.Portrait ? 38 : 40, scale, _owned ? UIPaper.Ink : UIPaper.MutedInk);

        UIRulesPaint.Example(batch, _resources.Pixel, layout.Example);
        UIRulesText.Line(batch, _resources.Font, entry.Example,
            new Rectangle(layout.Example.Left + layout.D(32), layout.Example.Top,
                layout.Example.Width - layout.D(64), layout.Example.Height),
            layout.Portrait ? 30 : 32, scale, UIPaper.Ink);

        UIRulesText.Paragraph(batch, _resources.CaptionFont,
            SkillCodexText.Footnote(_skill, _owned), layout.Footnote,
            layout.Portrait ? 22 : 24, scale, UIPaper.MutedInk);
    }

    private void DrawKicker(SpriteBatch batch, UIRulesLayout layout, SkillCodexText.Entry entry, float scale)
    {
        var area = layout.Kicker;
        int dot = Math.Max(6, (int)MathF.Round(9 * scale));
        UIRulesPaint.FamilyDot(batch, _resources.Pixel,
            new Rectangle(area.Left, area.Center.Y - dot / 2, dot, dot), entry.Family);

        int offset = dot + Math.Max(8, (int)MathF.Round(14 * scale));
        string kicker = $"{SkillCodexText.FamilyLabel(entry.Family)} · {CampaignSkillBalance.Cost(_skill)} SP";
        var text = new Rectangle(area.Left + offset, area.Top, area.Width - offset, area.Height);
        UIRulesText.Line(batch, _resources.CaptionFont, kicker, text, 24, scale, UIPaper.MutedInk);

        if (!_isNew) return;
        float width = UIRulesText.Width(_resources.CaptionFont, kicker, scale, 24);
        UIRulesText.Line(batch, _resources.CaptionFont, "· NEW",
            new Rectangle((int)(text.Left + width + 8 * scale), area.Top,
                (int)(120 * scale), area.Height),
            24, scale, UITheme.AccentBlue);
    }

    private void DrawMedal(SpriteBatch batch, UIRulesLayout layout, float scale)
    {
        UIPaper.Medal(batch, layout.Medal, new Color(222, 214, 198));
        int side = (int)(MathF.Min(layout.Medal.Width, layout.Medal.Height) * .46f);
        UISkillArt.Draw(batch, _skill,
            new Rectangle(layout.Medal.Center.X - side / 2, layout.Medal.Center.Y - side / 2, side, side),
            _owned ? UIPaper.Ink : UIPaper.MutedInk * .85f);
    }

    private void DrawWatermark(SpriteBatch batch, UIRulesLayout layout)
    {
        UISkillArt.Draw(batch, _skill, layout.Watermark, UIPaper.Ink * .05f);
    }
}