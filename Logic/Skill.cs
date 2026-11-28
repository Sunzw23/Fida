using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    /// <summary>
    /// 技能/动作上下文，携带流水线数据在状态之间传递。
    /// SourcePlayerId — 当前技能所属玩家（可在流水线中切换）；
    /// PipelineOriginId — 流水线发起者（锁定不变），用于在子阶段结束后还原。
    /// </summary>
    public class SkillContext
    {
        public int SourcePlayerId { get; set; }
        public int PipelineOriginId { get; init; }
        public int TargetPlayerId { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }
        public int[]? Numbers { get; init; }
        public IEnumerator<Skill>? SkillEnumerator { get; set; }
        public bool OrderedAttackSkillsQueued { get; set; }
        /// <summary>
        /// Set by attack-time skills that make this strike bypass the target's shield and
        /// <see cref="SkillTrigger.Defending"/> skills. It lives on the context, so it never
        /// needs to be persisted or networked: one attack, one context.
        /// </summary>
        public bool Pierce { get; set; }

        public SkillContext(int sourcePlayerId)
        {
            SourcePlayerId = sourcePlayerId;
            PipelineOriginId = sourcePlayerId;
        }

        public SkillContext(int sourcePlayerId, int damage, int healing = 0, int[]? numbers = null)
        {
            SourcePlayerId = sourcePlayerId;
            PipelineOriginId = sourcePlayerId;
            Damage = damage;
            Healing = healing;
            Numbers = numbers;
        }

        /// <summary>
        /// 基于当前上下文创建子上下文，切换 SourcePlayerId 但保留 PipelineOriginId
        /// </summary>
        public SkillContext Derive(int newSourcePlayerId)
        {
            return new SkillContext(newSourcePlayerId)
            {
                PipelineOriginId = this.PipelineOriginId,
                TargetPlayerId = this.TargetPlayerId,
                Damage = this.Damage,
                Healing = this.Healing,
                SkillEnumerator = this.SkillEnumerator,
                Pierce = this.Pierce,
            };
        }

        public void DisposeEnumerator()
        {
            SkillEnumerator?.Dispose();
            SkillEnumerator = null;
        }
    }

    public enum SkillTrigger
    {
        GameStart,
        TurnStart,
        OrderingAttack,
        OrderedAttack,
        Defending,
        Damaging,
        Damaged,
        AttackCompleted
    }

    public enum SkillType
    {
        PlusAttack,
        PlusDefence,
        DoubleHealth,
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
        SuperGod,
        SuperImmortal,
        SuperEmperor,
        Seng,
        Dao,
        Ci,
        Po,
        Zhen,
        Shatter,

        // 反向技能（drawback）：只登记了名字与费用，尚未接入流程。
        HalfAttack,
        HalfHealth,
        Bleed,
    }

    public enum SkillUsageLimit
    {
        Unlimited,
        PerRound,
        PerTurn,
        Limited
    }

    public sealed class SkillUsageState
    {
        public SkillUsageLimit UsageLimit { get; set; } = SkillUsageLimit.Unlimited;
        public int MaxUses { get; set; } = 1;
        public int RemainingUses { get; set; } = 1;
        public bool IsActivated { get; set; } = false;

        public bool CanUse() => UsageLimit == SkillUsageLimit.Unlimited || RemainingUses > 0;

        public void MarkUsed()
        {
            IsActivated = true;
            if (UsageLimit != SkillUsageLimit.Unlimited && RemainingUses > 0)
            {
                RemainingUses--;
            }
        }

        public void Reset()
        {
            IsActivated = false;
            RemainingUses = MaxUses;
        }

        public SkillUsageState() {}

        public SkillUsageState(SkillUsageLimit usageLimit)
        {
            UsageLimit = usageLimit;
            MaxUses = 1;
            RemainingUses = 1;
        }
        public SkillUsageState(int maxUses)
        {
            UsageLimit = SkillUsageLimit.Limited;
            MaxUses = maxUses;
            RemainingUses = maxUses;
        }
    }

    public abstract class Skill
    {
        public SkillType Type { get; set; }
        public int Value { get; set; } = 1;

        public SkillTrigger Trigger { get; set; }
        public SkillUsageState Usage { get; set; } = new();

        public bool CanUse(GameManager gameManager, Player player)
        {
            if (player == null || !player.IsAlive()) return false;
            if (!Usage.CanUse()) return false;
            return CheckCondition(gameManager, player);
        }

        public virtual bool CheckCondition(GameManager gameManager, Player player) => true;

        /// <summary>
        /// True for passives that turn every attack their owner makes into true damage. Read by the
        /// damage pipeline itself instead of a trigger, so such a skill needs no state: its
        /// <see cref="CreateState"/> returns null and every dispatch site already skips that.
        /// </summary>
        public virtual bool MakesAttacksTrue => false;

        /// <summary>
        /// True for passives that let their owner choose the number a broken shield leaves behind
        /// instead of always dropping to 1. Read by the shield stage, so no state is needed.
        /// </summary>
        public virtual bool ReforgesBrokenShield => false;

        public abstract GameState? CreateState(GameManager gameManager, SkillContext context);

    }
}
