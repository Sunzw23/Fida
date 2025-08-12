using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    



    public class GameConfig
    {
        public static List<Skill> GameStartSkills => [Skill.TripleHealth];
        public static List<Skill> TurnStartSkills => [Skill.Tong, Skill.Di];
        public static List<Skill> OrderingAttackSkills => [Skill.Long, Skill.DoubleAttack];
    }

    public class GameText
    {
        public static string SkillName(Skill skill)
        {
            return skill switch
            {
                Skill.DoubleAttack => "双倍攻击",
                Skill.DoubleDefence => "双倍防御",
                Skill.TripleHealth => "三倍生命",
                Skill.Shen => "神",
                Skill.Xian => "仙",
                Skill.Mo => "魔",
                Skill.Gui => "鬼",
                Skill.Tong => "同",
                Skill.Di => "帝",
                Skill.Long => "龙",
                _ => throw new ArgumentOutOfRangeException(nameof(skill), skill, null)
            };
        }

        public static string GameStart => "游戏开始!";
        public static string TurnStart(string name) => $"{name}的回合.";

        public static string SkillActivated(string name, Skill skill) => $"{name}触发技能: {SkillName(skill)}.";
        
        public static string ShieldBreak(string name) => $"{name} 的护盾被打破, 伤害被抵消!";
        public static string Attacked(string name, int damageValue) => $"{name}  被攻击, 失去 {damageValue} 点生命值.";
        public static string GameEndWithWinner(string name) => $"游戏结束, {name}获胜!";
        public static string GameEndWithDraw => "游戏结束, 平局!";
        public static string GameEnd => "游戏结束!";

        public static string ChooseFirstNumberIndex(string name) => $"{name} 请选择第一个数字:";
        public static string ChooseSecondNumberIndex(string name) => $"{name} 请选择第二个数字:";



        public static string ChooseTargetForDamaging => $"请选择攻击目标:";
        public static string ChooseTargetForHealing => $"请选择治疗目标:";


        public static string SkillActivatedUseIf(string name, Skill skill) => $"{name}触发{SkillName(skill)}. 请选择是否使用";
        public static string SkillActivatedNumberTurnFrom(string name, Skill skill) => $"{name}触发 {SkillName(skill)}. 请选择一个数字进行变化.  ";
        public static string SkillActivatedNumberTurnInto(string name, Skill skill) => $"{name}触发 {SkillName(skill)}. 请选择变成的数字. ";

    }
}
