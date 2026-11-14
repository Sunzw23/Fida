#nullable enable
using System;
using Fida.Campaign;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>One discovered skill in the index: glyph, name, cost. Filled when owned, faded when only seen.</summary>
public sealed class UISkillCodexTile : UIButton
{
    private readonly ResourceManager _resources;

    public SkillType Skill { get; }
    public bool Owned { get; }
    public bool IsNew { get; }

    public UISkillCodexTile(int id, Rectangle bounds, SkillType skill, bool owned, bool isNew,
        ResourceManager resources)
        : base(id, bounds, GameText.SkillName(skill), resources.Pixel, resources.LabelFont)
    {
        _resources = resources;
        Skill = skill;
        Owned = owned;
        IsNew = isNew;
    }

    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        var layout = new UIRulesLayout(batch.GraphicsDevice.Viewport);
        float scale = layout.Scale;
        float opacity = Owned ? 1f : .74f;
        Color ink = Owned ? UIPaper.Ink : UIPaper.MutedInk;
        Color muted = Owned ? UIPaper.MutedInk : UIPaper.MutedInk * .8f;

        DrawPaperButton(batch, UIPaper.Face * opacity, 9);
        if (Owned)
        {
            int bar = Math.Max(2, (int)MathF.Round(4 * scale));
            batch.Draw(Pixel, new Rectangle(Bounds.Left, Bounds.Top + Bounds.Height / 5,
                bar, Math.Max(1, Bounds.Height * 3 / 5)), UITheme.AccentBlue);
        }

        int pad = Math.Max(10, (int)MathF.Round(22 * scale));
        var glyph = new Rectangle(Bounds.Left + pad, Bounds.Top + (int)(Bounds.Height * .12f),
            Math.Max(20, (int)(Bounds.Width * .16f)), (int)(Bounds.Height * .32f));
        UIRulesText.Line(batch, _resources.TitleFont, UISkillIconCatalog.Glyph(Skill), glyph,
            32, scale, ink * opacity);

        var name = new Rectangle(Bounds.Left + pad, Bounds.Top + (int)(Bounds.Height * .46f),
            Bounds.Width - pad * 2, (int)(Bounds.Height * .26f));
        UIRulesText.Line(batch, _resources.LabelFont, Text, name, 28, scale, ink * opacity);

        var cost = new Rectangle(Bounds.Left + pad, Bounds.Top + (int)(Bounds.Height * .74f),
            Bounds.Width - pad * 2, (int)(Bounds.Height * .18f));
        UIRulesText.Line(batch, _resources.CaptionFont,
            $"{CampaignSkillBalance.Cost(Skill)} SP", cost, 20, scale, muted * opacity);

        if (IsNew)
        {
            int dot = Math.Max(6, (int)MathF.Round(9 * scale));
            batch.Draw(Pixel, new Rectangle(Bounds.Right - dot - pad, Bounds.Top + pad, dot, dot),
                UITheme.AccentBlue);
        }
    }
}
