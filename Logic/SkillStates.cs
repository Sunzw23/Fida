using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    #region skill states
    // 技能的状态类 — 统一通过 SkillContext 接收和传递流水线数据

    public class PlusAttackSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public PlusAttackSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.DecideApply,
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.PlusAttack)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count > 0 && actions[0].IsApply)
                _ctx.Damage += 1;

            GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
        }
    }

    public class DoubleAttackSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public DoubleAttackSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.DecideApply,
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleAttack)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count > 0 && actions[0].IsApply)
                _ctx.Damage *= 2;

            GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
        }
    }

    public class DoubleDefenceSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public DoubleDefenceSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleDefence));
            int newDamage = _ctx.Damage / 2;
            if (newDamage == 0) newDamage = 1; // 最少造成1点伤害
            _ctx.Damage = newDamage;
            _ctx.SourcePlayerId = _ctx.PipelineOriginId;
            GameManager.TransitionTo(new PreDamageState(GameManager, _ctx));
        }
    }

    public class PlusDefenceSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public PlusDefenceSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.PlusDefence));
            int newDamage = _ctx.Damage - 1;
            if (newDamage == 0) newDamage = 1; // 最少造成1点伤害
            _ctx.Damage = newDamage;
            _ctx.SourcePlayerId = _ctx.PipelineOriginId;
            GameManager.TransitionTo(new PreDamageState(GameManager, _ctx));
        }
    }

    public class DoubleHealthSkillState : GameState
    {
        public DoubleHealthSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleHealth));
            SourcePlayer.MaxHealth = SourcePlayer.MaxHealth * 2;
            SourcePlayer.Health = SourcePlayer.MaxHealth;
            SourcePlayer.NotifyHealthChanged();
            GameManager.OnSkillActivated(new SkillActivatedEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                SkillType = SkillType.DoubleHealth,
                Message = GameManager.GameMessage
            });
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    public class TripleHealthSkillState : GameState
    {
        public TripleHealthSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.TripleHealth));
            SourcePlayer.MaxHealth = SourcePlayer.MaxHealth * 3;
            SourcePlayer.Health = SourcePlayer.MaxHealth;
            SourcePlayer.NotifyHealthChanged();
            GameManager.OnSkillActivated(new SkillActivatedEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                SkillType = SkillType.TripleHealth,
                Message = GameManager.GameMessage
            });
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    // 龙
    public class LongSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public LongSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.DecideApply,
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.Long)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count > 0 && actions[0].IsApply && _ctx.Numbers != null)
                _ctx.Damage = _ctx.Numbers[0] + _ctx.Numbers[1];

            GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
        }
    }

    // 神
    public class ShenSkillState : GameState
    {
        public ShenSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.SkillActivatedNumberTurnFrom(SourcePlayer.Name, SkillType.Shen)
            });
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = Enumerable.Range(0, 10).ToList(),
                Prompt = GameText.SkillActivatedNumberTurnInto(SourcePlayer.Name, SkillType.Shen)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count < 2)
            {
                return;
            }

            var numberIndexToChange = actions[0].NumberIndex;
            var numberToApply = actions[1].Number;
            SourcePlayer.SetNumber(numberIndexToChange, numberToApply);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    // 仙
    public class XianSkillState : GameState
    {
        public XianSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.SkillActivatedNumberTurnFrom(SourcePlayer.Name, SkillType.Xian)
            });
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = [.. Enumerable.Range(0, 10)],
                Prompt = GameText.SkillActivatedNumberTurnInto(SourcePlayer.Name, SkillType.Xian)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count < 2)
            {
                return;
            }

            var numberIndexToChange = actions[0].NumberIndex;
            var numberToApply = actions[1].Number;
            SourcePlayer.SetNumber(numberIndexToChange, numberToApply);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    // 血
    public class XueSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public XueSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void Execute(List<GameAction> actions)
        {
            // 血技能直接触发治疗，无需玩家输入
            GameManager.TransitionTo(new HealingState(GameManager,
                new SkillContext(SourcePlayerId) { TargetPlayerId = SourcePlayerId, Healing = _ctx.Damage }));
        }
    }

    // 魔
    public class MoSkillState : GameState
    {
        private readonly SkillContext _ctx;
        private readonly int _damageReturned;
        public MoSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
            // 返回 ceil(damage/2) 伤害给攻击者（反射一半，向上取整）
            _damageReturned = (int)Math.Ceiling(_ctx.Damage / 2.0);
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseTarget,
                ValidTargets = GameManager.Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList(),
                Prompt = GameText.SkillActivatedChooseTarget(SourcePlayer.Name, SkillType.Mo) + GameText.MoSkillDamageReturn(_damageReturned)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count == 0)
            {
                return;
            }

            GameManager.TransitionTo(new TrueDamageState(GameManager,
                new SkillContext(SourcePlayerId) { TargetPlayerId = actions[0].TargetPlayerId, Damage = _damageReturned }));
        }
    }

    // 同
    public class TongSkillState : GameState
    {
        public TongSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.DecideApply,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.Tong)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count > 0 && actions[0].IsApply)
            {
                GameManager.StateStack.Pop();
                GameManager.StateStack.Push(new ChooseNumberIndexState(GameManager, SourcePlayerId, 0, -1));
            }
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    // 变
    public class BianSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public BianSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = [.. Enumerable.Range(1, 5)],
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.Bian)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count == 0)
            {
                return;
            }

            _ctx.Damage = actions[0].Number;
            GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
        }
    }

    public class DiSkillState : GameState
    {
        public DiSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }
        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.SkillActivatedNumberTurnFrom(SourcePlayer.Name, SkillType.Shen)
            });
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = Enumerable.Range(0, 10).ToList(),
                Prompt = GameText.SkillActivatedNumberTurnInto(SourcePlayer.Name, SkillType.Shen)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count < 2)
            {
                return;
            }

            var numberIndexToChange = actions[0].NumberIndex;
            var numberToApply = actions[1].Number;
            SourcePlayer.SetNumber(numberIndexToChange, numberToApply);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    #endregion
}
