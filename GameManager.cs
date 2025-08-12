// GameManager.cs 文件
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
    /// </summary>
    public class GameManager
    {
        public List<Player> Players { get; private set; } = [];// 所有玩家列表
        public int CurrentPlayerIndex { get; private set; } = 0; // 当前活跃玩家的索引
        public GamePhase CurrentGamePhase { get; set; } // 当前游戏阶段
        public ActionType CurrentActionType { get; private set; } // 当前操作类型
        public string GameMessage { get; private set; } // 显示给玩家的游戏消息

        public Stack<GamePhase> GamePhaseStack { get; private set; } = []; // 用于保存游戏阶段的栈



        // 构造函数
        public GameManager()
        {
            Players = [];
            GamePhaseStack = new Stack<GamePhase>();
        }

        public GameManager(List<Player> players)
        {
            Players = players;
            CurrentPlayerIndex = 0;
            var CurrentPlayer = Players[CurrentPlayerIndex];
            CurrentGamePhase = new GamePhase { Type = GamePhaseType.GameStart, SourcePlayerId = CurrentPlayer.PlayerId };
        }

        public void Initialize(List<Player> players)
        {
            Players = players;
            CurrentPlayerIndex = 0;
            var CurrentPlayer = Players[CurrentPlayerIndex];
            CurrentGamePhase = new GamePhase { Type = GamePhaseType.GameStart, SourcePlayerId = CurrentPlayer.PlayerId };
        }


        /// <summary>
        /// 更新游戏逻辑. 
        /// </summary>
        public void UpdateGame(Action action)
        {
            // 检查是否有获胜者 (只剩一个玩家)
            var alivePlayers = Players.Where(p => p.IsAlive()).ToList();
            if (alivePlayers.Count <= 1)
            {
                if (alivePlayers.Count == 1)
                {
                    GameMessage = GameText.GameEndWithWinner(alivePlayers[0].Name);
                }
                else
                {
                    GameMessage = GameText.GameEndWithDraw;
                }
                return; // 游戏结束, 不再处理输入
            }

            var currentPlayer = Players[CurrentPlayerIndex];

            // 根据当前游戏阶段处理逻辑
            switch (CurrentGamePhase.Type)
            {
                case GamePhaseType.GameStart:
                    HandleGameStart();
                    break;

                case GamePhaseType.TurnStart:
                    HandleTurnStart();
                    break;

                case GamePhaseType.ChooseNumberIndex:
                    HandleChooseNumberIndex(action);
                    break;
                case GamePhaseType.ApplyingEffect:
                    HandleSpecialEffects();
                    break;

                case GamePhaseType.OrderingAttack:
                    HandleOrderingAttack();
                    break;

                case GamePhaseType.TargetSelection:
                    HandleTargetSelection(action);
                    break;

                case GamePhaseType.PlayerTurnEnd:
                    HandlePlayerTurnEnd();
                    break;

                case GamePhaseType.Damaging:
                    HandleDamaging();
                    break;

                case GamePhaseType.Healing:
                    HandleHealing();
                    break;

                case GamePhaseType.HandleSkillDoubleAttack:
                    HandleSkillDoubleAttack(action);
                    break;

                case GamePhaseType.HandleSkillShen:
                    HandleSkillShen(action);
                    break;

                case GamePhaseType.HandleSkillMo:
                    HandleSkillMo();
                    break;

                case GamePhaseType.HandleSkillXian:
                    HandleSkillXian(action);
                    break;

                case GamePhaseType.HandleSkillTong:
                    HandleSkillTong(action);
                    break;

                case GamePhaseType.HandleSkillDi:
                    HandleSkillDi(action);
                    break;

                case GamePhaseType.HandleSkillLong:
                    HandleSkillLong(action);
                    break;

                default:
                    HandleStack();
                    break;
            }
        }

        private void HandleGameStart()
        {
            GameMessage = GameText.GameStart;

            foreach (var player in Players)
            {
                if (player.HasSkill(Skill.TripleHealth))
                {
                    player.Health *= 3;
                    player.MaxHealth *= 3;
                }
            }

            CurrentGamePhase = new GamePhase { Type = GamePhaseType.TurnStart };
        }

        private void HandleStack()
        {
            if (GamePhaseStack.Count > 0)
            {
                CurrentGamePhase = GamePhaseStack.Pop();
            }
            else
            {
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.PlayerTurnEnd };
            }
        }


        private bool SkillConditionStatisfied(Skill skill)
        {
            if (skill == Skill.Tong)
            {
                var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
                var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
                return _sourcePlayer.Numbers[0] == _sourcePlayer.Numbers[1] && _sourcePlayer.Numbers[0] == 0;
            }
            return true;
        }

        private void HandleTurnStart()
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
            var _stage = CurrentGamePhase.Stage;
            GameMessage = GameText.TurnStart(_sourcePlayer.Name);
            if (_stage < GameConfig.TurnStartSkills.Count)
            {
                var _currentSkill = GameConfig.TurnStartSkills[_stage];
                if (_sourcePlayer.HasSkill(_currentSkill) && SkillConditionStatisfied(_currentSkill))
                {
                    GameMessage = GameText.SkillActivated(_sourcePlayer.Name, _currentSkill);
                    GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TurnStart, SourcePlayerId= _sourcePlayerId, Stage = _stage + 1 });
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
            _sourcePlayer.SetNumber(numberChangeIndex, numberChangeValue);
            GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.ApplyingEffect, SourcePlayerId = _sourcePlayerId });
        }

        private void ChangeBothNumbers(int firstNumberChangeValue, int secondNumberChangeValue)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
            _sourcePlayer.SetNumber(0, firstNumberChangeValue);
            _sourcePlayer.SetNumber(1, secondNumberChangeValue);
            GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.ApplyingEffect, SourcePlayerId = _sourcePlayerId });
        }


        /// <summary>
        /// 处理选择数字的逻辑
        /// </summary>
        /// <param name="action"></param>
        private void HandleChooseNumberIndex(Action action)
        {
            var _stage = CurrentGamePhase.Stage;
            if (_stage == 0)
            {
                var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
                var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
                GameMessage = GameText.ChooseFirstNumberIndex(_sourcePlayer.Name);
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == CurrentGamePhase.SourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.ChooseNumberIndex, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = action.NumberIndex, Stage = 1 };
                }
            }
            else if (_stage == 1)
            {
                var _sourcPlayerId = CurrentGamePhase.SourcePlayerId;
                var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcPlayerId);
                GameMessage = GameText.ChooseSecondNumberIndex(_sourcePlayer.Name);
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex)
                {
                    if (action.TargetPlayerId == _sourcPlayerId && action.NumberIndex == CurrentGamePhase.SourceNumberIndex)
                    {
                        return; // 不能选择相同数字
                    }

                    var otherPlayerSelected = Players.FirstOrDefault(p => p.PlayerId == action.TargetPlayerId);
                    if (otherPlayerSelected == null || otherPlayerSelected.IsAlive() == false)
                    {
                        return; // 选择的目标为空或者已经死亡
                    }

                    var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
                    int _newCalculatedNumber = (_sourcePlayer.Numbers[_sourceNumberIndex] + otherPlayerSelected.Numbers[action.NumberIndex]) % 10;
                    GameMessage = $"玩家 {_sourcePlayer.Name} 的数字 {_sourcePlayer.Numbers[action.NumberIndex]} 变为 {_newCalculatedNumber}. 正在检查效果...";
                    ChangeNumber(_sourceNumberIndex, _newCalculatedNumber);
                    CurrentGamePhase = new GamePhase();
                }
            }
        }


        /// <summary>
        /// 处理特殊效果逻辑
        /// </summary>
        private void HandleSpecialEffects()
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
            int _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
            var _numbers = _sourcePlayer.Numbers;
            // 攻击 (数字变为 8 或 9)
            if (_numbers[0] == 8 || _numbers[0] == 9)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的第一个数字变为变为 {_numbers[0]}, 触发攻击! (造成1点伤害)";
                ChangeNumber(0, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 1, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }

            else if (_numbers[1] == 8 || _numbers[1] == 9)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的第二个数字变为变为 {_numbers[0]}, 触发攻击! (造成1点伤害)";
                ChangeNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 1, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }

            // 攻击 (数字变为 1 和 6)
            else if ((_sourcePlayer.Numbers[0] == 1 && _sourcePlayer.Numbers[1] == 6) || (_sourcePlayer.Numbers[0] == 6 && _sourcePlayer.Numbers[1] == 1))
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的两个数字变为 1 和 6, 触发攻击! (造成2点伤害)";
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 2, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }

            // 攻击 (数字变为 5 和 5)
            else if (_sourcePlayer.Numbers[0] == 5 && _sourcePlayer.Numbers[1] == 5)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的两个数字变为 5 和 5, 触发攻击! (造成3点伤害)";
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 3, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }

            // 攻击 (数字变为 6 和 6)
            else if (_sourcePlayer.Numbers[0] == 6 && _sourcePlayer.Numbers[1] == 6)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的两个数字变为 6 和 6, 触发攻击! (造成5点伤害)";
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 5, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }

            // 治愈 (数字变为 7)
            else if (_numbers[0] == 7)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的数字变为 7, 触发治愈! (恢复1点生命值)";
                ChangeNumber(0, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, HealingValue = 1, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }
            else if (_numbers[1] == 7)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的数字变为 7, 触发治愈! (恢复1点生命值)";
                ChangeNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, HealingValue = 1, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }

            // 治愈 (两个数字都变为 0)
            else if (_sourcePlayer.Numbers[0] == 0 && _sourcePlayer.Numbers[1] == 0)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的两个数字都变为 0, 触发治愈! (恢复10点生命值)";
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, HealingValue = 10, SourcePlayerId = _sourcePlayerId, Numbers = _numbers });
            }

            CurrentGamePhase = new GamePhase();
        }




        /// <summary>
        /// 发起攻击指令。
        /// </summary>
        private void HandleOrderingAttack()
        {
            var sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);
            var stage = CurrentGamePhase.Stage;

            // GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.AttackFinished });

            if (stage < GameConfig.OrderingAttackSkills.Count)
            {
                var currentSkill = GameConfig.OrderingAttackSkills[stage];
                if (sourcePlayer.HasSkill(currentSkill) && SkillConditionStatisfied(currentSkill))
                {
                    GameMessage = GameText.SkillActivated(sourcePlayer.Name, currentSkill);
                    CurrentGamePhase = new GamePhase { Type = GamePhaseOfSkill(currentSkill), SourcePlayerId = sourcePlayerId, DamageValue = CurrentGamePhase.DamageValue };
                    return;
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
        }

        public bool IsDamagingButHealing()
        {
            if (CurrentGamePhase.DamageValue > 0)
            {
                return true;
            }
            return false;
        }




        /// <summary>
        /// 处理目标选择阶段的逻辑
        /// </summary>
        /// <param name="action"></param>
        private void HandleTargetSelection(Action action)
        {
            if (IsDamagingButHealing())
            {
                GameMessage = GameText.ChooseTargetForDamaging;
            }
            else
            {
                GameMessage = GameText.ChooseTargetForHealing;
            }
            CurrentActionType = ActionType.ChooseTarget;
            if (action.Type == ActionType.ChooseTarget)
            {
                var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
                var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
                var _targetPlayerId = action.TargetPlayerId;
                var _targetPlayer = Players.FirstOrDefault(p => p.PlayerId == _targetPlayerId);

                if (_targetPlayer == null)
                {
                    return; // 选择的目标为空
                }


                if (IsDamagingButHealing())
                {
                    int _damageValue = CurrentGamePhase.DamageValue;
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.Damaging, SourcePlayerId = _sourcePlayerId, TargetPlayerId = _targetPlayerId, DamageValue = _damageValue };
                    return;
                }
                else 
                {
                    int _healingValue = CurrentGamePhase.HealingValue;
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.Healing, SourcePlayerId = _sourcePlayerId, TargetPlayerId = _targetPlayerId, HealingValue = _healingValue };
                    return;
                }


            }
        }

        /// <summary>
        /// 处理攻击阶段的逻辑
        /// </summary>
        private void HandleDamaging()
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
                GameMessage = GameText.ShieldBreak(targetPlayer.Name);
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.ApplyingEffect, SourcePlayerId = targetPlayer.PlayerId, SourceNumberIndex = _shieldIndex };
            }
            else
            {
                if (targetPlayer.HasSkill(Skill.DoubleDefence))
                {
                    _damageValue /= 2;
                }

                targetPlayer.TakeDamage(_damageValue);
                GameMessage = GameText.Attacked(targetPlayer.Name, _damageValue);

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
        }

        /// <summary>
        /// 处理治疗阶段的逻辑
        /// </summary>
        private void HandleHealing()
        {
            int _healingValue = CurrentGamePhase.HealingValue;
            var targetPlayer = Players.FirstOrDefault(p => p.PlayerId == CurrentGamePhase.TargetPlayerId);
            targetPlayer.Heal(_healingValue);
            GameMessage = $"玩家 {targetPlayer.PlayerId} 被治疗 {_healingValue} 点生命值. ";
            CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleStack };
        }

        /// <summary>
        /// 处理双倍攻击技能的逻辑
        /// </summary>
        /// <param name="action"></param>
        private void HandleSkillDoubleAttack(Action action)
        {
            var name = Players.FirstOrDefault(p => p.PlayerId == CurrentGamePhase.SourcePlayerId)?.Name;
            GameMessage = GameText.SkillActivated(name, Skill.DoubleAttack);
            CurrentActionType = ActionType.DecideApply;
            if (action.Type == ActionType.DecideApply)
            {
                var damageValue = CurrentGamePhase.DamageValue;
                if (action.IsApply)
                {
                    damageValue *= 2;
                }
                int stage = GameConfig.OrderingAttackSkills.IndexOf(Skill.DoubleAttack);
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.OrderingAttack, SourcePlayerId = CurrentGamePhase.SourcePlayerId, DamageValue = damageValue, Stage = stage + 1 };
            }
        }


        private void HandleSkillLong(Action action)
        {
            var sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == CurrentGamePhase.SourcePlayerId);
            GameMessage = GameText.SkillActivated(sourcePlayer.Name, Skill.Long);
            CurrentActionType = ActionType.DecideApply;
            if (action.Type == ActionType.DecideApply)
            {
                var damageValue = CurrentGamePhase.DamageValue;
                if (action.IsApply)
                {
                    damageValue = sourcePlayer.Numbers[0] + sourcePlayer.Numbers[1];
                }
                int stage = GameConfig.OrderingAttackSkills.IndexOf(Skill.Long);
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.OrderingAttack, SourcePlayerId = CurrentGamePhase.SourcePlayerId, DamageValue = damageValue, Stage = stage + 1 };
            }
        }


        /// <summary>
        /// 处理神技能的逻辑
        /// </summary>
        /// <param name="action"></param>
        private void HandleSkillShen(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
            if (CurrentGamePhase.Stage == 0)
            {
                GameMessage = GameText.SkillActivatedNumberTurnFrom(_sourcePlayer.Name, Skill.Shen);
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == CurrentGamePhase.SourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillShen, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex, Stage = 1 };
                }

            }
            else if (CurrentGamePhase.Stage == 1)
            {
                GameMessage = GameText.SkillActivatedNumberTurnInto(_sourcePlayer.Name, Skill.Shen);
                CurrentActionType = ActionType.ChooseNumber;
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
        }

        private void HandleSkillXian(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
            if (CurrentGamePhase.Stage == 0)
            {
                GameMessage = GameText.SkillActivatedNumberTurnFrom(_sourcePlayer.Name, Skill.Xian);
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == CurrentGamePhase.SourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillXian, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = action.NumberIndex, Stage = 1 };
                }

            }
            else if (CurrentGamePhase.Stage == 1)
            {
                GameMessage = GameText.SkillActivatedNumberTurnInto(_sourcePlayer.Name, Skill.Xian);
                CurrentActionType = ActionType.ChooseNumber;
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
        }


        private void HandleSkillMo()
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _damageValue = CurrentGamePhase.DamageValue;
            CurrentGamePhase = new GamePhase { Type = GamePhaseType.Healing, TargetPlayerId = _sourcePlayerId, HealingValue = _damageValue };
        }



        private void HandleSkillDi(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);

            if (CurrentGamePhase.Stage == 0)
            {
                GameMessage = GameText.SkillActivatedNumberTurnFrom(_sourcePlayer.Name, Skill.Di);
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == CurrentGamePhase.SourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillDi, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex, Stage = 1 };
                }

            }
            else if (CurrentGamePhase.Stage == 1)
            {
                GameMessage = GameText.SkillActivatedNumberTurnInto(_sourcePlayer.Name, Skill.Di);
                CurrentActionType = ActionType.ChooseNumber;
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
        }

        /// <summary>
        /// 处理同变技能的逻辑
        /// </summary>
        /// <param name="action"></param>
        private void HandleSkillTong(Action action)
        {
            var stage = CurrentGamePhase.Stage;
            if (stage == 0)
            {
                var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == CurrentGamePhase.SourcePlayerId);
                GameMessage = GameText.SkillActivatedUseIf(_sourcePlayer.Name, Skill.Tong);
                CurrentActionType = ActionType.DecideApply;
                if (action.Type == ActionType.DecideApply)
                {
                    var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
                    if (action.IsApply)
                    {
                        CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillTong, SourcePlayerId = _sourcePlayerId, Stage = 1 };
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
                if (action.Type == ActionType.ChooseNumber)
                {
                    int _newNumber = action.Number;
                    ChangeBothNumbers(_newNumber, _newNumber);
                    CurrentGamePhase = new GamePhase();
                }
            }
        }

        /// <summary>
        /// 处理玩家回合结束的逻辑
        /// </summary>
        private void HandlePlayerTurnEnd()
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
                GameMessage = GameText.TurnStart(_currentPlayer.Name);
            }
            else
            {
                // 理论上, 这种情况应该在游戏结束检查中捕获, 但作为后备
                GameMessage = GameText.GameEnd;
            }
        }


    }
}
