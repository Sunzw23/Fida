using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    public class ActionStep
    {
        public ActionType RequiredType { get; init; }
        public List<int> ValidTargets { get; init; } = [];
        public List<int> ValidNumbers { get; init; } = [];
        public string Prompt { get; init; } = string.Empty;
        public GameAction? Result { get; set; }
    }

    /// <summary>
    /// 抽象游戏状态基类
    /// </summary>
    public abstract class GameState
    {
        protected readonly GameManager GameManager;
        protected readonly int SourcePlayerId;
        protected readonly Player SourcePlayer;
        protected readonly List<ActionStep> Steps = [];
        protected int StepIndex;

        protected GameState(GameManager gameManager, int sourcePlayerId = -1)
        {
            GameManager = gameManager;
            SourcePlayerId = sourcePlayerId;
            if (sourcePlayerId != -1)
            {
                SourcePlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);
            }
        }

        /// <summary>
        /// 进入状态时执行的逻辑
        /// </summary>
        public virtual void Enter()
        {
            Steps.Clear();
            StepIndex = 0;
            BuildSteps();

            if (Steps.Count == 0)
            {
                Execute([]);
                return;
            }

            BroadcastCurrentStep();
        }

        /// <summary>
        /// 处理当前状态下的逻辑和玩家输入
        /// </summary>
        public virtual void Handle(GameAction action)
        {
            if (StepIndex >= Steps.Count) return;

            var step = Steps[StepIndex];
            if (action.Type != step.RequiredType) return;
            if (step.ValidTargets.Count > 0 && !step.ValidTargets.Contains(action.TargetPlayerId)) return;
            if (step.ValidNumbers.Count > 0 && !step.ValidNumbers.Contains(action.Number)) return;
            if (!ValidateStep(step, action)) return;

            step.Result = action;
            StepIndex++;

            if (StepIndex >= Steps.Count)
            {
                Execute(Steps.Select(s => s.Result ?? default).ToList());
                return;
            }

            BroadcastCurrentStep();
        }

        protected virtual void BuildSteps() { }

        protected virtual bool ValidateStep(ActionStep step, GameAction action) => true;

        protected virtual void Execute(List<GameAction> actions) { }

        protected void BroadcastCurrentStep()
        {
            if (StepIndex >= Steps.Count) return;

            var step = Steps[StepIndex];
            // 先更新消息，再触发事件
            // 确保 OnPlayerActionRequired 事件中传递的是最新的消息
            GameManager.SetGameMessage(step.Prompt);
            GameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                RequiredActionType = step.RequiredType,
                ValidTargets = step.ValidTargets,
                ValidNumbers = step.ValidNumbers,
                Message = GameManager.GameMessage
            });
        }
    }

    #region Concrete Game States

    /// <summary>
    /// 游戏开始状态
    /// </summary>
    public class GameStartState(GameManager gameManager) : GameState(gameManager)
    {
        public override void Enter()
        {
            GameManager.SetGameMessage(GameText.GameStart);
            GameManager.OnGameStarted(new GameStartEventArgs { Message = GameManager.GameMessage });

            // 游戏开始后，直接进入第一个玩家的回合开始阶段
            if (GameManager.Players.Count == 0 || GameManager.CurrentPlayerIndex == -1)
            {
                return;
            }

            var firstPlayer = GameManager.Players[GameManager.CurrentPlayerIndex];
            GameManager.StateStack.Push(new TurnStartState(GameManager, firstPlayer.PlayerId));

            foreach (var player in GameManager.Players)
            {
                foreach (var skill in player.Skills)
                {
                    if (skill.Trigger == SkillTrigger.GameStart && skill.CanUse(GameManager, player))
                    {
                        var playerId = player.PlayerId;
                        var state = skill.CreateState(GameManager, new SkillContext(playerId));
                        if (state != null)
                        {
                            GameManager.StateStack.Push(state);
                        }
                    }
                }
            }


            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    /// <summary>
    /// 处理状态栈的状态
    /// </summary>
    public class HandleStackState(GameManager gameManager) : GameState(gameManager)
    {
        public override void Enter()
        {
            if (GameManager.StateStack.Count > 0)
            {
                var nextState = GameManager.StateStack.Pop();
                GameManager.TransitionTo(nextState);
            }
            else
            {
                // 如果栈为空，通常意味着一个操作序列完成，进入回合结束
                GameManager.TransitionTo(new PlayerTurnEndState(GameManager, GameManager.GetCurrentPlayer().PlayerId));
            }
        }
    }


    /// <summary>
    /// 回合开始状态
    /// </summary>
    public class TurnStartState : GameState
    {

        public TurnStartState(GameManager gameManager, int sourcePlayerId, int skillCheckStage = 0) : base(gameManager, sourcePlayerId)
        {
        }

        public override void Enter()
        {
            // 只有在检查第一个技能时才显示回合开始消息
            GameManager.SetGameMessage(GameText.TurnStart(SourcePlayer.Name));
            GameManager.OnTurnStarted(new TurnStartEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                Message = GameManager.GameMessage
            });
            

            foreach (var player in GameManager.Players)
            {
                player.ResetPerTurnSkills();
            }

            // 整条回合主流程倒序入栈。
            GameManager.StateStack.Push(new ChooseNumberIndexState(GameManager, SourcePlayerId));

            foreach (var skill in SourcePlayer.Skills)
            {
                if (skill.Trigger == SkillTrigger.TurnStart && skill.CanUse(GameManager, SourcePlayer))
                {
                    var playerId = SourcePlayer.PlayerId;
                    var state = skill.CreateState(GameManager, new SkillContext(playerId));
                    if (state != null)
                    {
                        GameManager.StateStack.Push(state);
                    }
                }
            }


            // 所有回合开始技能检查完毕，进入选择数字阶段
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    /// <summary>
    /// 选择数字状态
    /// </summary>
    public class ChooseNumberIndexState : GameState
    {
        private readonly int _initialStage;
        private readonly int _initialFirstNumberIndex;
        private readonly bool _changeBothNumbers;

        public ChooseNumberIndexState(GameManager gameManager, int sourcePlayerId, int stage = 0, int firstNumberIndex = -1, bool changeBothNumbers = false) : base(gameManager, sourcePlayerId)
        {
            _initialStage = stage;
            _initialFirstNumberIndex = firstNumberIndex;
            _changeBothNumbers = changeBothNumbers;
        }

        public int Stage => StepIndex;

        /// <summary>
        /// 获取第一步已选择的数字索引，如果第一步尚未完成则返回 -1
        /// </summary>
        public int FirstStepNumberIndex => Steps.Count > 0 && Steps[0].Result is { } firstResult ? firstResult.NumberIndex : -1;

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.ChooseFirstNumberIndex(SourcePlayer.Name)
            });

            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = GameManager.Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList(),
                Prompt = GameText.ChooseSecondNumberIndex(SourcePlayer.Name)
            });

            StepIndex = _initialStage;
            if (StepIndex > 0 && _initialFirstNumberIndex >= 0)
            {
                Steps[0].Result = new GameAction
                {
                    Type = ActionType.ChooseNumberIndex,
                    TargetPlayerId = SourcePlayerId,
                    NumberIndex = _initialFirstNumberIndex
                };
            }
        }

        protected override bool ValidateStep(ActionStep step, GameAction action)
        {
            if (StepIndex != 1 || Steps[0].Result is not { } firstStepAction)
            {
                return true;
            }

            if (action.TargetPlayerId == SourcePlayerId && action.NumberIndex == firstStepAction.NumberIndex)
            {
                // 不能选择同一个数字，重置回第一步。
                Steps[0].Result = null;
                StepIndex = 0;
                BroadcastCurrentStep();
                return false;
            }

            return true;
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count < 2) return;

            var firstAction = actions[0];
            var secondAction = actions[1];
            var otherPlayerSelected = GameManager.Players.FirstOrDefault(p => p.PlayerId == secondAction.TargetPlayerId);
            if (otherPlayerSelected == null || !otherPlayerSelected.IsAlive())
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            var firstNumberIndex = firstAction.NumberIndex;
            var newCalculatedNumber = (SourcePlayer.Numbers[firstNumberIndex] + otherPlayerSelected.Numbers[secondAction.NumberIndex]) % 10;


            if (_changeBothNumbers)
            {
                GameManager.SetGameMessage(GameText.NumberChangedBothCheckEffect(SourcePlayer.Name, newCalculatedNumber));
                SourcePlayer.SetNumber(0, newCalculatedNumber);
                SourcePlayer.SetNumber(1, newCalculatedNumber);
            }
            else
            {
                GameManager.SetGameMessage(GameText.NumberChangedSingleCheckEffect(SourcePlayer.Name, SourcePlayer.Numbers[firstNumberIndex], newCalculatedNumber));
                SourcePlayer.SetNumber(firstNumberIndex, newCalculatedNumber);
            }
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            

            // 回合主流程已预先入栈，交还给栈调度。
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    /// <summary>
    /// 应用效果状态
    /// </summary>
    public class ApplyingEffectState : GameState
    {
        public ApplyingEffectState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }

        public override void Enter()
        {
            var numbers = SourcePlayer.Numbers;

            // 攻击 (数字变为 8 或 7)
            if (numbers[0] == 8 || numbers[0] == 7)
            {
                HandleAttackEffect(0, 1);
                return;
            }
            if (numbers[1] == 8 || numbers[1] == 7)
            {
                HandleAttackEffect(1, 1);
                return;
            }
            // 攻击 (数字变为 1 和 6)
            if ((numbers[0] == 1 && numbers[1] == 6) || (numbers[0] == 6 && numbers[1] == 1))
            {
                HandleAttackEffectBoth(2);
                return;
            }
            // 攻击 (数字变为 5 和 5)
            if (numbers[0] == 5 && numbers[1] == 5)
            {
                HandleAttackEffectBoth(3);
                return;
            }
            // 攻击 (数字变为 6 和 6)
            if (numbers[0] == 6 && numbers[1] == 6)
            {
                HandleAttackEffectBoth(5);
                return;
            }
            // 治愈 (数字变为 9)
            if (numbers[0] == 9)
            {
                HandleHealEffect(0, 1);
                return;
            }
            if (numbers[1] == 9)
            {
                HandleHealEffect(1, 1);
                return;
            }
            // 治愈 (两个数字都变为 0)
            if (numbers[0] == 0 && numbers[1] == 0)
            {
                HandleHealEffectBoth(10);
                return;
            }

            // 如果没有触发任何效果，则从状态栈中弹出下一个状态
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }

        private void HandleAttackEffect(int numberIndex, int damage)
        {
            GameManager.SetGameMessage(GameText.AttackTriggeredBySingleNumber(SourcePlayer.Name, SourcePlayer.Numbers[numberIndex], damage));
            int[] numbers = new int[2];
            Array.Copy(SourcePlayer.Numbers, numbers, 2);
            SourcePlayer.SetNumber(numberIndex, 1);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            var ctx = new SkillContext(SourcePlayerId, damage, numbers: numbers);
            GameManager.TransitionTo(new OrderingAttackState(GameManager, ctx));
        }

        private void HandleAttackEffectBoth(int damage)
        {
            GameManager.SetGameMessage(GameText.AttackTriggeredByNumberPair(SourcePlayer.Name, damage));
            int[] numbers = new int[2];
            Array.Copy(SourcePlayer.Numbers, numbers, 2);
            SourcePlayer.SetNumber(0, 1);
            SourcePlayer.SetNumber(1, 1);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            var ctx = new SkillContext(SourcePlayerId, damage, numbers: numbers);
            GameManager.TransitionTo(new OrderingAttackState(GameManager, ctx));
        }

        private void HandleHealEffect(int numberIndex, int healing)
        {
            GameManager.SetGameMessage(GameText.HealTriggeredBySingleNumber(SourcePlayer.Name, SourcePlayer.Numbers[numberIndex], healing));
            SourcePlayer.SetNumber(numberIndex, 1);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            var ctx = new SkillContext(SourcePlayerId, 0, healing);
            GameManager.TransitionTo(new TargetSelectionState(GameManager, ctx));
        }

        private void HandleHealEffectBoth(int healing)
        {
            GameManager.SetGameMessage(GameText.HealTriggeredByNumberPair(SourcePlayer.Name, healing));
            SourcePlayer.SetNumber(0, 1);
            SourcePlayer.SetNumber(1, 1);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            var ctx = new SkillContext(SourcePlayerId, 0, healing);
            GameManager.TransitionTo(new TargetSelectionState(GameManager, ctx));
        }
    }

    /// <summary>
    /// 攻击指令状态（逐一检查攻击前技能）
    /// </summary>
    public class OrderingAttackState : GameState
    {
        private readonly SkillContext _ctx;

        public OrderingAttackState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
            if (_ctx.SkillEnumerator == null)
                _ctx.SkillEnumerator = SourcePlayer.GetSkillsByTrigger(SkillTrigger.OrderingAttack);
        }

        public override void Enter()
        {
            // OrderedAttack 技能推栈（先于 OrderingAttack 处理）
            foreach (var skill in SourcePlayer.Skills)
            {
                if (skill.Trigger == SkillTrigger.OrderedAttack && skill.CanUse(GameManager, SourcePlayer))
                {
                    var state = skill.CreateState(GameManager, new SkillContext(SourcePlayerId));
                    if (state != null)
                    {
                        GameManager.StateStack.Push(state);
                    }
                }
            }

            if (_ctx.SkillEnumerator.MoveNext())
            {
                var currentSkill = _ctx.SkillEnumerator.Current;
                if (currentSkill.CanUse(GameManager, SourcePlayer))
                {
                    var skillState = currentSkill.CreateState(GameManager, _ctx);
                    if (skillState != null)
                    {
                        GameManager.TransitionTo(skillState);
                        return;
                    }
                }
                // 当前技能不可用或没有对应状态，跳过继续
                GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
            }
            else
            {
                // 所有攻击前技能检查完毕，清理枚举器，进入选择目标阶段
                _ctx.DisposeEnumerator();
                GameManager.TransitionTo(new TargetSelectionState(GameManager, _ctx));
            }
        }
    }


    /// <summary>
    /// 选择目标状态
    /// </summary>
    public class TargetSelectionState : GameState
    {
        private readonly SkillContext _ctx;

        public TargetSelectionState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        public int DamageValue => _ctx.Damage;
        public int HealingValue => _ctx.Healing;

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseTarget,
                ValidTargets = GameManager.Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList(),
                Prompt = _ctx.Damage > 0 ? GameText.ChooseTargetForDamaging : GameText.ChooseTargetForHealing
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count == 0)
            {
                return;
            }

            var targetId = actions[0].TargetPlayerId;
            var targetPlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == targetId);
            if (targetPlayer == null || !targetPlayer.IsAlive()) return;

            _ctx.TargetPlayerId = targetId;

            if (_ctx.Damage > 0)
            {
                GameManager.TransitionTo(new DamagingState(GameManager, _ctx));
            }
            else
            {
                GameManager.TransitionTo(new HealingState(GameManager, _ctx));
            }
        }
    }

    /// <summary>
    /// 造成伤害状态
    /// </summary>
    public class DamagingState : GameState
    {
        private readonly SkillContext _ctx;

        public DamagingState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        public override void Enter()
        {
            var targetPlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == _ctx.TargetPlayerId);
            if (targetPlayer == null)
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            var (hasShield, shieldIndex) = targetPlayer.HasShield();
            if (hasShield)
            {
                targetPlayer.SetNumber(shieldIndex, 1); // 护盾数字变为1
                GameManager.PushState(new ApplyingEffectState(GameManager, _ctx.TargetPlayerId));
                GameManager.SetGameMessage(GameText.ShieldBreak(targetPlayer.Name));
                // 护盾破碎会导致数字改变，进入效果应用阶段，但源玩家是目标玩家
                GameManager.TransitionTo(new HandleStackState(GameManager));
            }
            else
            {
                // 进入防御技能处理流水线
                GameManager.TransitionTo(new PreDamageState(GameManager, _ctx));
            }
        }
    }

    /// <summary>
    /// 防御技能处理状态 — 逐一迭代目标玩家的防御技能，然后结算伤害和反应技能
    /// </summary>
    public class PreDamageState : GameState
    {
        private readonly SkillContext _ctx;
        private readonly int _attackerId;

        public PreDamageState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
            _attackerId = ctx.PipelineOriginId;

            if (_ctx.SkillEnumerator == null)
            {
                var target = gameManager.Players.FirstOrDefault(p => p.PlayerId == ctx.TargetPlayerId);
                _ctx.SkillEnumerator = target?.GetSkillsByTrigger(SkillTrigger.Defending);
            }
        }

        public override void Enter()
        {
            if (_ctx.SkillEnumerator != null && _ctx.SkillEnumerator.MoveNext())
            {
                var skill = _ctx.SkillEnumerator.Current;
                var target = GameManager.Players.FirstOrDefault(p => p.PlayerId == _ctx.TargetPlayerId);
                if (target != null && skill.CanUse(GameManager, target))
                {
                    var subCtx = _ctx.Derive(target.PlayerId);
                    var state = skill.CreateState(GameManager, subCtx);
                    if (state != null)
                    {
                        GameManager.TransitionTo(state);
                        return;
                    }
                }
                // 不可用则跳过
                GameManager.TransitionTo(new PreDamageState(GameManager, _ctx));
            }
            else
            {
                _ctx.DisposeEnumerator();
                ApplyDamage();
            }
        }

        private void ApplyDamage()
        {
            var targetPlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == _ctx.TargetPlayerId);
            if (targetPlayer == null)
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            targetPlayer.TakeDamage(_ctx.Damage);
            GameManager.SetGameMessage(GameText.Damaged(targetPlayer.Name, _ctx.Damage));

            // 推入受伤后反应技能（栈模式，直接调用 skill.CreateState 而非 SkillManager）
            var attacker = GameManager.Players.FirstOrDefault(p => p.PlayerId == _attackerId);

            // Damaging 技能（攻击者）
            if (attacker != null)
            {
                foreach (var skill in attacker.Skills)
                {
                    if (skill.Trigger == SkillTrigger.Damaging && skill.CanUse(GameManager, attacker))
                    {
                        var state = skill.CreateState(GameManager, _ctx.Derive(attacker.PlayerId));
                        if (state != null)
                            GameManager.StateStack.Push(state);
                    }
                }
            }

            // Damaged 技能（目标玩家）
            foreach (var skill in targetPlayer.Skills)
            {
                if (skill.Trigger == SkillTrigger.Damaged && skill.CanUse(GameManager, targetPlayer))
                {
                    var state = skill.CreateState(GameManager, _ctx.Derive(targetPlayer.PlayerId));
                    if (state != null)
                        GameManager.StateStack.Push(state);
                }
            }



            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    /// <summary>
    /// 治疗状态
    /// </summary>
    public class HealingState : GameState
    {
        private readonly SkillContext _ctx;

        public HealingState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        public override void Enter()
        {
            var targetPlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == _ctx.TargetPlayerId);
            if (targetPlayer == null)
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            targetPlayer.Heal(_ctx.Healing);
            GameManager.SetGameMessage(GameText.Healed(targetPlayer.Name, _ctx.Healing));

            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    /// <summary>
    /// 真实伤害状态
    /// </summary>
    public class TrueDamageState : GameState
    {
        private readonly SkillContext _ctx;

        public TrueDamageState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        public override void Enter()
        {
            var targetPlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == _ctx.TargetPlayerId);
            if (targetPlayer == null)
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            targetPlayer.TakeDamage(_ctx.Damage);
            GameManager.SetGameMessage(GameText.TrueDamaged(targetPlayer.Name, _ctx.Damage));

            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }

    /// <summary>
    /// 回合结束状态
    /// </summary>
    public class PlayerTurnEndState : GameState
    {
        public PlayerTurnEndState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }

        public override void Enter()
        {
            GameManager.EndCurrentTurn();
            var nextPlayer = GameManager.GetCurrentPlayer();
            GameManager.TransitionTo(new TurnStartState(GameManager, nextPlayer.PlayerId));
        }
    }
    #endregion
}


