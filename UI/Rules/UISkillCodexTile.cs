#nullable enable
using System;
using Fida.Campaign;
using Fida.Logic;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>One discovered skill in the index: icon, name, cost. Filled when owned, faded when only seen.</summary>
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

        // The codex used to print the two-letter code ("A+", "DR", "TR") where an icon belongs.
        // The grids, the battle cards and the route rewards all draw the real skill mark, so the
        // index now does too instead of asking the player to learn a second, letter-only notation.
        int pad = Math.Max(10, (int)MathF.Round(22 * scale));
        int icon = Math.Max(18, (int)(Bounds.Height * .30f));
        var mark = new Rectangle(Bounds.Left + pad, Bounds.Top + (int)(Bounds.Height * .10f), icon, icon);
        UISkillArt.Draw(batch, Skill, mark, ink * opacity);

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
