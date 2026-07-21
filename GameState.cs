
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida
{
    /// <summary>
    /// 抽象游戏状态基类
    /// </summary>
    public abstract class GameState
    {
        protected readonly GameManager _gameManager;
        public int _sourcePlayerId;
        public Player _sourcePlayer;

        protected GameState(GameManager gameManager, int sourcePlayerId = -1)
        {
            _gameManager = gameManager;
            _sourcePlayerId = sourcePlayerId;
            if (sourcePlayerId != -1)
            {
                _sourcePlayer = _gameManager.Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);
            }
        }

        /// <summary>
        /// 进入状态时执行的逻辑
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// 处理当前状态下的逻辑和玩家输入
        /// </summary>
        public virtual void Handle(Action action) { }
    }

    #region Concrete Game States

    /// <summary>
    /// 游戏开始状态
    /// </summary>
    public class GameStartState : GameState
    {
        public GameStartState(GameManager gameManager) : base(gameManager) { }

        public override void Enter()
        {
            _gameManager.SetGameMessage(GameText.GameStart);
            _gameManager.OnGameStarted(new GameStartEventArgs { Message = _gameManager.GameMessage });

            // 游戏开始后，直接进入第一个玩家的回合开始阶段
            var firstPlayer = _gameManager.Players[_gameManager.CurrentPlayerIndex];
            _gameManager.StateStack.Push(new TurnStartState(_gameManager, firstPlayer.PlayerId));

            foreach (var player in _gameManager.Players)
            {
                if (player.SkillAvaible(SkillType.TripleHealth))
                {
                    _gameManager.StateStack.Push(new TripleHealthSkillState(_gameManager, player.PlayerId));
                }
            }


            _gameManager.TransitionTo(new HandleStackState(_gameManager));
        }
    }

    /// <summary>
    /// 处理状态栈的状态
    /// </summary>
    public class HandleStackState : GameState
    {
        public HandleStackState(GameManager gameManager) : base(gameManager) { }
        public override void Enter()
        {
            if (_gameManager.StateStack.Count > 0)
            {
                var nextState = _gameManager.StateStack.Pop();
                _gameManager.TransitionTo(nextState);
            }
            else
            {
                // 如果栈为空，通常意味着一个操作序列完成，进入回合结束
                _gameManager.TransitionTo(new PlayerTurnEndState(_gameManager, _gameManager.GetCurrentPlayer().PlayerId));
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
            _gameManager.SetGameMessage(GameText.TurnStart(_sourcePlayer.Name), _sourcePlayerId);
            _gameManager.OnTurnStarted(new TurnStartEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                Message = _gameManager.GameMessage
            });
            

            foreach (var player in _gameManager.Players)
            {
                player.ResetPerTurnSkills();
            }

            _gameManager.StateStack.Push(new ChooseNumberIndexState(_gameManager, _sourcePlayerId));
            if (_sourcePlayer.SkillAvaible(SkillType.Tong))
            {
                _gameManager.StateStack.Push(new TongSkillState(_gameManager, _sourcePlayerId));
            }


            // 所有回合开始技能检查完毕，进入选择数字阶段
            _gameManager.TransitionTo(new HandleStackState(_gameManager));
        }
    }

    /// <summary>
    /// 选择数字状态
    /// </summary>
    public class ChooseNumberIndexState : GameState
    {
        private int _stage = 0;
        private int _firstNumberIndex = -1;
        private readonly bool _changeBothNumbers = false;
        public ChooseNumberIndexState(GameManager gameManager, int sourcePlayerId, int stage = 0, int firstNumberIndex = -1, bool changeBothNumbers = false) : base(gameManager, sourcePlayerId)
        {
            _stage = stage;
            _firstNumberIndex = firstNumberIndex;
            _changeBothNumbers = changeBothNumbers;
        }

        public int Stage => _stage;


        public override void Enter()
        {
            if (_stage == 0)
            {
                _gameManager.SetGameMessage(GameText.ChooseFirstNumberIndex(_sourcePlayer.Name), _sourcePlayerId);
                _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumberIndex,
                    ValidTargets = [_sourcePlayerId],
                    Message = _gameManager.GameMessage
                });
            }
            else // _stage == 1
            {
                _gameManager.SetGameMessage(GameText.ChooseSecondNumberIndex(_sourcePlayer.Name), _sourcePlayerId);
                var validTargets = _gameManager.Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList();
                _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumberIndex,
                    ValidTargets = validTargets,
                    Message = _gameManager.GameMessage
                });
            }
        }

        public override void Handle(Action action)
        {
            if (action.Type != ActionType.ChooseNumberIndex) return;

            if (_stage == 0)
            {
                if (action.TargetPlayerId == _sourcePlayerId)
                {
                    // 进入下一阶段
                    _gameManager.TransitionTo(new ChooseNumberIndexState(_gameManager, _sourcePlayerId, 1, action.NumberIndex, _changeBothNumbers));
                }
            }
            else // _stage == 1
            {
                var otherPlayerSelected = _gameManager.Players.FirstOrDefault(p => p.PlayerId == action.TargetPlayerId);
                if (otherPlayerSelected == null || !otherPlayerSelected.IsAlive()) return;

                if (action.TargetPlayerId == _sourcePlayerId && action.NumberIndex == _firstNumberIndex)
                {
                    // 不能选择同一个数字，重新选择第一个数字
                    _gameManager.TransitionTo(new ChooseNumberIndexState(_gameManager, _sourcePlayerId, 0));
                    return;
                }
                int newCalculatedNumber = (_sourcePlayer.Numbers[_firstNumberIndex] + otherPlayerSelected.Numbers[action.NumberIndex]) % 10;

                if (_changeBothNumbers)
                {
                    _gameManager.SetGameMessage($"玩家 {_sourcePlayer.Name} 的两个数字都变为 {newCalculatedNumber}. 正在检查效果...", _sourcePlayerId);
                    _gameManager.ChangeBothNumbers(_sourcePlayerId, newCalculatedNumber, newCalculatedNumber);
                }
                else
                {
                    _gameManager.SetGameMessage($"玩家 {_sourcePlayer.Name} 的数字 {_sourcePlayer.Numbers[_firstNumberIndex]} 变为 {newCalculatedNumber}. 正在检查效果...", _sourcePlayerId);
                    _gameManager.ChangeNumber(_sourcePlayerId, _firstNumberIndex, newCalculatedNumber);
                }
                _gameManager.TransitionTo(new ApplyingEffectState(_gameManager, _sourcePlayerId));
            }
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
            var numbers = _sourcePlayer.Numbers;

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
            _gameManager.TransitionTo(new HandleStackState(_gameManager));
        }

        private void HandleAttackEffect(int numberIndex, int damage)
        {
            _gameManager.SetGameMessage($"玩家 {_sourcePlayer.Name} 的数字变为 {_sourcePlayer.Numbers[numberIndex]}, 触发攻击! (造成{damage}点伤害)", _sourcePlayerId);
            int[] numbers = new int[2];
            Array.Copy(_sourcePlayer.Numbers, numbers, 2);
            _gameManager.ChangeNumber(_sourcePlayerId, numberIndex, 1);
            _gameManager.PushState(new ApplyingEffectState(_gameManager, _sourcePlayerId));
            _gameManager._enumerator = SkillManager.OrderingAttackSkills.GetEnumerator();
            _gameManager.TransitionTo(new OrderingAttackState(_gameManager, _sourcePlayerId, numbers, damage));
        }

        private void HandleAttackEffectBoth(int damage)
        {
            _gameManager.SetGameMessage($"玩家 {_sourcePlayer.Name} 的数字组合触发攻击! (造成{damage}点伤害)", _sourcePlayerId);
            int[] numbers = new int[2];
            Array.Copy(_sourcePlayer.Numbers, numbers, 2);
            _gameManager.ChangeBothNumbers(_sourcePlayerId, 1, 1);
            _gameManager._enumerator = SkillManager.OrderingAttackSkills.GetEnumerator();
            _gameManager.TransitionTo(new OrderingAttackState(_gameManager, _sourcePlayerId, numbers, damage));
        }

        private void HandleHealEffect(int numberIndex, int healing)
        {
            _gameManager.SetGameMessage($"玩家 {_sourcePlayer.Name} 的数字变为 7, 触发治愈! (恢复{healing}点生命值)", _sourcePlayerId);
            _gameManager.ChangeNumber(_sourcePlayerId, numberIndex, 1);
            _gameManager.PushState(new ApplyingEffectState(_gameManager, _sourcePlayerId));
            _gameManager.TransitionTo(new TargetSelectionState(_gameManager, _sourcePlayerId, 0, healing));
        }

        private void HandleHealEffectBoth(int healing)
        {
            _gameManager.SetGameMessage($"玩家 {_sourcePlayer.Name} 的数字组合触发治愈! (恢复{healing}点生命值)", _sourcePlayerId);
            _gameManager.ChangeBothNumbers(_sourcePlayerId, 1, 1);
            _gameManager.TransitionTo(new TargetSelectionState(_gameManager, _sourcePlayerId, 0, healing));
        }
    }

    /// <summary>
    /// 攻击指令状态（检查攻击前技能）
    /// </summary>
    public class OrderingAttackState : GameState
    {
        private readonly int[] _numbers;
        private readonly int _baseDamage;
        public OrderingAttackState(GameManager gameManager, int sourcePlayerId, int[] numbers, int damage) : base(gameManager, sourcePlayerId)
        {
            _numbers = numbers;
            _baseDamage = damage;
        }


        public override void Enter()
        {

            if (_sourcePlayer.SkillAvaible(SkillType.Xian))
            {
                _gameManager.StateStack.Push(new XianSkillState(_gameManager, _sourcePlayerId));
            }
            
            if (_gameManager._enumerator.MoveNext())
            {
                var currentSkill = _gameManager._enumerator.Current;
                if (_sourcePlayer.SkillAvaible(currentSkill))
                {

                    // 转换到技能处理状态
                    GameState skillState = currentSkill switch
                    {
                        SkillType.Long => new LongSkillState(_gameManager, _sourcePlayerId, _numbers, _baseDamage),
                        SkillType.DoubleAttack => new DoubleAttackSkillState(_gameManager, _sourcePlayerId, _baseDamage),
                        _ => null
                    };

                    if (skillState != null)
                    {
                        _gameManager.TransitionTo(skillState);
                        return;
                    }
                }
                // 如果不满足条件或没有对应状态，直接进入下一阶段检查
                _gameManager.TransitionTo(new OrderingAttackState(_gameManager, _sourcePlayerId, _numbers, _baseDamage));
            }
            else
            {
                // 所有攻击前技能检查完毕，进入选择目标阶段
                _gameManager.TransitionTo(new TargetSelectionState(_gameManager, _sourcePlayerId, _baseDamage, 0));
            }
        }
    }


    /// <summary>
    /// 选择目标状态
    /// </summary>
    public class TargetSelectionState : GameState
    {
        private readonly int _damageValue;
        private readonly int _healingValue;

        public TargetSelectionState(GameManager gameManager, int sourcePlayerId, int damageValue, int healingValue) : base(gameManager, sourcePlayerId)
        {
            _damageValue = damageValue;
            _healingValue = healingValue;
        }

        public int DamageValue => _damageValue;
        public int HealingValue => _healingValue;

        public override void Enter()
        {
            if (_damageValue > 0)
            {
                _gameManager.SetGameMessage(GameText.ChooseTargetForDamaging, _sourcePlayerId);
            }
            else
            {
                _gameManager.SetGameMessage(GameText.ChooseTargetForHealing, _sourcePlayerId);
            }

            var validTargets = _gameManager.Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList();
            _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                RequiredActionType = ActionType.ChooseTarget,
                ValidTargets = validTargets,
                Message = _gameManager.GameMessage
            });
        }

        public override void Handle(Action action)
        {
            if (action.Type != ActionType.ChooseTarget) return;

            var targetPlayer = _gameManager.Players.FirstOrDefault(p => p.PlayerId == action.TargetPlayerId);
            if (targetPlayer == null || !targetPlayer.IsAlive()) return;

            if (_damageValue > 0)
            {
                _gameManager.TransitionTo(new DamagingState(_gameManager, _sourcePlayerId, action.TargetPlayerId, _damageValue));
            }
            else
            {
                _gameManager.TransitionTo(new HealingState(_gameManager, _sourcePlayerId, action.TargetPlayerId, _healingValue));
            }
        }
    }

    /// <summary>
    /// 造成伤害状态
    /// </summary>
    public class DamagingState : GameState
    {
        private readonly int _targetPlayerId;
        private readonly int _damageValue;

        public DamagingState(GameManager gameManager, int sourcePlayerId, int targetPlayerId, int damageValue) : base(gameManager, sourcePlayerId)
        {
            _targetPlayerId = targetPlayerId;
            _damageValue = damageValue;
        }

        public override void Enter()
        {
            var targetPlayer = _gameManager.Players.FirstOrDefault(p => p.PlayerId == _targetPlayerId);
            if (targetPlayer == null)
            {
                _gameManager.TransitionTo(new HandleStackState(_gameManager));
                return;
            }

            var (hasShield, shieldIndex) = targetPlayer.HasShield();
            if (hasShield)
            {
                targetPlayer.Numbers[shieldIndex] = 1; // 护盾数字变为1
                _gameManager.SetGameMessage(GameText.ShieldBreak(targetPlayer.Name), _targetPlayerId);
                // 护盾破碎会导致数字改变，进入效果应用阶段，但源玩家是目标玩家
                _gameManager.TransitionTo(new ApplyingEffectState(_gameManager, targetPlayer.PlayerId));
            }
            else
            {
                int finalDamage = _damageValue;
                if (targetPlayer.SkillAvaible(SkillType.DoubleDefence))
                {
                    finalDamage /= 2;
                }

                targetPlayer.TakeDamage(finalDamage);
                _gameManager.SetGameMessage(GameText.Damaged(targetPlayer.Name, finalDamage), _targetPlayerId);
                _gameManager.OnAttackExecuted(new AttackEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    TargetPlayerId = _targetPlayerId,
                    DamageValue = finalDamage,
                    Message = _gameManager.GameMessage
                });

                // 检查攻击后技能
                if (targetPlayer.SkillAvaible(SkillType.Shen))
                {
                    _gameManager.PushState(new ShenSkillState(_gameManager, targetPlayer.PlayerId));
                }
                if (_sourcePlayer.SkillAvaible(SkillType.Xue))
                {
                    _gameManager.PushState(new XueSkillState(_gameManager, _sourcePlayerId, finalDamage));
                }

                if (targetPlayer.SkillAvaible(SkillType.Mo))
                {
                    _gameManager.PushState(new MoSkillState(_gameManager, targetPlayer.PlayerId, finalDamage));
                }


                _gameManager.TransitionTo(new HandleStackState(_gameManager));
            }
        }
    }

    /// <summary>
    /// 治疗状态
    /// </summary>
    public class HealingState : GameState
    {
        private readonly int _targetPlayerId;
        private readonly int _healingValue;

        public HealingState(GameManager gameManager, int sourcePlayerId, int targetPlayerId, int healingValue) : base(gameManager, sourcePlayerId)
        {
            _targetPlayerId = targetPlayerId;
            _healingValue = healingValue;
        }

        public override void Enter()
        {
            var targetPlayer = _gameManager.Players.FirstOrDefault(p => p.PlayerId == _targetPlayerId);
            if (targetPlayer == null)
            {
                _gameManager.TransitionTo(new HandleStackState(_gameManager));
                return;
            }

            targetPlayer.Heal(_healingValue);
            _gameManager.SetGameMessage(GameText.Healed(targetPlayer.Name, _healingValue), _targetPlayerId);
            _gameManager.OnHealExecuted(new HealEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                TargetPlayerId = _targetPlayerId,
                HealingValue = _healingValue,
                Message = _gameManager.GameMessage
            });

            _gameManager.TransitionTo(new HandleStackState(_gameManager));
        }
    }

    /// <summary>
    /// 治疗状态
    /// </summary>
    public class TrueDamageState : GameState
    {
        private readonly int _targetPlayerId;
        private readonly int _damageValue;

        public TrueDamageState(GameManager gameManager, int sourcePlayerId, int targetPlayerId, int damageValue) : base(gameManager, sourcePlayerId)
        {
            _targetPlayerId = targetPlayerId;
            _damageValue = damageValue;
        }

        public override void Enter()
        {
            var targetPlayer = _gameManager.Players.FirstOrDefault(p => p.PlayerId == _targetPlayerId);
            if (targetPlayer == null)
            {
                _gameManager.TransitionTo(new HandleStackState(_gameManager));
                return;
            }

            targetPlayer.TakeDamage(_damageValue);
            _gameManager.SetGameMessage(GameText.TrueDamaged(targetPlayer.Name, _damageValue), _targetPlayerId);
            _gameManager.OnAttackExecuted(new AttackEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                TargetPlayerId = _targetPlayerId,
                DamageValue = _damageValue,
                Message = _gameManager.GameMessage
            });

            _gameManager.TransitionTo(new HandleStackState(_gameManager));
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
            _gameManager.EndCurrentTurn();
            var nextPlayer = _gameManager.GetCurrentPlayer();
            _gameManager.TransitionTo(new TurnStartState(_gameManager, nextPlayer.PlayerId));
        }
    }
    #endregion


    #region skill states
    // 技能的状态类 

    public class DoubleAttackSkillState : GameState
    {
        private readonly int _originalDamage;
        public DoubleAttackSkillState(GameManager gameManager, int sourcePlayerId, int originalDamage) : base(gameManager, sourcePlayerId)
        {
            _originalDamage = originalDamage;
        }

        public override void Enter()
        {
            _gameManager.SetGameMessage(GameText.SkillActivated(_sourcePlayer.Name, SkillType.DoubleAttack), _sourcePlayerId);
            _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                RequiredActionType = ActionType.DecideApply,
                Message = _gameManager.GameMessage
            });
        }

        public override void Handle(Action action)
        {
            if (action.Type != ActionType.DecideApply) return;

            var nextState = _gameManager.StateStack.Pop() as OrderingAttackState;
            int newDamage = action.IsApply ? _originalDamage * 2 : _originalDamage;

            // 创建一个新的OrderingAttackState实例，而不是修改弹出的实例
            var newState = new OrderingAttackState(_gameManager, nextState._sourcePlayerId, _sourcePlayer.Numbers, newDamage);
            _gameManager.TransitionTo(newState);
        }
    }

    public class DoubleDefenceSkillState: GameState
    {
        private readonly int _originalDamage;
        private readonly int _targetPlayerId;
        public DoubleDefenceSkillState(GameManager gameManager, int sourcePlayerId, int targetPlayerId, int originalDamage) : base(gameManager, sourcePlayerId)
        {
            _originalDamage = originalDamage;
            _targetPlayerId = targetPlayerId;
        }
        public override void Enter()
        {
            _gameManager.SetGameMessage(GameText.SkillActivated(_sourcePlayer.Name, SkillType.DoubleDefence), _sourcePlayerId);
        }
        public override void Handle(Action action)
        { 
            int newDamage = _originalDamage / 2;
            var newState = new DamagingState(_gameManager, _sourcePlayerId, _targetPlayerId, newDamage);
            _gameManager.TransitionTo(newState);
        }
    }

    public class TripleHealthSkillState : GameState
    {
        public TripleHealthSkillState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }
        public override void Enter()
        {
            _gameManager.SetGameMessage(GameText.SkillActivated(_sourcePlayer.Name, SkillType.TripleHealth), _sourcePlayerId);
            _sourcePlayer.MaxHealth = _sourcePlayer.MaxHealth * 3;
            _sourcePlayer.Health = _sourcePlayer.MaxHealth;
            _gameManager.OnSkillActivated(new SkillActivatedEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                SkillType = SkillType.TripleHealth,
                Message = _gameManager.GameMessage
            });
            // 技能效果处理完毕，回到状态栈的下一个状态
            _gameManager.TransitionTo(new HandleStackState(_gameManager));
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

        public override void Enter()
        {
            _gameManager.SetGameMessage(GameText.SkillActivated(_sourcePlayer.Name, SkillType.Long), _sourcePlayerId);
            _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                RequiredActionType = ActionType.DecideApply,
                Message = _gameManager.GameMessage
            });
        }

        public override void Handle(Action action)
        {
            if (action.Type != ActionType.DecideApply) return;
            int newDamage = action.IsApply ? _numbers[0] + _numbers[1] : _originalDamage;

            var newState = new OrderingAttackState(_gameManager, _sourcePlayerId, _numbers, newDamage);
            _gameManager.TransitionTo(newState);
        }
    }



    // 神
    public class ShenSkillState : GameState
    {
        private int _stage = 0;
        private int _numberIndexToChange = -1;

        public ShenSkillState(GameManager gameManager, int sourcePlayerId, int stage = 0, int numberIndex = -1) : base(gameManager, sourcePlayerId)
        {
            _stage = stage;
            _numberIndexToChange = numberIndex;
        }

        public override void Enter()
        {
            if (_stage == 0)
            {
                _gameManager.SetGameMessage(GameText.SkillActivatedNumberTurnFrom(_sourcePlayer.Name, SkillType.Shen), _sourcePlayerId);
                _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumberIndex,
                    ValidTargets = [_sourcePlayerId],
                    Message = _gameManager.GameMessage
                });
            }
            else // stage 1
            {
                _gameManager.SetGameMessage(GameText.SkillActivatedNumberTurnInto(_sourcePlayer.Name, SkillType.Shen), _sourcePlayerId);
                _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumber,
                    ValidNumbers = Enumerable.Range(0, 10).ToList(),
                    Message = _gameManager.GameMessage
                });
            }
        }

        public override void Handle(Action action)
        {
            if (_stage == 0 && action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == _sourcePlayerId)
            {
                _gameManager.TransitionTo(new ShenSkillState(_gameManager, _sourcePlayerId, 1, action.NumberIndex));
            }
            else if (_stage == 1 && action.Type == ActionType.ChooseNumber)
            {
                _gameManager.ChangeNumber(_sourcePlayerId, _numberIndexToChange, action.Number);
                // 技能效果处理完毕，回到状态栈的下一个状态
                _gameManager.TransitionTo(new ApplyingEffectState(_gameManager, _sourcePlayerId));
            }
        }
    }



    // 仙
    public class XianSkillState : GameState
    {
        private int _stage = 0;
        private int _numberIndexToChange = -1;

        public XianSkillState(GameManager gameManager, int sourcePlayerId, int stage = 0, int numberIndex = -1) : base(gameManager, sourcePlayerId)
        {
            _stage = stage;
            _numberIndexToChange = numberIndex;
        }

        public override void Enter()
        {
            if (_stage == 0)
            {
                _gameManager.SetGameMessage(GameText.SkillActivatedNumberTurnFrom(_sourcePlayer.Name, SkillType.Xian), _sourcePlayerId);
                _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumberIndex,
                    ValidTargets = [_sourcePlayerId],
                    Message = _gameManager.GameMessage
                });
            }
            else // stage 1
            {
                _gameManager.SetGameMessage(GameText.SkillActivatedNumberTurnInto(_sourcePlayer.Name, SkillType.Xian), _sourcePlayerId);
                _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumber,
                    ValidNumbers = [.. Enumerable.Range(0, 10)],
                    Message = _gameManager.GameMessage
                });
            }
        }

        public override void Handle(Action action)
        {
            if (_stage == 0 && action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == _sourcePlayerId)
            {
                _gameManager.TransitionTo(new XianSkillState(_gameManager, _sourcePlayerId, 1, action.NumberIndex));
            }
            else if (_stage == 1 && action.Type == ActionType.ChooseNumber)
            {
                _gameManager.ChangeNumber(_sourcePlayerId, _numberIndexToChange, action.Number);
                // 技能效果处理完毕，回到状态栈的下一个状态
                _gameManager.TransitionTo(new ApplyingEffectState(_gameManager, _sourcePlayerId));
            }
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

        public override void Enter()
        {
            // 血技能直接触发治疗，无需玩家输入
            _gameManager.TransitionTo(new HealingState(_gameManager, _sourcePlayerId, _sourcePlayerId, _damageDealt));
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

        public override void Enter()
        {
            _gameManager.SetGameMessage(GameText.SkillActivatedChooseTarget(_sourcePlayer.Name, SkillType.Mo) + $", 返回 {_damageReturned} 点伤害", _sourcePlayerId);
            var validTargets = _gameManager.Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList();
            _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                RequiredActionType = ActionType.ChooseTarget,
                ValidTargets = validTargets,
                Message = _gameManager.GameMessage 
            });
        }

        public override void Handle(Action action)
        {
            if (action.Type == ActionType.ChooseTarget)
                _gameManager.TransitionTo(new TrueDamageState(_gameManager, _sourcePlayerId, action.TargetPlayerId, _damageReturned));
        }
    }


    // 同

    public class TongSkillState : GameState
    {
        public TongSkillState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }
        public override void Enter()
        {
            _gameManager.SetGameMessage(GameText.SkillActivated(_sourcePlayer.Name, SkillType.Tong), _sourcePlayerId);
            _gameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                RequiredActionType = ActionType.DecideApply,
                ValidTargets = [_sourcePlayerId],
                Message = _gameManager.GameMessage
            });
        }

        public override void Handle(Action action)
        {
            if (action.Type != ActionType.DecideApply) return;
            if (action.IsApply)
            {
                _gameManager.StateStack.Pop(); 
                _gameManager.StateStack.Push(new ChooseNumberIndexState(_gameManager, _sourcePlayerId, 0, -1, true));
            }
            _gameManager.TransitionTo(new HandleStackState(_gameManager));
        }
    }


    #endregion
}


