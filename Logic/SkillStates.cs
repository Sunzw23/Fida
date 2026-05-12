using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    #region skill states
    // 技能的状态类

    public class PlusAttackSkillState : GameState
    {
        private readonly int _originalDamage;
        public PlusAttackSkillState(GameManager gameManager, int sourcePlayerId, int originalDamage) : base(gameManager, sourcePlayerId)
        {
            _originalDamage = originalDamage;
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
            var apply = actions.Count > 0 && actions[0].IsApply;
            var newDamage = apply ? _originalDamage + 1 : _originalDamage;

            var newState = new OrderingAttackState(GameManager, SourcePlayerId, SourcePlayer.Numbers, newDamage);
            GameManager.TransitionTo(newState);
        }
    }

    public class DoubleAttackSkillState : GameState
    {
        private readonly int _originalDamage;
        public DoubleAttackSkillState(GameManager gameManager, int sourcePlayerId, int originalDamage) : base(gameManager, sourcePlayerId)
        {
            _originalDamage = originalDamage;
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
            var apply = actions.Count > 0 && actions[0].IsApply;
            var newDamage = apply ? _originalDamage * 2 : _originalDamage;

            // 创建一个新的OrderingAttackState实例，而不是修改弹出的实例
            var newState = new OrderingAttackState(GameManager, SourcePlayerId, SourcePlayer.Numbers, newDamage);
            GameManager.TransitionTo(newState);
        }
    }

    public class DoubleDefenceSkillState : GameState
    {
        private readonly int _originalDamage;
        private readonly int _targetPlayerId;
        public DoubleDefenceSkillState(GameManager gameManager, int sourcePlayerId, int targetPlayerId, int originalDamage) : base(gameManager, sourcePlayerId)
        {
            _originalDamage = originalDamage;
            _targetPlayerId = targetPlayerId;
        }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleDefence));
            int newDamage = _originalDamage / 2;
            if (newDamage == 0) newDamage = 1; // 最少造成1点伤害
            var newState = new DamagingState(GameManager, SourcePlayerId, _targetPlayerId, newDamage);
            GameManager.TransitionTo(newState);
        }
    }

    public class PlusDefenceSkillState : GameState
    {
        private readonly int _originalDamage;
        private readonly int _targetPlayerId;
        public PlusDefenceSkillState(GameManager gameManager, int sourcePlayerId, int targetPlayerId, int originalDamage) : base(gameManager, sourcePlayerId)
        {
            _originalDamage = originalDamage;
            _targetPlayerId = targetPlayerId;
        }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleDefence));
            int newDamage = _originalDamage - 1;
            if (newDamage == 0) newDamage = 1; // 最少造成1点伤害
            var newState = new DamagingState(GameManager, SourcePlayerId, _targetPlayerId, newDamage);
            GameManager.TransitionTo(newState);
        }
    }

    public class DoubleHealthSkillState : GameState
    {
        public DoubleHealthSkillState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleHealth));
            SourcePlayer.MaxHealth = SourcePlayer.MaxHealth * 2;
            SourcePlayer.Health = SourcePlayer.MaxHealth;
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
        public TripleHealthSkillState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.TripleHealth));
            SourcePlayer.MaxHealth = SourcePlayer.MaxHealth * 3;
            SourcePlayer.Health = SourcePlayer.MaxHealth;
            GameManager.OnSkillActivated(new SkillActivatedEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                SkillType = SkillType.TripleHealth,
                Message = GameManager.GameMessage
            });
            // 技能效果处理完毕，回到状态栈的下一个状态
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }



    public class LongSkillState : GameState
    {
        private int[] _numbers;
        private readonly int _originalDamage;
        public LongSkillState(GameManager gameManager, int sourcePlayerId, int[] numbers, int originalDamage) : base(gameManager, sourcePlayerId)
        {
            _numbers = numbers;
            _originalDamage = originalDamage;
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
            var apply = actions.Count > 0 && actions[0].IsApply;
            int newDamage = apply ? _numbers[0] + _numbers[1] : _originalDamage;

            var newState = new OrderingAttackState(GameManager, SourcePlayerId, _numbers, newDamage);
            GameManager.TransitionTo(newState);
        }
    }



    // 神
    public class ShenSkillState : GameState
    {
        public ShenSkillState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }

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
            GameManager.ChangeNumber(SourcePlayerId, numberIndexToChange, numberToApply);
            // 技能效果处理完毕，回到状态栈的下一个状态
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }



    // 仙
    public class XianSkillState : GameState
    {
        public XianSkillState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }

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
            GameManager.ChangeNumber(SourcePlayerId, numberIndexToChange, numberToApply);
            // 技能效果处理完毕，回到状态栈的下一个状态
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    // 血
    public class XueSkillState : GameState
    {
        private readonly int _damageDealt;
        public XueSkillState(GameManager gameManager, int sourcePlayerId, int damageDealt) : base(gameManager, sourcePlayerId)
        {
            _damageDealt = damageDealt;
        }

        protected override void Execute(List<GameAction> actions)
        {
            // 血技能直接触发治疗，无需玩家输入
            GameManager.TransitionTo(new HealingState(GameManager, SourcePlayerId, SourcePlayerId, _damageDealt));
        }
    }


    // 魔
    public class MoSkillState : GameState
    {
        private readonly int _damageDealt;
        private readonly int _damageReturned;
        public MoSkillState(GameManager gameManager, int sourcePlayerId, int damageDealt) : base(gameManager, sourcePlayerId)
        {
            _damageDealt = damageDealt;
            _damageReturned = _damageDealt - _damageDealt / 2;

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

            GameManager.TransitionTo(new TrueDamageState(GameManager, SourcePlayerId, actions[0].TargetPlayerId, _damageReturned));
        }
    }


    // 同

    public class TongSkillState : GameState
    {
        public TongSkillState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }

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
                GameManager.StateStack.Push(new ChooseNumberIndexState(GameManager, SourcePlayerId, 0, -1, true));
            }
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    // 变

    public class BianSkillState : GameState
    {

        private readonly int _originalDamage;
        public BianSkillState(GameManager gameManager, int sourcePlayerId, int originalDamage) : base(gameManager, sourcePlayerId)
        {
            _originalDamage = originalDamage;
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

            int newDamage = actions[0].Number;
            var newState = new OrderingAttackState(GameManager, SourcePlayerId, SourcePlayer.Numbers, newDamage);
            GameManager.TransitionTo(newState);
        }
    }




    #endregion
}
