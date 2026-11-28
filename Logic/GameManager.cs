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
        public event EventHandler<GameStartEventArgs>? GameStarted;
        public event EventHandler<TurnStartEventArgs>? TurnStarted;
        public event EventHandler<SkillActivatedEventArgs>? SkillActivated;
        public event EventHandler<PlayerActionRequiredEventArgs>? PlayerActionRequired;
        public event EventHandler<GameEndEventArgs>? GameEnded;
        public event EventHandler<GameEventArgs>? MessageUpdated;
        public event EventHandler<NumberOperationEventArgs>? NumberOperationResolved;
        public event EventHandler<EffectNumbersConsumedEventArgs>? EffectNumbersConsumed;
        public event EventHandler<CardActionEventArgs>? CardActionStarted;
        public event Action<GameAction, GameActionOrigin>? ActionSubmitted;
        /// <summary>
        /// 游戏状态变化事件 - 当 GameState 发生变化时触发，用于通知 UI 层刷新按钮状态
        /// </summary>
        public event EventHandler<GameStateEventArgs>? GameStateChanged;
        public event EventHandler? RosterChanged;

        // --- Game Data ---
        public List<Player> Players { get; private set; } = [];
        public List<Group> Groups { get; private set; } = [];
        public int CurrentPlayerIndex { get; private set; }
        /// <summary>Number of player turns begun in this battle (used by online checkpoints).</summary>
        public int TurnNumber { get; private set; }
        public string GameMessage { get; private set; } = string.Empty;
        public ActionType CurrentActionType { get; private set; } = ActionType.None;
        /// <summary>
        /// 当前步骤允许的目标玩家 ID 列表（空 = 无限制）。
        /// 由 BroadcastCurrentStep 随事件填充，UI 据此门控可选按钮，与状态机校验保持一致。
        /// </summary>
        public List<int> CurrentStepValidTargets { get; private set; } = [];

        /// <summary>
        /// 当前步骤允许选择的数字值列表（空 = 无限制，即 0-9）。
        /// 由 BroadcastCurrentStep 随事件填充，数字面板据此门控可选数字，与状态机校验保持一致。
        /// </summary>
        public List<int> CurrentStepValidNumbers { get; private set; } = [];

        /// <summary>
        /// 本地玩家 ID（单机默认 0，联机由 NetworkManager 设置）
        /// </summary>
        public int LocalPlayerId { get; set; } = 0;
        public bool IsGameEnded { get; private set; }
        private int _gameEndDeferralDepth;

        // --- State Management ---
        private GameState? _currentState;
        public GameState? CurrentState => _currentState;

        /// <summary>
        /// Effects resolved since the last number change. A single change can fire twice (8 and 6
        /// attacks, then the reset 1 pairs with the 6); the chain achievement reads this.
        /// </summary>
        public int ChainDepth { get; set; }

        /// <summary>
        /// 状态信息查询对象 — 集中封装对当前状态/当前回合玩家的只读查询
        /// </summary>
        public GameQuery Query { get; private set; }

        public GameManager()
        {
            Query = new GameQuery(this);
        }

        public Stack<GameState> StateStack { get; private set; } = new();

        /// <summary>
        /// 初始化游戏并设置初始状态
        /// </summary>
        public void Initialize(List<Player> players)
        {
            Players = players;
            Groups = [.. players.Select(p => new Group(p.PlayerId, p.Name, [p]))];
            CurrentPlayerIndex = 0;
            TurnNumber = 0;
            GameMessage = string.Empty;
            _currentState = null;
            StateStack.Clear();
            IsGameEnded = false;
            _gameEndDeferralDepth = 0;
        }

        /// <summary>
        /// 初始化游戏并指定分组
        /// </summary>
        public void Initialize(List<Player> players, List<Group> groups)
        {
            Players = players;
            Groups = groups;
            CurrentPlayerIndex = 0;
            TurnNumber = 0;
            GameMessage = string.Empty;
            _currentState = null;
            StateStack.Clear();
            IsGameEnded = false;
            _gameEndDeferralDepth = 0;
        }

        /// <summary>
        /// Restores a network checkpoint taken between turns. The receiver resumes by
        /// beginning CurrentPlayerIndex's turn; transient skill/action states are not copied.
        /// </summary>
        public void RestoreTurnBoundary(List<Player> players, List<Group> groups,
            int currentPlayerId, int turnNumber)
        {
            if (players == null || players.Count == 0)
                throw new ArgumentException("A battle checkpoint must contain players.", nameof(players));
            int currentIndex = players.FindIndex(player => player.PlayerId == currentPlayerId);
            if (currentIndex < 0)
                throw new ArgumentException("The current player must exist in the checkpoint roster.", nameof(currentPlayerId));

            Initialize(players, groups ?? []);
            CurrentPlayerIndex = currentIndex;
            TurnNumber = Math.Max(0, turnNumber);
        }

        /// <summary>Continues a restored turn-boundary checkpoint without replaying old actions.</summary>
        public void ResumeRestoredTurn()
        {
            if (Players.Count == 0 || IsGameEnded) return;
            StateStack.Clear();
            TransitionTo(new TurnStartState(this, GetCurrentPlayer().PlayerId));
        }

        /// <summary>Resume a checkpoint captured before the current turn's first number choice.</summary>
        public void ResumeRestoredInputBoundary()
        {
            if (Players.Count == 0 || IsGameEnded) return;
            StateStack.Clear();
            TransitionTo(new ChooseNumberIndexState(this, GetCurrentPlayer().PlayerId));
        }
        
        public void StartGame()
        {
            TransitionTo(new GameStartState(this));
        }
        
        /// <summary>
        /// 主游戏循环，将逻辑委托给当前状态
        /// </summary>
        public void UpdateGame(GameAction action, GameActionOrigin origin = GameActionOrigin.LocalInput)
        {
            if (IsGameEnded) return;
            if (CheckGameEnd()) return;

            if (action.Type == ActionType.SkipTurn)
            {
                if (Players.Count == 0 || action.TargetPlayerId != GetCurrentPlayer().PlayerId) return;
                ActionSubmitted?.Invoke(action, origin);
                SkipCurrentTurn();
                return;
            }

            ActionSubmitted?.Invoke(action, origin);
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
        /// Push states so they are popped in the supplied order. StateStack is LIFO,
        /// while simultaneous skills must resolve in the player's loadout order.
        /// </summary>
        public void PushStatesInOrder(IEnumerable<GameState> states)
        {
            var ordered = states.ToList();
            for (int i = ordered.Count - 1; i >= 0; i--)
            {
                StateStack.Push(ordered[i]);
            }
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

        public Group? GetGroupForPlayer(int playerId) =>
            Groups.FirstOrDefault(g => g.Players.Any(p => p.PlayerId == playerId));

        public bool AreAllies(int firstPlayerId, int secondPlayerId)
        {
            var first = GetGroupForPlayer(firstPlayerId);
            var second = GetGroupForPlayer(secondPlayerId);
            return first != null && second != null && first.GroupId == second.GroupId;
        }

        public void AddPlayer(Player player, int groupId)
        {
            if (player == null || Players.Any(p => p.PlayerId == player.PlayerId)) return;
            Players.Add(player);
            var group = Groups.FirstOrDefault(g => g.GroupId == groupId);
            if (group == null)
            {
                group = new Group(groupId, $"Group {groupId}");
                Groups.Add(group);
            }
            group.AddPlayer(player);
            RosterChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Removes a downed campaign summon from the board. Never removes the acting
        /// player: callers remove at a round boundary so turn order stays coherent.
        /// </summary>
        public bool RemovePlayer(int playerId)
        {
            var player = Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null) return false;
            if (Players.Count > 1 && GetCurrentPlayer().PlayerId == playerId) return false;
            Players.Remove(player);
            foreach (var group in Groups) group.Players.Remove(player);
            if (CurrentPlayerIndex >= Players.Count) CurrentPlayerIndex = 0;
            RosterChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void SkipCurrentTurn()
        {
            if (IsGameEnded || Players.Count == 0) return;
            StateStack.Clear();
            TransitionTo(new PlayerTurnEndState(this, GetCurrentPlayer().PlayerId));
        }

        /// <summary>Rechecks victory and asks the active state to prompt its new controller.</summary>
        public void ReissueCurrentAction()
        {
            if (IsGameEnded || CheckGameEnd()) return;
            _currentState?.ReissueCurrentAction();
        }

        /// <summary>Delays terminal evaluation until an action presentation completes.</summary>
        public void DeferGameEndCheck() => _gameEndDeferralDepth++;

        public void ResumeGameEndCheck()
        {
            if (_gameEndDeferralDepth > 0) _gameEndDeferralDepth--;
            if (_gameEndDeferralDepth == 0 && !IsGameEnded) CheckGameEnd();
        }


        public void SetGameMessage(string message)
        {
            GameMessage = message;
            OnMessageUpdated(new GameEventArgs { Message = message });
        }

        #region Event Invokers

        public void OnGameStarted(GameStartEventArgs e) => GameStarted?.Invoke(this, e);
        public void OnTurnStarted(TurnStartEventArgs e)
        {
            TurnNumber++;
            TurnStarted?.Invoke(this, e);
        }
        public void OnSkillActivated(SkillActivatedEventArgs e) => SkillActivated?.Invoke(this, e);
        public void OnPlayerActionRequired(PlayerActionRequiredEventArgs e)
        {
            CurrentActionType = e.RequiredActionType;
            CurrentStepValidTargets = e.ValidTargets;
            CurrentStepValidNumbers = e.ValidNumbers;
            PlayerActionRequired?.Invoke(this, e);
        }
        public void OnGameEnded(GameEndEventArgs e) => GameEnded?.Invoke(this, e);
        public void OnMessageUpdated(GameEventArgs e) => MessageUpdated?.Invoke(this, e);
        public void OnNumberOperationResolved(NumberOperationEventArgs e) =>
            NumberOperationResolved?.Invoke(this, e);
        public void OnEffectNumbersConsumed(EffectNumbersConsumedEventArgs e) =>
            EffectNumbersConsumed?.Invoke(this, e);
        public void OnCardActionStarted(CardActionEventArgs e) =>
            CardActionStarted?.Invoke(this, e);
        public void OnGameStateChanged(GameStateEventArgs e) => GameStateChanged?.Invoke(this, e);

        #endregion

        /// <summary>
        /// 强制结束游戏，指定获胜者（用于联机对手断开连接时）
        /// </summary>
        public void ForceEndGame(int winnerPlayerId)
        {
            if (IsGameEnded) return;
            IsGameEnded = true;
            var winner = Players.FirstOrDefault(p => p.PlayerId == winnerPlayerId);
            var winnerName = winner?.Name ?? $"Player {winnerPlayerId}";
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
            if (IsGameEnded) return true;
            if (_gameEndDeferralDepth > 0) return false;
            if (Groups.Count > 0)
            {
                var aliveGroups = Groups.Where(g => g.IsAlive()).ToList();
                if (aliveGroups.Count <= 1)
                {
                    // Lock the terminal state before notifying subscribers. Result
                    // handlers may synchronously switch screens or query the game.
                    IsGameEnded = true;
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
                IsGameEnded = true;
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
