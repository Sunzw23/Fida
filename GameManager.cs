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
        TargetSelection,        // 如果效果需要, 选择攻击或治疗目标
        PlayerTurnEnd,          // 玩家回合结束
        Healing,                // 玩家进行治疗
        Attacking,              // 玩家进行攻击
        HandleSkillDoubleAttack,      // 玩家使用技能: 双倍攻击
        HandleSkillShen,              // 玩家使用技能: 神
        HandleSkillMo,                // 玩家使用技能: 魔
        HandleSkillXian,              // 玩家使用技能: 仙
        HandleSkillTong,              // 玩家使用技能: 同变
        HandleSkillDi,                // 玩家使用技能: 帝
    }

    public struct GamePhase
    {
        public GamePhaseType Type;
        public int SourcePlayerId { get; set; }
        public int TargetPlayerId { get; set; }
        public int SourceNumberIndex { get; set; }
        public int TargetNumberIndex { get; set; }
        public int DamageValue { get; set; }
        public int HealingValue { get; set; }
        public int NewNumber { get; set; }
        public int Stage { get; set; }

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
        public List<Character> Players { get; private set; } = [];// 所有玩家列表
        public int CurrentPlayerIndex { get; private set; } // 当前活跃玩家的索引
        public GamePhase CurrentGamePhase { get; private set; } // 当前游戏阶段
        public ActionType CurrentActionType { get; private set; } // 当前操作类型
        public string GameMessage { get; private set; } // 显示给玩家的游戏消息

        public Stack<GamePhase> GamePhaseStack { get; private set; } // 用于保存游戏阶段的栈
        
        


        private ResourceManager _resourceManager; // 资源管理器

        // 构造函数
        public GameManager(ResourceManager resourceManager)
        {
            Players = [];
            GamePhaseStack = new Stack<GamePhase>();
            _resourceManager = resourceManager;
        }

        /// <summary>
        /// 初始化游戏状态和玩家. 
        /// </summary>
        public void InitializeGame()
        {
        }

        public void LoadScene1()
        {
            Players.Clear();
            Character Player1 = new Character(0, 6, _resourceManager.Font, new Vector2(960, 800));
            //Player1.Skills.Add(new Skill { SkillType = SkillType.DoubleAttack, SkillName = "双倍攻击" });
            //Player1.Skills.Add(new Skill { SkillType = SkillType.Shen, SkillName = "神" });
            //Player1.Skills.Add(new Skill { SkillType = SkillType.Tong, SkillName = "同变" });
            //Player1.Skills.Add(new Skill { SkillType = SkillType.Mo, SkillName = "魔" });
            //Player1.Skills.Add(new Skill { SkillType = SkillType.Di, SkillName = "帝" });
            Character Player2 = new Character(1, 6, _resourceManager.Font, new Vector2(460, 200));
            //Player2.Skills.Add(new Skill { SkillType = SkillType.DoubleDefence, SkillName = "双倍防御" });
            //Character Player3 = new Character(2, 6, _resourceManager.Font, new Vector2(1460, 200));
            //Character Player2 = new Character(
            //    id: 1, 
            //    health: 6,
            //    name: "King of the Old",
            //    font: _resourceManager.Font, 
            //    fontColor : Color.Gold,
            //    position: new Vector2(960, 200),
            //    texture2Ds: new List<Texture2D> { _resourceManager.GetTexture("oldking") },
            //    texture2DLocations: new List<Vector2> { new Vector2(-400, -200) },
            //    numberLocations: new List<Vector2> { new Vector2(-400, 100), new Vector2(400, 100) },
            //    healthLocation: new Vector2(0, -100)
            //    );
            //Player2.Texture2Ds.Add(_resourceManager.GetTexture("oldking"));
            //Player2.Texture2DLocations.Add(new Vector2(-400, -200));

            Players = [Player1, Player2];
            CurrentPlayerIndex = 0;
            var CurrentPlayer = Players[CurrentPlayerIndex];
            CurrentGamePhase = new GamePhase { Type = GamePhaseType.GameStart, SourcePlayerId = CurrentPlayer.PlayerId };
            GameMessage = $"玩家 {CurrentPlayer.PlayerId} 的回合.";
        }



        /// <summary>
        /// 更新游戏逻辑. 
        /// </summary>
        /// <param name="mouseClickedArea">鼠标点击的区域, 由UIManager提供. </param>
        /// <param name="isLeftClicked">鼠标左键是否被点击. </param>
        public void UpdateGame(Action action)
        {
            // 检查是否有获胜者 (只剩一个玩家)
            var alivePlayers = Players.Where(p => p.IsAlive()).ToList();
            if (alivePlayers.Count <= 1)
            {
                if (alivePlayers.Count == 1)
                {
                    GameMessage = $"游戏结束! 玩家 {alivePlayers[0].PlayerId} 获胜!";
                }
                else
                {
                    GameMessage = "游戏结束! 没有获胜者. "; // 所有玩家都出局
                }
                return; // 游戏结束, 不再处理输入
            }

            Character currentPlayer = Players[CurrentPlayerIndex];

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

                case GamePhaseType.Attacking:
                    HandleAttacking();
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
                    HandleSkillMo(action);
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

                default:
                    HandleStack();
                    break;
            }
        }

        private void HandleGameStart()
        {
            GameMessage = "游戏开始! ";

            foreach (var player in Players)
            {
                if (player.HasSkill(SkillType.TripleHealth))
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

        bool _checkedSkillTong = false;
        bool _checkedSkillDi = false;
        private void HandleTurnStart()
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
            if (_sourcePlayer.HasSkill(SkillType.Tong) && !_checkedSkillTong && _sourcePlayer.Numbers[0] == 3 && _sourcePlayer.Numbers[1] == 3)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 触发技能: 同变. ";
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillTong, SourcePlayerId = _sourcePlayerId };
                _checkedSkillTong = true;
            }
            else if (_sourcePlayer.HasSkill(SkillType.Di)&&!_checkedSkillDi)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 触发技能: 帝. ";
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillDi, SourcePlayerId = _sourcePlayerId };
                _checkedSkillDi = true;
            }
            else
            {
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.ChooseNumberIndex, SourcePlayerId = _sourcePlayerId };
                _checkedSkillTong = false;
                _checkedSkillDi = false;
            }

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
            var _cur_stage = CurrentGamePhase.Stage;
            if (_cur_stage == 0)
            {
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == CurrentGamePhase.SourcePlayerId)
                {
                    var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
                    var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcePlayerId);
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.ChooseNumberIndex, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = action.NumberIndex, Stage = 1 };

                    GameMessage = $"玩家 {_sourcePlayerId} 选择了自己的数字 {_sourcePlayer.Numbers[action.NumberIndex]}. 现在选择另一个数字. ";
                }
            }
            else if (_cur_stage == 1)
            {
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex)
                {
                    var _sourcPlayerId = CurrentGamePhase.SourcePlayerId;
                    var _sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == _sourcPlayerId);
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
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 1, SourcePlayerId = _sourcePlayerId });
            }

            else if (_numbers[1] == 8 || _numbers[1] == 9)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的第一个数字变为变为 {_numbers[0]}, 触发攻击! (造成1点伤害)";
                ChangeNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 1, SourcePlayerId = _sourcePlayerId });
            }

            // 攻击 (数字变为 1 和 6)
            else if ((_sourcePlayer.Numbers[0] == 1 && _sourcePlayer.Numbers[1] == 6) || (_sourcePlayer.Numbers[0] == 6 && _sourcePlayer.Numbers[1] == 1))
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的两个数字变为 1 和 6, 触发攻击! (造成2点伤害)";
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 2, SourcePlayerId = _sourcePlayerId });
            }

            // 攻击 (数字变为 5 和 5)
            else if (_sourcePlayer.Numbers[0] == 5 && _sourcePlayer.Numbers[1] == 5)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的两个数字变为 5 和 5, 触发攻击! (造成3点伤害)";
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 3, SourcePlayerId = _sourcePlayerId });
            }

            // 攻击 (数字变为 6 和 6)
            else if (_sourcePlayer.Numbers[0] == 6 && _sourcePlayer.Numbers[1] == 6)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的两个数字变为 6 和 6, 触发攻击! (造成5点伤害)";
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.OrderingAttack, DamageValue = 5, SourcePlayerId = _sourcePlayerId });
            }

            // 治愈 (数字变为 7)
            else if (_numbers[0] == 7)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的数字变为 7, 触发治愈! (恢复1点生命值)";
                ChangeNumber(0, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, HealingValue = 1, SourcePlayerId = _sourcePlayerId });
            }
            else if (_numbers[1] == 7)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的数字变为 7, 触发治愈! (恢复1点生命值)";
                ChangeNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, HealingValue = 1, SourcePlayerId = _sourcePlayerId });
            }

            // 治愈 (两个数字都变为 0)
            else if (_sourcePlayer.Numbers[0] == 0 && _sourcePlayer.Numbers[1] == 0)
            {
                GameMessage = $"玩家 {_sourcePlayer.PlayerId} 的两个数字都变为 0, 触发治愈! (恢复10点生命值)";
                _sourcePlayer.SetNumber(0, 1);
                _sourcePlayer.SetNumber(1, 1);
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, HealingValue = 10, SourcePlayerId = _sourcePlayerId });
            }

            CurrentGamePhase = new GamePhase();
        }

        bool _checkedSkillDoubleAttack = false;



        /// <summary>
        /// 发起攻击指令。
        /// </summary>
        /// <param name="action"></param>
        private void HandleOrderingAttack()
        {
            var sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var sourcePlayer = Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);

            

            if (sourcePlayer.HasSkill(SkillType.DoubleAttack) && !_checkedSkillDoubleAttack)
            {
                _checkedSkillDoubleAttack = true;
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillDoubleAttack, SourcePlayerId = sourcePlayerId, DamageValue = CurrentGamePhase.DamageValue };
            }
            else
            {
                if (sourcePlayer.HasSkill(SkillType.Xian))
                {
                    GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.HandleSkillXian, SourcePlayerId = sourcePlayerId });
                }
                _checkedSkillDoubleAttack = false;
                GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.TargetSelection, SourcePlayerId = sourcePlayerId, DamageValue = CurrentGamePhase.DamageValue });
                CurrentGamePhase = new GamePhase();
            }

        }

        




        /// <summary>
        /// 处理目标选择阶段的逻辑
        /// </summary>
        /// <param name="action"></param>
        private void HandleTargetSelection(Action action)
        {
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

                if (GameMessage.Contains("治愈"))
                {
                    int _healingValue = CurrentGamePhase.HealingValue;
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.Healing, SourcePlayerId = _sourcePlayerId, TargetPlayerId = _targetPlayerId, HealingValue = _healingValue };
                    return;
                }

                if (GameMessage.Contains("攻击"))
                {
                    int _damageValue = CurrentGamePhase.DamageValue;
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.Attacking, SourcePlayerId = _sourcePlayerId, TargetPlayerId = _targetPlayerId, DamageValue = _damageValue };
                    return;
                }
            }
        }

        /// <summary>
        /// 处理攻击阶段的逻辑
        /// </summary>
        private void HandleAttacking()
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
                GameMessage = $"玩家 {targetPlayer.PlayerId} 的护盾被打破, 伤害被抵消!";
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.ApplyingEffect, SourcePlayerId = targetPlayer.PlayerId, SourceNumberIndex = _shieldIndex };
            }
            else
            {
                if (targetPlayer.HasSkill(SkillType.DoubleDefence))
                {
                    _damageValue /= 2;
                }

                targetPlayer.TakeDamage(_damageValue);
                GameMessage = $"玩家 {targetPlayer.PlayerId} 被攻击 {_damageValue} 点生命值. ";
                if (targetPlayer.HasSkill(SkillType.Shen))
                {
                    GamePhaseStack.Push(new GamePhase { Type = GamePhaseType.HandleSkillShen, SourcePlayerId = targetPlayer.PlayerId });
                }

                if (sourcePlayer.HasSkill(SkillType.Mo))
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
            CurrentActionType = ActionType.DecideApply;
            if (action.Type == ActionType.DecideApply)
            {
                var damageValue = CurrentGamePhase.DamageValue;
                if (action.IsApply)
                {
                    damageValue *= 2;
                }
                _checkedSkillDoubleAttack = true;
                CurrentGamePhase = new GamePhase { Type = GamePhaseType.OrderingAttack, SourcePlayerId = CurrentGamePhase.SourcePlayerId, DamageValue = damageValue };
            }
        }


        bool _checkedSkillShenChooseNumberIndex = false;
        bool _checkedSkillShenChooseNumber = false;
        /// <summary>
        /// 处理神技能的逻辑
        /// </summary>
        /// <param name="action"></param>
        private void HandleSkillShen(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;

            if (!_checkedSkillShenChooseNumberIndex)
            {
                GameMessage = $"玩家{CurrentGamePhase.SourcePlayerId}受到攻击触发神. 请选择一个数字进行变化. ";
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == CurrentGamePhase.SourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillShen, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex };
                    _checkedSkillShenChooseNumberIndex = true;
                }

            }
            else if (!_checkedSkillShenChooseNumber)
            {
                GameMessage = $"玩家{CurrentGamePhase.SourcePlayerId}受到攻击触发神. 请选择变成的数字. ";
                CurrentActionType = ActionType.ChooseNumber;
                if (action.Type == ActionType.ChooseNumber)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillShen, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex, NewNumber = action.Number };
                    _checkedSkillShenChooseNumber = true;
                }

            }
            else
            {
                int _newNumber = CurrentGamePhase.NewNumber;
                ChangeNumber(_sourceNumberIndex, _newNumber);
                _checkedSkillShenChooseNumberIndex = false;
                _checkedSkillShenChooseNumber = false;
                CurrentActionType = ActionType.None;
                CurrentGamePhase = new GamePhase();
            }
        }

        bool _checkedSkillXianChooseNumberIndex = false;
        bool _checkedSkillXianChooseNumber = false;
        private void HandleSkillXian(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;
            if (!_checkedSkillXianChooseNumberIndex)
            {
                GameMessage = $"玩家{CurrentGamePhase.SourcePlayerId}进行攻击触发仙. 请选择一个数字进行变化. ";
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == CurrentGamePhase.SourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillXian, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = action.NumberIndex };
                    _checkedSkillXianChooseNumberIndex = true;
                }

            }
            else if (!_checkedSkillXianChooseNumber)
            {
                GameMessage = $"玩家{CurrentGamePhase.SourcePlayerId}进行攻击触发仙. 请选择变成的数字. ";
                CurrentActionType = ActionType.ChooseNumber;
                if (action.Type == ActionType.ChooseNumber)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillXian, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex, NewNumber = action.Number };
                    _checkedSkillXianChooseNumber = true;
                }

            }
            else
            {
                int _newNumber = CurrentGamePhase.NewNumber;
                ChangeNumber(_sourceNumberIndex, _newNumber);
                _checkedSkillXianChooseNumberIndex = false;
                _checkedSkillXianChooseNumber = false;
                CurrentActionType = ActionType.None;
                CurrentGamePhase = new GamePhase();
            }
        }


        private void HandleSkillMo(Action action)
        {
            GameMessage = $"玩家{CurrentGamePhase.SourcePlayerId}发动攻击触发魔. 请选择是否使用";
            CurrentActionType = ActionType.DecideApply;
            if (action.Type == ActionType.DecideApply)
            {
                var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
                var _damageValue = CurrentGamePhase.DamageValue;
                if (action.IsApply)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.Healing, TargetPlayerId = _sourcePlayerId, HealingValue = _damageValue };
                }
                else
                {
                    CurrentGamePhase = new GamePhase();
                }
            }
        }

        bool _checkedSkillDiChooseNumberIndex = false;
        bool _checkedSkillDiChooseNumber = false;

        private void HandleSkillDi(Action action)
        {
            var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
            var _sourceNumberIndex = CurrentGamePhase.SourceNumberIndex;

            if (!_checkedSkillDiChooseNumberIndex)
            {
                GameMessage = $"玩家{CurrentGamePhase.SourcePlayerId}开始回合触发帝. 请选择一个数字进行变化. ";
                CurrentActionType = ActionType.ChooseNumberIndex;
                if (action.Type == ActionType.ChooseNumberIndex && action.TargetPlayerId == CurrentGamePhase.SourcePlayerId)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillDi, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex };
                    _checkedSkillDiChooseNumberIndex = true;
                }

            }
            else if (!_checkedSkillDiChooseNumber)
            {
                GameMessage = $"玩家{CurrentGamePhase.SourcePlayerId}开始回合触发帝. 请选择变成的数字. ";
                CurrentActionType = ActionType.ChooseNumber;
                if (action.Type == ActionType.ChooseNumber)
                {
                    CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillDi, SourcePlayerId = _sourcePlayerId, SourceNumberIndex = _sourceNumberIndex, NewNumber = action.Number };
                    _checkedSkillDiChooseNumber = true;
                }

            }
            else
            {
                int _newNumber = CurrentGamePhase.NewNumber;
                ChangeNumber(_sourceNumberIndex, _newNumber);
                _checkedSkillDiChooseNumberIndex = false;
                _checkedSkillDiChooseNumber = false;
                CurrentActionType = ActionType.None;
                CurrentGamePhase = new GamePhase();
            }
        }

        bool _checkedSkillTongDecideApply = false;
        /// <summary>
        /// 处理同变技能的逻辑
        /// </summary>
        /// <param name="action"></param>
        private void HandleSkillTong(Action action)
        {
            if (!_checkedSkillTongDecideApply)
            {
                GameMessage = $"玩家{CurrentGamePhase.SourcePlayerId}触发同变技能. 请选择是否使用";
                CurrentActionType = ActionType.DecideApply;
                if (action.Type == ActionType.DecideApply)
                {
                    var _sourcePlayerId = CurrentGamePhase.SourcePlayerId;
                    if (action.IsApply)
                    {
                        CurrentGamePhase = new GamePhase { Type = GamePhaseType.HandleSkillTong, SourcePlayerId = _sourcePlayerId };
                    }
                    else 
                    {
                        CurrentGamePhase = new GamePhase { Type = GamePhaseType.TurnStart, SourcePlayerId = _sourcePlayerId };
                    }
                    _checkedSkillTongDecideApply = true;
                }
            }
            else 
            {
                CurrentActionType = ActionType.ChooseNumber;
                if (action.Type == ActionType.ChooseNumber)
                {
                    _checkedSkillDoubleAttack = false;
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
                GameMessage = $"玩家 {Players[CurrentPlayerIndex].PlayerId} 的回合. ";
            }
            else
            {
                // 理论上, 这种情况应该在游戏结束检查中捕获, 但作为后备
                GameMessage = "游戏结束, 没有下一个玩家. ";
            }
        }
    }
}
