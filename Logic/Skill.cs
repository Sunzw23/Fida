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
        public int[] Numbers { get; init; }
        public IEnumerator<Skill> SkillEnumerator { get; set; }

        public SkillContext(int sourcePlayerId)
        {
            SourcePlayerId = sourcePlayerId;
            PipelineOriginId = sourcePlayerId;
        }

        public SkillContext(int sourcePlayerId, int damage, int healing = 0, int[] numbers = null)
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
        Damaged
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
        public SkillUsageState Usage { get; set; }

        public bool CanUse(GameManager gameManager, Player player)
        {
            if (!Usage.CanUse()) return false;
            return CheckCondition(gameManager, player);
        }

        public virtual bool CheckCondition(GameManager gameManager, Player player) => true;

        public abstract GameState CreateState(GameManager gameManager, SkillContext context);

    }


    public class PlusAttackSkill : Skill
    {
        public PlusAttackSkill()
        {
            Type = SkillType.PlusAttack;
            Value = 1;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public SkillUsageLimit SkillUsageLimit { get; }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new PlusAttackSkillState(gameManager, context);
        }
    }

    public class DoubleAttackSkill : Skill
    {
        public DoubleAttackSkill()
        {
            Type = SkillType.DoubleAttack;
            Value = 1;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new DoubleAttackSkillState(gameManager, context);
        }
    }

    public class PlusDefenceSkill : Skill
    {
        public PlusDefenceSkill()
        {
            Type = SkillType.PlusDefence;
            Value = 1;
            Trigger = SkillTrigger.Defending;
            Usage = new SkillUsageState();
        }
        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new PlusDefenceSkillState(gameManager, context);
        }
    }

    public class DoubleDefenceSkill : Skill
    {
        public DoubleDefenceSkill()
        {
            Type = SkillType.DoubleDefence;
            Value = 1;
            Trigger = SkillTrigger.Defending;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new DoubleDefenceSkillState(gameManager, context);
        }
    }

    public class DoubleHealthSkill : Skill
    {
        public DoubleHealthSkill()
        {
            Type = SkillType.DoubleHealth;
            Value = 1;
            Trigger = SkillTrigger.GameStart;
            Usage = new SkillUsageState(maxUses: 1);
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new DoubleHealthSkillState(gameManager, context);
        }
    }

    public class TripleHealthSkill : Skill
    {
        public TripleHealthSkill()
        {
            Type = SkillType.TripleHealth;
            Value = 1;
            Trigger = SkillTrigger.GameStart;
            Usage = new SkillUsageState(maxUses: 1);
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new TripleHealthSkillState(gameManager, context);
        }
    }

    public class ShenSkill : Skill
    {
        public ShenSkill()
        {
            Type = SkillType.Shen;
            Value = 2;
            Trigger = SkillTrigger.Damaged;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new ShenSkillState(gameManager, context);
        }
    }

    public class XianSkill : Skill
    {
        public XianSkill()
        {
            Type = SkillType.Xian;
            Value = 1;
            Trigger = SkillTrigger.OrderedAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new XianSkillState(gameManager, context);
        }
    }

    public class MoSkill : Skill
    {
        public MoSkill()
        {
            Type = SkillType.Mo;
            Value = 1;
            Trigger = SkillTrigger.Damaged;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new MoSkillState(gameManager, context);
        }
    }

    public class XueSkill : Skill
    {
        public XueSkill()
        {
            Type = SkillType.Xue;
            Value = 1;
            Trigger = SkillTrigger.Damaging;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new XueSkillState(gameManager, context);
        }
    }

    public class TongSkill : Skill
    {
        public TongSkill()
        {
            Type = SkillType.Tong;
            Value = 2;
            Trigger = SkillTrigger.TurnStart;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new TongSkillState(gameManager, context);
        }
    }

    public class BianSkill : Skill
    {
        public BianSkill()
        {
            Type = SkillType.Bian;
            Value = 3;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new BianSkillState(gameManager, context);
        }
    }

    public class LongSkill : Skill
    {
        public LongSkill()
        {
            Type = SkillType.Long;
            Value = 3;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new LongSkillState(gameManager, context);
        }
    }

    public class DiSkill : Skill
    {
        public DiSkill()
        {
            Type = SkillType.Di;
            Value = 2;
            Trigger = SkillTrigger.TurnStart;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new DiSkillState(gameManager, context);
        }
    }







}
