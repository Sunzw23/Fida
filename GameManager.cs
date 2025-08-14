using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Linq;

namespace Fida
{
    // 游戏事件参数基类
    public class GameEventArgs : EventArgs
    {
        public int SourcePlayerId { get; set; }
        public int TargetPlayerId { get; set; }
        public string Message { get; set; }
    }

    // 具体事件参数类
    public class GameStartEventArgs : GameEventArgs { }

    public class TurnStartEventArgs : GameEventArgs { }

    public class NumberChangeEventArgs : GameEventArgs
    {
        public int NumberIndex { get; set; }
        public int OldValue { get; set; }
        public int NewValue { get; set; }
    }

    public class AttackEventArgs : GameEventArgs
    {
        public int DamageValue { get; set; }
    }

    public class HealEventArgs : GameEventArgs
    {
        public int HealingValue { get; set; }
    }

    public class SkillActivatedEventArgs : GameEventArgs
    {
        public Skill SkillType { get; set; }
    }

    public class PlayerActionRequiredEventArgs : GameEventArgs
    {
        public ActionType RequiredActionType { get; set; }
        public List<int> ValidTargets { get; set; } = [];
        public List<int> ValidNumbers { get; set; } = [];
    }

    public class GameEndEventArgs : GameEventArgs
    {
        public int WinnerPlayerId { get; set; }
        public bool IsDraw { get; set; }
    }

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

    /// <summary>
    /// GameManager 类负责管理游戏的核心逻辑、玩家状态和游戏流程. 
    /// 使用事件驱动模式进行解耦
    /// </summary>
    public class GameManager
    {
        // 事件定义
        public event EventHandler<GameStartEventArgs> GameStarted;
        public event EventHandler<TurnStartEventArgs> TurnStarted;
        public event EventHandler<NumberChangeEventArgs> NumberChanged;
        public event EventHandler<AttackEventArgs> AttackExecuted;
        public event EventHandler<HealEventArgs> HealExecuted;
        public event EventHandler<SkillActivatedEventArgs> SkillActivated;
        public event EventHandler<PlayerActionRequiredEventArgs> PlayerActionRequired;
        public event EventHandler<GameEndEventArgs> GameEnded;
        public event EventHandler<GameEventArgs> MessageUpdated;

        // 游戏状态
        public List<Player> Players { get; private set; } = [];
        public int CurrentPlayerIndex { get; private set; } = 0;
        public GamePhase CurrentGamePhase { get; set; }
        public ActionType CurrentActionType { get; private set; }
        public string GameMessage { get; private set; }
        public Stack<GamePhase> GamePhaseStack { get; private set; } = [];

        // 委托用于不同阶段的处理器
        private Dictionary<GamePhaseType, Func<Action, bool>> _phaseHandlers;

        // 构造函数
        public GameManager()
        {
            Players = [];
            GamePhaseStack = new Stack<GamePhase>();
            InitializePhaseHandlers();
        }

        public GameManager(List<Player> players)
        {
            Players = players;
            CurrentPlayerIndex = 0;
            var CurrentPlayer = Players[CurrentPlayerIndex];
            CurrentGamePhase = new GamePhase { Type = GamePhaseType.GameStart, SourcePlayerId = CurrentPlayer.PlayerId };
            InitializePhaseHandlers();
        }

        // 初始化阶段处理器字典
        private void InitializePhaseHandlers()
        {
            _phaseHandlers = new Dictionary<GamePhaseType, Func<Action, bool>>
            {
                { GamePhaseType.GameStart, HandleGameStart },
                { GamePhaseType.TurnStart, HandleTurnStart },
                { GamePhaseType.ChooseNumberIndex, HandleChooseNumberIndex },
                { GamePhaseType.ApplyingEffect, HandleSpecialEffects },
                { GamePhaseType.OrderingAttack, HandleOrderingAttack },
                { GamePhaseType.TargetSelection, HandleTargetSelection },
                { GamePhaseType.PlayerTurnEnd, HandlePlayerTurnEnd },
                { GamePhaseType.Damaging, HandleDamaging },
                { GamePhaseType.Healing, HandleHealing },
                { GamePhaseType.HandleSkillDoubleAttack, HandleSkillDoubleAttack },
                { GamePhaseType.HandleSkillShen, HandleSkillShen },
                { GamePhaseType.HandleSkillMo, HandleSkillMo },
                { GamePhaseType.HandleSkillXian, HandleSkillXian },
                { GamePhaseType.HandleSkillTong, HandleSkillTong },
                { GamePhaseType.HandleSkillDi, HandleSkillDi },
                { GamePhaseType.HandleSkillLong, HandleSkillLong },
                { GamePhaseType.HandleStack, HandleStack }
            };
        }

