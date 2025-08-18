using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida
{
    /// <summary>
    /// GameManager 类负责管理游戏的核心数据和状态转换。
    /// 游戏逻辑被委托给具体的 GameState 对象。
    /// </summary>
    public class GameManager
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

        // --- Game Data ---
        public List<Player> Players { get; private set; }
        public int CurrentPlayerIndex { get; private set; }
        public string GameMessage { get; private set; }

        // --- State Management ---
        private GameState _currentState;
        public Stack<GameState> StateStack { get; private set; }
        
        public GameManager()
        {
            Players = [];
            StateStack = new Stack<GameState>();
        }

        /// <summary>
        /// 初始化游戏并设置初始状态
        /// </summary>
        public void Initialize(List<Player> players)
        {
            Players = players;
            CurrentPlayerIndex = 0;
            GameMessage = string.Empty;
            _currentState = null;
            StateStack.Clear();
        }

        /// <summary>
        /// 主游戏循环，将逻辑委托给当前状态
        /// </summary>
        public void UpdateGame(Action action)
        {
            if (CheckGameEnd()) return;

            _currentState?.Handle(action);
        }


        /// <summary>
        /// 转换到新的游戏状态
        /// </summary>
        public void TransitionTo(GameState newState)
        {
            _currentState = newState;
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

        public void SetGameMessage(string message, int sourcePlayerId = -1)
        {
            GameMessage = message;
            OnMessageUpdated(new GameEventArgs { Message = message, SourcePlayerId = sourcePlayerId });
        }

        public void ChangeNumber(int playerId, int numberIndex, int newValue)
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
        }

        public void ChangeBothNumbers(int playerId, int firstValue, int secondValue)
        {
            var player = Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null) return;

            ChangeNumber(playerId, 0, firstValue);
            ChangeNumber(playerId, 1, secondValue);

            OnNumberChanged(new NumberChangeEventArgs
            {
                SourcePlayerId = playerId,
            });
        }

        #endregion

        #region State Information for Agents
        public int CurrentPlayerId => Players[CurrentPlayerIndex].PlayerId;
        public bool IsChooseFirstNumberIndexState()
        {
            if (_currentState is ChooseNumberIndexState chooseState && chooseState.Stage == 0)
            {
                return true;
            }
            return false;
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

        public virtual void OnGameStarted(GameStartEventArgs e) => GameStarted?.Invoke(this, e);
        public virtual void OnTurnStarted(TurnStartEventArgs e) => TurnStarted?.Invoke(this, e);
        public virtual void OnNumberChanged(NumberChangeEventArgs e) => NumberChanged?.Invoke(this, e);
        public virtual void OnAttackExecuted(AttackEventArgs e) => AttackExecuted?.Invoke(this, e);
        public virtual void OnHealExecuted(HealEventArgs e) => HealExecuted?.Invoke(this, e);
        public virtual void OnSkillActivated(SkillActivatedEventArgs e) => SkillActivated?.Invoke(this, e);
        public virtual void OnPlayerActionRequired(PlayerActionRequiredEventArgs e) => PlayerActionRequired?.Invoke(this, e);
        public virtual void OnGameEnded(GameEndEventArgs e) => GameEnded?.Invoke(this, e);
        public virtual void OnMessageUpdated(GameEventArgs e) => MessageUpdated?.Invoke(this, e);

        #endregion

        private bool CheckGameEnd()
        {
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
