#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>Two primitives the rules pages share: a hairline and a quiet inset band.</summary>
public static class UIRulesPaint
{
    public static void Hairline(SpriteBatch batch, Texture2D pixel, Rectangle area, float alpha = .16f)
    {
        if (area.Width <= 0) return;
        batch.Draw(pixel, new Rectangle(area.Left, area.Top, area.Width, System.Math.Max(1, area.Height)),
            UIPaper.Ink * alpha);
    }

    /// <summary>
    /// Rules between two hairlines instead of a filled card: the example stays paper, and a
    /// translucent sheet would pick up a grey backing from the material's depth pass.
    /// </summary>
    public static void Example(SpriteBatch batch, Texture2D pixel, Rectangle area)
    {
        Hairline(batch, pixel, new Rectangle(area.Left, area.Top, area.Width, 1), .22f);
        Hairline(batch, pixel, new Rectangle(area.Left, area.Bottom - 1, area.Width, 1), .22f);
    }

    public static void FamilyDot(SpriteBatch batch, Texture2D pixel, Rectangle area, SkillFamily family)
    {
        Color color = family switch
        {
            SkillFamily.Offense => UITheme.AccentViolet,
            SkillFamily.Defense => UITheme.AccentBlue,
            SkillFamily.Vitality => UITheme.AccentYellow,
            SkillFamily.Counter => UITheme.GoldBright,
            _ => UITheme.InkMuted
        };
        batch.Draw(pixel, area, color);
    }
}