        public void Initialize(List<Player> players)
        {
            Players = players;
            CurrentPlayerIndex = 0;
            var CurrentPlayer = Players[CurrentPlayerIndex];
            CurrentGamePhase = new GamePhase { Type = GamePhaseType.GameStart, SourcePlayerId = CurrentPlayer.PlayerId };
            UpdateGame();
        }

        // 事件触发方法
        protected virtual void OnGameStarted(GameStartEventArgs e) => GameStarted?.Invoke(this, e);
        protected virtual void OnTurnStarted(TurnStartEventArgs e) => TurnStarted?.Invoke(this, e);
        protected virtual void OnNumberChanged(NumberChangeEventArgs e) => NumberChanged?.Invoke(this, e);
        protected virtual void OnAttackExecuted(AttackEventArgs e) => AttackExecuted?.Invoke(this, e);
        protected virtual void OnHealExecuted(HealEventArgs e) => HealExecuted?.Invoke(this, e);
        protected virtual void OnSkillActivated(SkillActivatedEventArgs e) => SkillActivated?.Invoke(this, e);
        protected virtual void OnPlayerActionRequired(PlayerActionRequiredEventArgs e) => PlayerActionRequired?.Invoke(this, e);
        protected virtual void OnGameEnded(GameEndEventArgs e) => GameEnded?.Invoke(this, e);
        protected virtual void OnMessageUpdated(GameEventArgs e) => MessageUpdated?.Invoke(this, e);

        // 设置游戏消息并触发事件
        private void SetGameMessage(string message, int sourcePlayerId = -1)
        {
            GameMessage = message;
            OnMessageUpdated(new GameEventArgs { Message = message, SourcePlayerId = sourcePlayerId });
        }

        /// <summary>
        /// 更新游戏逻辑
        /// </summary>
        public void UpdateGame(Action action)
        {
            // 检查是否有获胜者
            if (CheckGameEnd()) return;

            // 使用委托处理当前阶段
            if (_phaseHandlers.TryGetValue(CurrentGamePhase.Type, out var handler))
            {
                handler(action);
            }
        }

        private void UpdateGame()
        {
            // 检查是否有获胜者
            if (CheckGameEnd()) return;
            // 如果没有传入动作, 则处理当前阶段
            if (_phaseHandlers.TryGetValue(CurrentGamePhase.Type, out var handler))
            {
                handler(new Action());
            }
        }

        private bool CheckGameEnd()
        {
            var alivePlayers = Players.Where(p => p.IsAlive()).ToList();
            if (alivePlayers.Count <= 1)
            {
                if (alivePlayers.Count == 1)
                {
                    var winner = alivePlayers[0];
                    SetGameMessage(GameText.GameEndWithWinner(winner.Name));
                    OnGameEnded(new GameEndEventArgs
                    {
                        WinnerPlayerId = winner.PlayerId,
                        Message = GameMessage
                    });
                }
                else
                {
                    SetGameMessage(GameText.GameEndWithDraw);
                    OnGameEnded(new GameEndEventArgs
                    {
                        IsDraw = true,
                        Message = GameMessage
                    });
                }
                return true;
            }
            return false;
        }

