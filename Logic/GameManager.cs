using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// GameManager 类负责管理游戏的核心数据和状态转换。
    /// 游戏逻辑被委托给具体的 GameState 对象。
    /// </summary>
    public partial class GameManager
    {
        // --- Events ---
        public event EventHandler<GameStartEventArgs> GameStarted;
        public event EventHandler<TurnStartEventArgs> TurnStarted;
        public event EventHandler<NumberChangeEventArgs> NumberChanged;
        public event EventHandler<AttackEventArgs> AttackExecuted;
        public event EventHandler<HealEventArgs> HealExecuted;
        public event EventHandler<SkillActivatedEventArgs> SkillActivated;
        public event EventHandler<PlayerActionRequiredEventArgs> PlayerActionRequired;
        public event EventHandler<GameEndEventArgs> GameEnded;
        public event EventHandler<GameEventArgs> MessageUpdated;
        /// <summary>
        /// 游戏状态变化事件 - 当 GameState 发生变化时触发，用于通知 UI 层刷新按钮状态
        /// </summary>
        public event EventHandler<GameStateEventArgs> GameStateChanged;

        // --- Game Data ---
        public List<Player> Players { get; private set; } = [];
        public List<Group> Groups { get; private set; } = [];
        public int CurrentPlayerIndex { get; private set; }
        public string GameMessage { get; private set; }
        public ActionType CurrentActionType { get; private set; } = ActionType.None;

        /// <summary>
        /// 本地玩家 ID（单机默认 0，联机由 NetworkManager 设置）
        /// </summary>
        public int LocalPlayerId { get; set; } = 0;

        // --- State Management ---
        private GameState _currentState;
        public Stack<GameState> StateStack { get; private set; } = new();

        /// <summary>
        /// 初始化游戏并设置初始状态
        /// </summary>
        public void Initialize(List<Player> players)
        {
            Players = players;
            Groups = [.. players.Select(p => new Group(p.PlayerId, p.Name, [p]))];
            CurrentPlayerIndex = 0;
            GameMessage = string.Empty;
            _currentState = null;
            StateStack.Clear();
        }

        /// <summary>
        /// 初始化游戏并指定分组
        /// </summary>
        public void Initialize(List<Player> players, List<Group> groups)
        {
            Players = players;
            Groups = groups;
            CurrentPlayerIndex = 0;
            GameMessage = string.Empty;
            _currentState = null;
            StateStack.Clear();
        }
        
        public void StartGame()
        {
            TransitionTo(new GameStartState(this));
        }
        
        /// <summary>
        /// 主游戏循环，将逻辑委托给当前状态
        /// </summary>
        public void UpdateGame(GameAction action)
        {
            if (CheckGameEnd()) return;

            _currentState?.Handle(action);

            if (CheckGameEnd()) return;
        }


        /// <summary>
        /// 转换到新的游戏状态
        /// </summary>
        public void TransitionTo(GameState newState)
        {
            _currentState = newState;
            // 触发状态变化事件，通知 UI 层刷新按钮状态
            OnGameStateChanged(new GameStateEventArgs { NewState = newState });
            _currentState?.Enter();
        }

        /// <summary>
        /// 将一个状态压入栈中，通常用于临时状态（如技能处理）后需要返回的场景
        /// </summary>
        public void PushState(GameState state)
        {
            StateStack.Push(state);
        }

        /// <summary>
        /// 结束当前回合并切换到下一个玩家
        /// </summary>
        public void EndCurrentTurn()
        {
            int nextPlayerTrial = (CurrentPlayerIndex + 1) % Players.Count;
            while (!Players[nextPlayerTrial].IsAlive() && nextPlayerTrial != CurrentPlayerIndex)
            {
                nextPlayerTrial = (nextPlayerTrial + 1) % Players.Count;
            }

            if (Players[nextPlayerTrial].IsAlive())
            {
                CurrentPlayerIndex = nextPlayerTrial;
            }
        }

        public Player GetCurrentPlayer() => Players[CurrentPlayerIndex];


        #region Public Helper Methods (called by States)

        public void SetGameMessage(string message)
        {
            GameMessage = message;
            OnMessageUpdated(new GameEventArgs { Message = message });
        }

        public void ChangeNumber(int playerId, int numberIndex, int newValue, bool enqueueApplyingEffect = true)
        {
            var player = Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null) return;

            var oldValue = player.Numbers[numberIndex];
            player.SetNumber(numberIndex, newValue);

            OnNumberChanged(new NumberChangeEventArgs
            {
                SourcePlayerId = playerId,
                NumberIndex = numberIndex,
                OldValue = oldValue,
                NewValue = newValue
            });

            if (enqueueApplyingEffect)
            {
                PushState(new ApplyingEffectState(this, playerId));
            }
        }

        public void ChangeBothNumbers(int playerId, int firstValue, int secondValue)
        {
            var player = Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null) return;

            ChangeNumber(playerId, 0, firstValue, false);
            ChangeNumber(playerId, 1, secondValue, false);

            PushState(new ApplyingEffectState(this, playerId));

            OnNumberChanged(new NumberChangeEventArgs
            {
                SourcePlayerId = playerId,
            });
        }

        #endregion

        #region State Information for Agents
        public int CurrentPlayerId => Players.Count > 0 && CurrentPlayerIndex >= 0 && CurrentPlayerIndex < Players.Count ? Players[CurrentPlayerIndex].PlayerId : -1;
        public bool IsChooseFirstNumberIndexState()
        {
            if (_currentState is ChooseNumberIndexState chooseState && chooseState.Stage == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取第一步已选择的数字索引（仅在 ChooseNumberIndexState 的第二阶段有效）
        /// 返回 -1 表示不在第二阶段或第一步尚未完成
        /// </summary>
        public int GetFirstChosenNumberIndex()
        {
            if (_currentState is ChooseNumberIndexState chooseState && chooseState.Stage == 1)
            {
                return chooseState.FirstStepNumberIndex;
            }
            return -1;
        }

        public bool IsDamaging()
        {
            if (_currentState is TargetSelectionState targetSelectionState && (targetSelectionState.DamageValue > 0 || targetSelectionState.HealingValue < 0))
            {
                return true;
            }
            return false;
        }

        public bool IsHealing()
        {
            if (_currentState is TargetSelectionState targetSelectionState && targetSelectionState.HealingValue > 0)
            {
                return true;
            }
            return false;
        }



        #endregion

        #region Event Invokers

        public void OnGameStarted(GameStartEventArgs e) => GameStarted?.Invoke(this, e);
        public void OnTurnStarted(TurnStartEventArgs e) => TurnStarted?.Invoke(this, e);
        public void OnNumberChanged(NumberChangeEventArgs e) => NumberChanged?.Invoke(this, e);
        public void OnAttackExecuted(AttackEventArgs e) => AttackExecuted?.Invoke(this, e);
        public void OnHealExecuted(HealEventArgs e) => HealExecuted?.Invoke(this, e);
        public void OnSkillActivated(SkillActivatedEventArgs e) => SkillActivated?.Invoke(this, e);
        public void OnPlayerActionRequired(PlayerActionRequiredEventArgs e)
        {
            CurrentActionType = e.RequiredActionType;
            PlayerActionRequired?.Invoke(this, e);
        }
        public void OnGameEnded(GameEndEventArgs e) => GameEnded?.Invoke(this, e);
        public void OnMessageUpdated(GameEventArgs e) => MessageUpdated?.Invoke(this, e);
        public void OnGameStateChanged(GameStateEventArgs e) => GameStateChanged?.Invoke(this, e);

        #endregion

        /// <summary>
        /// 强制结束游戏，指定获胜者（用于联机对手断开连接时）
        /// </summary>
        public void ForceEndGame(int winnerPlayerId)
        {
            var winner = Players.FirstOrDefault(p => p.PlayerId == winnerPlayerId);
            var winnerName = winner?.Name ?? $"玩家{winnerPlayerId}";
            SetGameMessage(GameText.GameEndWithWinner(winnerName));
            OnGameEnded(new GameEndEventArgs
            {
                WinnerPlayerId = winnerPlayerId,
                Message = GameMessage
            });
        }

        /// <summary>
        /// 检查游戏是否结束
        /// </summary>
        /// <returns></returns>
        private bool CheckGameEnd()
        {
            if (Groups.Count > 0)
            {
                var aliveGroups = Groups.Where(g => g.IsAlive()).ToList();
                if (aliveGroups.Count <= 1)
                {
                    if (aliveGroups.Count == 1)
                    {
                        var winnerGroup = aliveGroups[0];
                        var winnerPlayer = winnerGroup.Players.FirstOrDefault(p => p.IsAlive());
                        SetGameMessage(GameText.GameEndWithWinner(winnerGroup.Name));
                        OnGameEnded(new GameEndEventArgs
                        {
                            WinnerPlayerId = winnerPlayer?.PlayerId ?? -1,
                            Message = GameMessage
                        });
                    }
                    else
                    {
                        SetGameMessage(GameText.GameEndWithDraw);
                        OnGameEnded(new GameEndEventArgs { IsDraw = true, Message = GameMessage });
                    }
                    return true;
                }
                return false;
            }

            var alivePlayers = Players.Where(p => p.IsAlive()).ToList();
            if (alivePlayers.Count <= 1)
            {
                if (alivePlayers.Count == 1)
                {
                    var winner = alivePlayers[0];
                    SetGameMessage(GameText.GameEndWithWinner(winner.Name));
                    OnGameEnded(new GameEndEventArgs { WinnerPlayerId = winner.PlayerId, Message = GameMessage });
                }
                else
                {
                    SetGameMessage(GameText.GameEndWithDraw);
                    OnGameEnded(new GameEndEventArgs { IsDraw = true, Message = GameMessage });
                }
                return true;
            }
            return false;
        }
    }
}
