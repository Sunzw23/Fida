#nullable enable
using System.Collections.Generic;
using Fida.Logic;
using Microsoft.Xna.Framework;

namespace Fida.UI
{
    /// <summary>Single visual vocabulary for compact skill medallions.</summary>
    public static class UISkillIconCatalog
    {
        private static readonly IReadOnlyDictionary<SkillType, string> Glyphs =
            new Dictionary<SkillType, string>
            {
                [SkillType.PlusAttack] = "A+", [SkillType.PlusDefence] = "D+",
                [SkillType.DoubleHealth] = "H2", [SkillType.DoubleAttack] = "A2",
                [SkillType.DoubleDefence] = "D2", [SkillType.TripleHealth] = "H3",
                [SkillType.Shen] = "G", [SkillType.Xian] = "I", [SkillType.Mo] = "M",
                [SkillType.Seng] = "MN", [SkillType.Dao] = "DO",
                [SkillType.Xue] = "B", [SkillType.Tong] = "=", [SkillType.Di] = "E",
                [SkillType.Long] = "DR", [SkillType.Bian] = "TR", [SkillType.Shi] = "S",
                [SkillType.SuperGod] = "G+", [SkillType.SuperImmortal] = "I+",
                [SkillType.SuperEmperor] = "E+"
            };

        public static string Glyph(SkillType skill) =>
            Glyphs.TryGetValue(skill, out string? glyph) ? glyph : "?";

        public static Color Fill(Skill skill) => skill.Trigger switch
        {
            SkillTrigger.OrderingAttack or SkillTrigger.OrderedAttack or SkillTrigger.Damaging =>
                UITheme.AccentViolet,
            SkillTrigger.Defending or SkillTrigger.Damaged => UITheme.AccentBlue,
            SkillTrigger.GameStart => UITheme.Gold,
            SkillTrigger.TurnStart => UITheme.AccentGreen,
            _ => UITheme.Ink
        };
    }
}
