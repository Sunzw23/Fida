using Fida.Logic;
using Microsoft.Xna.Framework;

namespace Fida.UI.Campaign
{
    /// <summary>Short, UI-only descriptions and color families for the loadout browser.</summary>
    internal static class CampaignSkillPresentation
    {
        public static (string family, string detail) Describe(SkillType skill) => skill switch
        {
            SkillType.PlusAttack => ("OFFENSE", "Before attacking, add 1 damage."),
            SkillType.DoubleAttack => ("OFFENSE", "Before attacking, double the damage."),
            SkillType.PlusDefence => ("DEFENSE", "Reduce incoming damage by 1. Minimum 1."),
            SkillType.DoubleDefence => ("DEFENSE", "Halve incoming damage. Minimum 1."),
            SkillType.DoubleHealth => ("VITALITY", "Start with double health."),
            SkillType.TripleHealth => ("VITALITY", "Start with triple health."),
            SkillType.Xue => ("VITALITY", "Heal for the damage you deal."),
            SkillType.Tong => ("CONTROL", "Once per turn, copy the result to both numbers."),
            SkillType.Mo => ("COUNTER", "After damage, retaliate with true damage."),
            SkillType.Xian => ("CONTROL", "After attacking, set one number to 0-9."),
            SkillType.Shen => ("CONTROL", "After damage, set one number to 0-9."),
            SkillType.Di => ("CONTROL", "At turn start, set one number to 0-9."),
            SkillType.Bian => ("OFFENSE", "Choose 1-5 damage when attacking."),
            SkillType.Long => ("OFFENSE", "Use the sum of your numbers as damage."),
            SkillType.Shi => ("DEFENSE", "Take at most one attack each turn."),
            SkillType.SuperGod => ("CONTROL", "After damage, set both numbers to 0-9."),
            SkillType.SuperImmortal => ("CONTROL", "After attacking, set both numbers to 0-9."),
            SkillType.SuperEmperor => ("CONTROL", "At turn start, set both numbers to 0-9."),
            _ => ("MODULE", "A special battle effect.")
        };

        public static Color Accent(SkillType skill) => Describe(skill).family switch
        {
            "OFFENSE" => UITheme.AccentViolet,
            "DEFENSE" => UITheme.AccentBlue,
            "VITALITY" => UITheme.AccentGreen,
            "COUNTER" => UITheme.GoldBright,
            _ => UITheme.Gold
        };
    }
}