        private bool HandleGameStart(Action action)
        {
            SetGameMessage(GameText.GameStart);
            OnGameStarted(new GameStartEventArgs { Message = GameMessage });

            foreach (var player in Players)
            {
                if (player.HasSkill(Skill.TripleHealth))
                {
                    player.Health *= 3;
                    player.MaxHealth *= 3;
                }
            }

            CurrentGamePhase = new GamePhase { Type = GamePhaseType.TurnStart };
            UpdateGame();
            return true;
        }

        private bool HandleStack(Action action)
        {
            if (GamePhaseStack.Count > 0)
            {
                CurrentGamePhase = GamePhaseStack.Pop();
            }
            else
            {
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.PlayerTurnEnd };
            }
            UpdateGame();
            return true;
        }



        private bool HandleTurnStart(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
            var _stage = CurrentGamePhase.Stage;

            SetGameMessage(GameText.TurnStart(_sourcePlayer.Name), _sourcePlayerId);
            OnTurnStarted(new TurnStartEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                Message = GameMessage
            });

            if (_stage < GameConfig.TurnStartSkills.Count)
            {
                var _currentSkill = GameConfig.TurnStartSkills[_stage];
                if (_sourcePlayer.HasSkill(_currentSkill) && _sourcePlayer.ConditionSatisfiedSkill(_currentSkill))
                {
                    SetGameMessage(GameText.SkillActivated(_sourcePlayer.Name, _currentSkill), _sourcePlayerId);
                    OnSkillActivated(new SkillActivatedEventArgs
                    {
                        SourcePlayerId = _sourcePlayerId,
                        SkillType = _currentSkill,
                        Message = GameMessage
                    });

                    GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TurnStart, SourcePlayerId = _sourcePlayerId, Stage = _stage + 1 });
                    CurrentGamePhase = new GamePhase { Type = GamePhaseOfSkill(_currentSkill), SourcePlayerId = _sourcePlayerId };
                }
                else
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.TurnStart, SourcePlayerId = _sourcePlayerId, Stage = _stage + 1 };
                }
            }
            else
            {
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.ChooseNumberIndex, SourcePlayerId = _sourcePlayerId };
            }
            UpdateGame();
            return true;
        }

        private GamePhaseType GamePhaseOfSkill(Skill skill)
        {
            return skill switch
            {
                Skill.DoubleAttack => GamePhaseType.HandleSkillDoubleAttack,
                Skill.Shen => GamePhaseType.HandleSkillShen,
                Skill.Mo => GamePhaseType.HandleSkillMo,
                Skill.Xian => GamePhaseType.HandleSkillXian,
                Skill.Tong => GamePhaseType.HandleSkillTong,
                Skill.Di => GamePhaseType.HandleSkillDi,
                Skill.Long => GamePhaseType.HandleSkillLong,
                _ => throw new ArgumentOutOfRangeException(nameof(skill), skill, null)
            };
        }

        private void ChangeNumber(int numberChangeIndex, int numberChangeValue)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
            var oldValue = _sourcePlayer.Numbers[numberChangeIndex];

            _sourcePlayer.SetNumber(numberChangeIndex, numberChangeValue);

            OnNumberChanged(new NumberChangeEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                NumberIndex = numberChangeIndex,
                OldValue = oldValue,
                NewValue = numberChangeValue
            });

            GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.ApplyingEffect, SourcePlayerId = _sourcePlayerId });
        }

        private void ChangeBothNumbers(int firstNumberChangeValue, int secondNumberChangeValue)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);

            var oldValue1 = _sourcePlayer.Numbers[0];
            var oldValue2 = _sourcePlayer.Numbers[1];

            _sourcePlayer.SetNumber(0, firstNumberChangeValue);
            _sourcePlayer.SetNumber(1, secondNumberChangeValue);

            OnNumberChanged(new NumberChangeEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                NumberIndex = 0,
                OldValue = oldValue1,
                NewValue = firstNumberChangeValue
            });

            OnNumberChanged(new NumberChangeEventArgs
            {
                SourcePlayerId = _sourcePlayerId,
                NumberIndex = 1,
                OldValue = oldValue2,
                NewValue = secondNumberChangeValue
            });

            GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.ApplyingEffect, SourcePlayerId = _sourcePlayerId });
        }

        /// <summary>
        /// 处理选择数字的逻辑
        /// </summary>
        private bool HandleChooseNumberIndex(Action action)
        {
            var _stage = CurrentGamePhase.Stage;
            if (_stage == 0)
            {
                var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
                var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);

                SetGameMessage(GameText.ChooseFirstNumberIndex(_sourcePlayer.Name), _sourcePlayerId);
                CurrentActionType = ActionType.ChooseNumberIndex;

                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumberIndex,
                    ValidTargets = [_sourcePlayerId],
                    Message = GameMessage
                });

                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == CurrentGamePhase.SourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.ChooseNumberIndex, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = action.NumberIndex, Stage = 1 };
                }
            }
            else if (_stage == 1)
            {
                var _sourcPlayerId = CurrentGamePhase.SourcePlayerId;
                var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcPlayerId);

                SetGameMessage(GameText.ChooseSecondNumberIndex(_sourcePlayer.Name), _sourcPlayerId);
                CurrentActionType = ActionType.ChooseNumberIndex;

                var validTargets = Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList();
                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcPlayerId,
                    RequiredActionType = ActionType.ChooseNumberIndex,
                    ValidTargets = validTargets,
                    Message = GameMessage
                });

                if (action.Type == ActionType.ChooseNumberIndex)
                {
                    var otherPlayerSelected = Players.FirstOrDefault(p => p.PlayerId == action.TargetPlayerId);
                    if (otherPlayerSelected == null || otherPlayerSelected.IsAlive() == false)
                    {
                        return false; // 选择的目标为空或者已经死亡
                    }

                    if (action.TargetPlayerId == _sourcPlayerId && action.NumberIndex == CurrentGamePhase.SourceNumberIndex)
                    {
                        CurrentGamePhase = new GamePhase { Type = GamePhaseType.ChooseNumberIndex, SourcePlayerId = _sourcPlayerId, Stage = 0 };
                        return false;
                    }

                    var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
                    int _newCalculatedNumber = (_sourcePlayer.Numbers[_sourceNumberIndex] + otherPlayerSelected.Numbers[action.NumberIndex]) % 10;
                    SetGameMessage($"玩家 {_sourcePlayer.Name} 的数字 {_sourcePlayer.Numbers[_sourceNumberIndex]} 变为 {_newCalculatedNumber}. 正在检查效果...", _sourcPlayerId);
                    ChangeNumber(_sourceNumberIndex, _newCalculatedNumber);
                    CurrentGamePhase = new GamePhase();
                    UpdateGame();
                }
            }
            return true;
        }

        /// <summary>
        /// 处理特殊效果逻辑
        /// </summary>
        private bool HandleSpecialEffects(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
            int _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
            var _numbers = _sourcePlayer.Numbers;

            // 攻击 (数字变为 8 或 9)
            if (_numbers[0] == 8 || _numbers[0] == 9)
            {
                SetGameMessage($"玩家 {_sourcePlayer.Name} 的第一个数字变为 {_numbers[0]}, 触发攻击! (造成1点伤害)", _sourcePlayerId);
                ChangeNumber(0, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 1, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }
            else if (_numbers[1] == 8 || _numbers[1] == 9)
            {
                SetGameMessage($"玩家 {_sourcePlayer.Name} 的第二个数字变为 {_numbers[1]}, 触发攻击! (造成1点伤害)", _sourcePlayerId);
                ChangeNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 1, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }
            // 攻击 (数字变为 1 和 6)
            else if ((_sourcePlayer.Numbers[0] == 1 && _sourcePlayer.Numbers[1] == 6) || (_sourcePlayer.Numbers[0] == 6 && _sourcePlayer.Numbers[1] == 1))
            {
                SetGameMessage($"玩家 {_sourcePlayer.Name} 的两个数字变为 1 和 6, 触发攻击! (造成2点伤害)", _sourcePlayerId);
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 2, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }
            // 攻击 (数字变为 5 和 5)
            else if (_sourcePlayer.Numbers[0] == 5 && _sourcePlayer.Numbers[1] == 5)
            {
                SetGameMessage($"玩家 {_sourcePlayer.Name} 的两个数字变为 5 和 5, 触发攻击! (造成3点伤害)", _sourcePlayerId);
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 3, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }
            // 攻击 (数字变为 6 和 6)
            else if (_sourcePlayer.Numbers[0] == 6 && _sourcePlayer.Numbers[1] == 6)
            {
                SetGameMessage($"玩家 {_sourcePlayer.Name} 的两个数字变为 6 和 6, 触发攻击! (造成5点伤害)", _sourcePlayerId);
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 5, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }
            // 治愈 (数字变为 7)
            else if (_numbers[0] == 7)
            {
                SetGameMessage($"玩家 {_sourcePlayer.Name} 的数字变为 7, 触发治愈! (恢复1点生命值)", _sourcePlayerId);
                ChangeNumber(0, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, HealingValue = 1, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }
            else if (_numbers[1] == 7)
            {
                SetGameMessage($"玩家 {_sourcePlayer.Name} 的数字变为 7, 触发治愈! (恢复1点生命值)", _sourcePlayerId);
                ChangeNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, HealingValue = 1, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }
            // 治愈 (两个数字都变为 0)
            else if (_sourcePlayer.Numbers[0] == 0 && _sourcePlayer.Numbers[1] == 0)
            {
                SetGameMessage($"玩家 {_sourcePlayer.Name} 的两个数字都变为 0, 触发治愈! (恢复10点生命值)", _sourcePlayerId);
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, HealingValue = 10, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }

            CurrentGamePhase = new GamePhase();
            
            return true;
        }

        /// <summary>
        /// 发起攻击指令。
        /// </summary>
        private bool HandleOrderingAttack(Action action)
        {
            var sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);
            var stage = CurrentGamePhase.Stage;

            if (stage < GameConfig.OrderingAttackSkills.Count)
            {
                var currentSkill = GameConfig.OrderingAttackSkills[stage];
                if (sourcePlayer.HasSkill(currentSkill) && sourcePlayer.ConditionSatisfiedSkill(currentSkill))
                {
                    SetGameMessage(GameText.SkillActivated(sourcePlayer.Name, currentSkill), sourcePlayerId);
                    OnSkillActivated(new SkillActivatedEventArgs
                    {
                        SourcePlayerId = sourcePlayerId,
                        SkillType = currentSkill,
                        Message = GameMessage
                    });
                    CurrentGamePhase = new GamePhase { Type = GamePhaseOfSkill(currentSkill), SourcePlayerId = sourcePlayerId, DamageValue = CurrentGamePhase.DamageValue };
                    return true;
                }
                else
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.OrderingAttack, SourcePlayerId = sourcePlayerId, DamageValue = CurrentGamePhase.DamageValue, Stage = stage + 1 };
                }
            }
            else
            {
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.TargetSelection, SourcePlayerId = sourcePlayerId, DamageValue = CurrentGamePhase.DamageValue };
            }
            return true;
        }

        public bool IsDamagingButHealing()
        {
            return CurrentGamePhase.DamageValue > 0;
        }

        /// <summary>
        /// 处理目标选择阶段的逻辑
        /// </summary>
        private bool HandleTargetSelection(Action action)
        {
            if (IsDamagingButHealing())
            {
                SetGameMessage(GameText.ChooseTargetForDamaging, CurrentGamePhase.SourcePlayerId);
            }
            else
            {
                SetGameMessage(GameText.ChooseTargetForHealing, CurrentGamePhase.SourcePlayerId);
            }

            CurrentActionType = ActionType.ChooseTarget;
            var validTargets = Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList();

            OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = CurrentGamePhase.SourcePlayerId,
                RequiredActionType = ActionType.ChooseTarget,
                ValidTargets = validTargets,
                Message = GameMessage
            });

            if (action.Type == ActionType.ChooseTarget)
            {
                var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
                var _targetPlayerId = action.TargetPlayerId;
                var _targetPlayer = Players.FirstOrDefault(p => p.PlayerId == _targetPlayerId);

                if (_targetPlayer == null)
                {
                    return false; // 选择的目标为空
                }

                if (IsDamagingButHealing())
                {
                    int _damageValue = CurrentGamePhase.DamageValue;
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.Damaging, SourcePlayerId = _sourcePlayerId, TargetPlayerId = _targetPlayerId, DamageValue = _damageValue };
                }
                else
                {
                    int _healingValue = CurrentGamePhase.HealingValue;
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.Healing, SourcePlayerId = _sourcePlayerId, TargetPlayerId = _targetPlayerId, HealingValue = _healingValue };
                }
            }
            return true;
        }

        /// <summary>
        /// 处理攻击阶段的逻辑
        /// </summary>
        private bool HandleDamaging(Action action)
        {
            int _damageValue = CurrentGamePhase.DamageValue;
            var sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);
            var targetPlayerId = CurrentGamePhase.TargetPlayerId;
            var targetPlayer = Players.FirstOrDefault(p => p.PlayerId == targetPlayerId);

            // 如果有护盾, 消耗护盾并取消伤害
            var (_hasShield, _shieldIndex) = targetPlayer.HasShield();
            if (_hasShield)
            {
                targetPlayer.Numbers[_shieldIndex] = 1; // 护盾数字变为1
                SetGameMessage(GameText.ShieldBreak(targetPlayer.Name), targetPlayerId);
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.ApplyingEffect, SourcePlayerId = targetPlayer.PlayerId, SourceNumberIndex = _shieldIndex };
            }
            else
            {
                if (targetPlayer.HasSkill(Skill.DoubleDefence))
                {
                    _damageValue /= 2;
                }

                targetPlayer.TakeDamage(_damageValue);
                SetGameMessage(GameText.Attacked(targetPlayer.Name, _damageValue), targetPlayerId);

                OnAttackExecuted(new AttackEventArgs
                {
                    SourcePlayerId = sourcePlayerId,
                    TargetPlayerId = targetPlayerId,
                    DamageValue = _damageValue,
                    Message = GameMessage
                });

                if (targetPlayer.HasSkill(Skill.Shen))
                {
                    GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.HandleSkillShen, SourcePlayerId = targetPlayer.PlayerId });
                }

                if (sourcePlayer.HasSkill(Skill.Mo))
                {
                    GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.HandleSkillMo, SourcePlayerId = sourcePlayerId, DamageValue = _damageValue });
                }
                CurrentGamePhase = new GamePhase();
            }
            return true;
        }

        /// <summary>
        /// 处理治疗阶段的逻辑
        /// </summary>
        private bool HandleHealing(Action action)
        {
            int _healingValue = CurrentGamePhase.HealingValue;
            var targetPlayerId = CurrentGamePhase.TargetPlayerId;
            var targetPlayer = Players.FirstOrDefault(p => p.PlayerId == targetPlayerId);

            targetPlayer.Heal(_healingValue);
            SetGameMessage($"玩家 {targetPlayer.Name} 被治疗 {_healingValue} 点生命值.", targetPlayerId);

            OnHealExecuted(new HealEventArgs
            {
                SourcePlayerId = CurrentGamePhase.SourcePlayerId,
                TargetPlayerId = targetPlayerId,
                HealingValue = _healingValue,
                Message = GameMessage
            });

            CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleStack };
            return true;
        }

        /// <summary>
        /// 处理双倍攻击技能的逻辑
        /// </summary>
        private bool HandleSkillDoubleAttack(Action action)
        {
            var sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var name = Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId)?.Name;

            SetGameMessage(GameText.SkillActivated(name, Skill.DoubleAttack), sourcePlayerId);
            CurrentActionType = ActionType.DecideApply;

            OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = sourcePlayerId,
                RequiredActionType = ActionType.DecideApply,
                Message = GameMessage
            });

            if (action.Type == ActionType.DecideApply)
            {
                var damageValue = CurrentGamePhase.DamageValue;
                if (action.IsApply)
                {
                    damageValue *= 2;
                }
                int stage = GameConfig.OrderingAttackSkills.IndexOf(Skill.DoubleAttack);
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.OrderingAttack, SourcePlayerId = sourcePlayerId, DamageValue = damageValue, Stage = stage + 1 };
            }
            return true;
        }

        private bool HandleSkillLong(Action action)
        {
            var sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);

            SetGameMessage(GameText.SkillActivated(sourcePlayer.Name, Skill.Long), sourcePlayerId);
            CurrentActionType = ActionType.DecideApply;

            OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = sourcePlayerId,
                RequiredActionType = ActionType.DecideApply,
                Message = GameMessage
            });

            if (action.Type == ActionType.DecideApply)
            {
                var damageValue = CurrentGamePhase.DamageValue;
                if (action.IsApply)
                {
                    damageValue = sourcePlayer.Numbers[0] + sourcePlayer.Numbers[1];
                }
                int stage = GameConfig.OrderingAttackSkills.IndexOf(Skill.Long);
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.OrderingAttack, SourcePlayerId = sourcePlayerId, DamageValue = damageValue, Stage = stage + 1 };
            }
            return true;
        }

        /// <summary>
        /// 处理神技能的逻辑
        /// </summary>
        private bool HandleSkillShen(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);

            if (CurrentGamePhase.Stage == 0)
            {
                SetGameMessage(GameText.SkillActivatedNumberTurnFrom(_sourcePlayer.Name, Skill.Shen), _sourcePlayerId);
                CurrentActionType = ActionType.ChooseNumberIndex;

                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumberIndex,
                    ValidTargets = new List<int> { _sourcePlayerId },
                    Message = GameMessage
                });

                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == _sourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillShen, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = action.NumberIndex, Stage = 1 };
                }
            }
            else if (CurrentGamePhase.Stage == 1)
            {
                SetGameMessage(GameText.SkillActivatedNumberTurnInto(_sourcePlayer.Name, Skill.Shen), _sourcePlayerId);
                CurrentActionType = ActionType.ChooseNumber;

                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumber,
                    ValidNumbers = Enumerable.Range(0, 10).ToList(),
                    Message = GameMessage
                });

                if (action.Type == ActionType.ChooseNumber)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillShen, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex, NewNumber = action.Number, Stage = 2 };
                }
            }
            else
            {
                int _newNumber = CurrentGamePhase.NewNumber;
                ChangeNumber(_sourceNumberIndex, _newNumber);
                CurrentActionType = ActionType.None;
                CurrentGamePhase = new GamePhase();
            }
            return true;
        }

        private bool HandleSkillXian(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);

            if (CurrentGamePhase.Stage == 0)
            {
                SetGameMessage(GameText.SkillActivatedNumberTurnFrom(_sourcePlayer.Name, Skill.Xian), _sourcePlayerId);
                CurrentActionType = ActionType.ChooseNumberIndex;

                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumberIndex,
                    ValidTargets = new List<int> { _sourcePlayerId },
                    Message = GameMessage
                });

                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == _sourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillXian, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = action.NumberIndex, Stage = 1 };
                }
            }
            else if (CurrentGamePhase.Stage == 1)
            {
                SetGameMessage(GameText.SkillActivatedNumberTurnInto(_sourcePlayer.Name, Skill.Xian), _sourcePlayerId);
                CurrentActionType = ActionType.ChooseNumber;

                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumber,
                    ValidNumbers = Enumerable.Range(0, 10).ToList(),
                    Message = GameMessage
                });

                if (action.Type == ActionType.ChooseNumber)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillXian, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex, NewNumber = action.Number, Stage = 2 };
                }
            }
            else
            {
                int _newNumber = CurrentGamePhase.NewNumber;
                ChangeNumber(_sourceNumberIndex, _newNumber);
                CurrentActionType = ActionType.None;
                CurrentGamePhase = new GamePhase();
            }
            return true;
        }

        private bool HandleSkillMo(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _damageValue = CurrentGamePhase.DamageValue;
            CurrentGamePhase = new GamePhase { Type = GamePhaseType.Healing, TargetPlayerId = _sourcePlayerId, HealingValue = _damageValue };
            return true;
        }

        private bool HandleSkillDi(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);

            if (CurrentGamePhase.Stage == 0)
            {
                SetGameMessage(GameText.SkillActivatedNumberTurnFrom(_sourcePlayer.Name, Skill.Di), _sourcePlayerId);
                CurrentActionType = ActionType.ChooseNumberIndex;

                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumberIndex,
                    ValidTargets = new List<int> { _sourcePlayerId },
                    Message = GameMessage
                });

                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == _sourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillDi, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = action.NumberIndex, Stage = 1 };
                }
            }
            else if (CurrentGamePhase.Stage == 1)
            {
                SetGameMessage(GameText.SkillActivatedNumberTurnInto(_sourcePlayer.Name, Skill.Di), _sourcePlayerId);
                CurrentActionType = ActionType.ChooseNumber;

                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = _sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumber,
                    ValidNumbers = Enumerable.Range(0, 10).ToList(),
                    Message = GameMessage
                });

                if (action.Type == ActionType.ChooseNumber)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillDi, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex, NewNumber = action.Number, Stage = 2 };
                }
            }
            else
            {
                int _newNumber = CurrentGamePhase.NewNumber;
                ChangeNumber(_sourceNumberIndex, _newNumber);
                CurrentActionType = ActionType.None;
                CurrentGamePhase = new GamePhase();
            }
            return true;
        }

        /// <summary>
        /// 处理同变技能的逻辑
        /// </summary>
        private bool HandleSkillTong(Action action)
        {
            var stage = CurrentGamePhase.Stage;
            var sourcePlayerId = CurrentGamePhase.SourcePlayerId;

            if (stage == 0)
            {
                var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);
                SetGameMessage(GameText.SkillActivatedUseIf(_sourcePlayer.Name, Skill.Tong), sourcePlayerId);
                CurrentActionType = ActionType.DecideApply;

                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = sourcePlayerId,
                    RequiredActionType = ActionType.DecideApply,
                    Message = GameMessage
                });

                if (action.Type == ActionType.DecideApply)
                {
                    if (action.IsApply)
                    {
                        CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillTong, SourcePlayerId = sourcePlayerId, Stage = 1 };
                    }
                    else
                    {
                        CurrentGamePhase = new GamePhase();
                    }
                }
            }
            else
            {
                CurrentActionType = ActionType.ChooseNumber;

                OnPlayerActionRequired(new PlayerActionRequiredEventArgs
                {
                    SourcePlayerId = sourcePlayerId,
                    RequiredActionType = ActionType.ChooseNumber,
                    ValidNumbers = Enumerable.Range(0, 10).ToList(),
                    Message = GameMessage
                });

                if (action.Type == ActionType.ChooseNumber)
                {
                    int _newNumber = action.Number;
                    ChangeBothNumbers(_newNumber, _newNumber);
                    CurrentGamePhase = new GamePhase();
                }
            }
            return true;
        }

        /// <summary>
        /// 处理玩家回合结束的逻辑
        /// </summary>
        private bool HandlePlayerTurnEnd(Action action)
        {
            // 寻找下一个存活的玩家
            int nextPlayerTrial = (CurrentPlayerIndex + 1) % Players.Count;
            while (!Players[nextPlayerTrial].IsAlive() && nextPlayerTrial != CurrentPlayerIndex)
            {
                nextPlayerTrial = (nextPlayerTrial + 1) % Players.Count;
            }

            if (Players[nextPlayerTrial].IsAlive())
            {
                CurrentPlayerIndex = nextPlayerTrial;
                var _currentPlayer = Players[CurrentPlayerIndex];
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.TurnStart, SourcePlayerId = _currentPlayer.PlayerId };
                SetGameMessage(GameText.TurnStart(_currentPlayer.Name), _currentPlayer.PlayerId);
            }
            else
            {
                // 理论上, 这种情况应该在游戏结束检查中捕获, 但作为后备
                SetGameMessage(GameText.GameEnd);
            }
            return true;
        }
    }
}