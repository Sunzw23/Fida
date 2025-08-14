using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    // 定义游戏阶段, 用于控制游戏流程
    public enum GamePhaseType
    {
        GameStart,              // 游戏开始
        HandleStack,            // 处理栈
        TurnStart,              // 回合开始
        ChooseNumberIndex,      // 选择数字的操作
        ApplyingEffect,         // 应用数字操作后的效果
        OrderingAttack,         // 发动攻击
        AttackFinished,         // 攻击完成
        TargetSelection,        // 如果效果需要, 选择攻击或治疗目标
        PlayerTurnEnd,          // 玩家回合结束
        Healing,                // 玩家进行治疗
        Damaging,              // 玩家进行攻击, 造成伤害
        HandleSkillDoubleAttack,      // 玩家使用技能: 双倍攻击
        HandleSkillShen,              // 玩家使用技能: 神
        HandleSkillMo,                // 玩家使用技能: 魔
        HandleSkillXian,              // 玩家使用技能: 仙
        HandleSkillTong,              // 玩家使用技能: 同变
        HandleSkillDi,                // 玩家使用技能: 帝
        HandleSkillLong,               // 玩家使用技能: 龙
    }

    public struct GamePhase
    {
        public GamePhaseType Type;
        public int SourcePlayerId { get; set; }
        public int TargetPlayerId { get; set; }
        public int SourceNumberIndex { get; set; }
        public int TargetNumberIndex { get; set; }
        public int DamageValue { get; set; } = 0;
        public int HealingValue { get; set; } = 0;
        public int NewNumber { get; set; }
        public int Stage { get; set; } = 0;
        public List<int> Numbers { get; set; } = [1, 1];

        public GamePhase()
        {
            Type = GamePhaseType.HandleStack;
        }
    }
}
