using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    public static class SkillManager
    {
        public static List<SkillType> GameStartSkills => [SkillType.TripleHealth];
        public static List<SkillType> TurnStartSkills => [SkillType.Tong, SkillType.Di];
        public static List<SkillType> OrderingAttackSkills => [SkillType.Long, SkillType.DoubleAttack];
        public static List<SkillType> OrderedAttackSkills => [SkillType.Xian];

        public static List<SkillType> DamagingSkills => [SkillType.Xue];
        public static List<SkillType> DamagedSkills => [SkillType.Shen, SkillType.Mo];
    }

    public enum SkillType
    {
        PlusAttack,
        PlusDefence,
        PlusHealth,
        DoubleAttack,
        DoubleDefence,
        TripleHealth,
        Shen,
        Xian,
        Mo,
        Gui,
        Xue,
        Tong,
        Di,
        Long,
        Huo,
        Bian,
        Shi,

    }

    public class Skill: IEquatable<Skill>
    {
        public SkillType Type { get; set; }
        public int Value;
        public bool IsGameStart { get; set; } = false;
        public bool IsPerRound { get; set; } = false;
        public bool IsPerTurn { get; set; } = false;
        public bool IsUnLimited { get; set; } = false;
        public bool IsActivated { get; set; } = false;

        public bool Equals(Skill other)
        {
            return Type == other.Type;
        }
    }



    public class SkillLibrary
    {
        public static Skill GetSkill(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.PlusAttack:
                    return new Skill { Type = SkillType.PlusAttack, Value = 1, IsUnLimited = true };
                case SkillType.PlusDefence:
                    return new Skill { Type = SkillType.PlusDefence, Value = 1, IsUnLimited = true };
                case SkillType.PlusHealth:
                    return new Skill { Type = SkillType.PlusHealth, Value = 1, IsGameStart = true };
                case SkillType.DoubleAttack:
                    return new Skill { Type = SkillType.DoubleAttack, Value = 2, IsUnLimited = true };
                case SkillType.DoubleDefence:
                    return new Skill { Type= SkillType.DoubleDefence, Value = 2, IsUnLimited = true }; 
                case SkillType.TripleHealth:
                    return new Skill { Type = SkillType.TripleHealth, Value = 2, IsGameStart = true };
                case SkillType.Shen:
                    return new Skill { Type = SkillType.Shen, Value = 2, IsPerTurn = true };
                case SkillType.Xian:
                    return new Skill { Type = SkillType.Xian, Value = 2, IsPerTurn = true };
                case SkillType.Mo:
                    return new Skill { Type = SkillType.Mo, Value = 2, IsUnLimited = true };
                case SkillType.Xue:
                    return new Skill { Type = SkillType.Xue, Value = 2, IsUnLimited = true };
                case SkillType.Tong:
                    return new Skill { Type = SkillType.Tong, Value = 2, IsUnLimited = true };
                case SkillType.Long:
                    return new Skill { Type = SkillType.Long, Value = 3, IsUnLimited = true };
                case SkillType.Huo:
                    return new Skill { Type = SkillType.Huo, Value = 2, IsUnLimited = true };
                case SkillType.Bian:
                    return new Skill { Type = SkillType.Bian, Value = 3, IsUnLimited = true };
                case SkillType.Shi:
                    return new Skill { Type = SkillType.Shi, Value = 2, IsUnLimited = true };
                default:
                    return null;
            }
        }

        

        
    }

}
