using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    public static class GameText
    {
        public static string Rules => "规则如下\n" +
            " 1. 共有若干玩家, 游戏开始, 玩家按照顺序依次行动\n" +
            " 2. 玩家可以先选择自己的一个数字, \n" +
            " 然后选择自己或其他玩家的另一个数字\n" +
            " 然后将那个数字加到自己的数字之上. \n" +
            " 3. 数字改变后, 检查数字组合, 造成一定效果, 列表如下: \n" +
            " 7 => -1 \n" +
            " 8 => -1 \n" +
            " 9 => +1 \n" +
            " 1-6 => -2 \n" +
            " 5-5 => -3 \n" +
            " 6-6 => -5 \n" +
            " 0 => 防御一次攻击\n" +
            " 0-0 => +5\n\n" +
            "当生命值 恰好 归零时游戏结束. \n" +
            "祝你好运";

        public static string Skills => "       技能列表\n" +
            "1. 双倍攻击: 你的攻击伤害翻倍.\n" +
            "2. 双倍防御: 你受到的伤害减半.\n" +
            "3. 三倍生命: 你的生命值上限增加到三倍.\n" +
            "4. 神: 每回合一次, 受到伤害时, 可以改变一个数字.\n" +
            "5. 仙: 每回合一次, 进行攻击时, 可以改变一个数字.\n" +
            "6. 魔: 被攻击时, 可以返回一半伤害给任意对手. \n" +
            "7. 血: 攻击造成伤害后, 可以恢复造成伤害的血量. \n" +
            "8. 同: 每回合开始时, 如果你的数字是相同, 可以同时改变.\n" +
            "9. 帝: 每回合开始时, 可以选择改变一个数字.\n" +
            "10. 火: 发动攻击可以选择同时攻击所有目标. \n" +
            "11. 石: 每回合第一次收到攻击时, 不受到伤害.\n" +
            "12. 变: 进行攻击时可以把数值修改为1-5之间任何值\n" +
            "13. 龙: 进行攻击时可以把数值修改为自己的两个数字之和.\n";

            


        public static string SkillName(SkillType skill)
        {
            return skill switch
            {
                SkillType.DoubleAttack => "双倍攻击",
                SkillType.DoubleDefence => "双倍防御",
                SkillType.TripleHealth => "三倍生命",
                SkillType.Shen => "神",
                SkillType.Xian => "仙",
                SkillType.Mo => "魔",
                SkillType.Xue => "血",
                SkillType.Gui => "鬼",
                SkillType.Tong => "同",
                SkillType.Di => "帝",
                SkillType.Long => "龙",
                _ => throw new ArgumentOutOfRangeException(nameof(skill), skill, null)
            };
        }


        public static SkillType SkillTypeFromString(string skillName)
        {
            return skillName switch
            {
                "双倍攻击" => SkillType.DoubleAttack,
                "双倍防御" => SkillType.DoubleDefence,
                "三倍生命" => SkillType.TripleHealth,
                "神" => SkillType.Shen,
                "仙" => SkillType.Xian,
                "魔" => SkillType.Mo,
                "血" => SkillType.Xue,
                "鬼" => SkillType.Gui,
                "同" => SkillType.Tong,
                "帝" => SkillType.Di,
                "龙" => SkillType.Long,
                _ => throw new ArgumentException($"'{skillName}' is not a valid skill name.", nameof(skillName))
            };
        }


        public static string GameStart => "游戏开始!";
        public static string TurnStart(string name) => $"{name}的回合.";

        public static string SkillActivated(string name, SkillType skill) => $"{name}触发技能: {SkillName(skill)}.";

        public static string ShieldBreak(string name) => $"{name} 的护盾被打破, 伤害被抵消!";
        public static string Damaged(string name, int damageValue) => $"{name}  被攻击, 失去 {damageValue} 点生命值.";
        public static string Healed(string name, int healValue) => $"{name} 被治疗, 恢复 {healValue} 点生命值.";
        public static string TrueDamaged(string name, int damageValue) => $"{name} 受到真实伤害, 失去 {damageValue} 点生命值.";
        public static string GameEndWithWinner(string name) => $"游戏结束, {name}获胜!";
        public static string GameEndWithDraw => "游戏结束, 平局!";
        public static string GameEnd => "游戏结束!";

        public static string ChooseFirstNumberIndex(string name) => $"{name} 请选择第一个数字:";
        public static string ChooseSecondNumberIndex(string name) => $"{name} 请选择第二个数字:";



        public static string ChooseTargetForDamaging => $"请选择攻击目标:";
        public static string ChooseTargetForHealing => $"请选择治疗目标:";


        public static string SkillActivatedUseIf(string name, SkillType skill) => $"{name}触发{SkillName(skill)}. 请选择是否使用";
        public static string SkillActivatedNumberTurnFrom(string name, SkillType skill) => $"{name}触发 {SkillName(skill)}. 请选择一个数字进行变化.  ";
        public static string SkillActivatedNumberTurnInto(string name, SkillType skill) => $"{name}触发 {SkillName(skill)}. 请选择变成的数字. ";

        public static string SkillActivatedChooseTarget(string name, SkillType skill) => $"{name}触发 {SkillName(skill)}. 请选择一个目标发动.";

    }
}
