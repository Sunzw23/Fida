using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    

    public enum SkillType
    {
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
        Long
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
                default:
                    return null;
            }
        }

        
    }

}
