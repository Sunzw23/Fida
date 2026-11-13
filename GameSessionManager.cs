#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using Fida.Agent;
using Fida.Logic;
using Fida.Network;
using Fida.Scene;
using Fida.UI;
using Fida.Campaign;
using Microsoft.Xna.Framework.Input;

namespace Fida
{
    /// <summary>
    /// 游戏会话管理 - 负责单机和联机游戏的初始化与输入处理
    /// 支持 PvP（与好友对战）和 Campaign Co-op（联机战役：独立选关、双人配装、增援）两种联机模式
    /// </summary>
    public class GameSessionManager
    {
        private readonly UIManager _uiManager;
        private readonly PlayingSceneManager _playingSceneManager;
        private readonly NetworkManager _networkManager;
        private readonly GameStateMachine _stateMachine;
        private KeyboardState _tutorialPreviousKeyboard;
        private readonly HashSet<string> _tutorialHintsShown = new();
        private string _tutorialTrackedNodeId = string.Empty;
        private int _tutorialTrackedRosterSize;

        private bool _networkGameStarted;
        private string _campaignSessionId = string.Empty;
        private string _pendingBattleJoinSession = string.Empty;
        private bool _awaitingBattleJoinAcknowledgement;
        private bool _controlsCampaignAgents;
        private bool _disconnectForfeitPending;
        private bool _campaignDisconnectPending;
        private readonly ConcurrentQueue<string> _pendingNetworkDisconnects = new();

        public GameSessionManager(UIManager uiManager, PlayingSceneManager playingSceneManager,
            NetworkManager networkManager, GameStateMachine stateMachine)
        {
            _uiManager = uiManager;
            _playingSceneManager = playingSceneManager;
            _networkManager = networkManager;
            _stateMachine = stateMachine;

            // 监听网络断开事件，在游戏中时强制结束游戏
            _networkManager.Disconnected += OnNetworkDisconnected;
            _networkManager.BattleJoinRequested += OnBattleJoinRequested;
            _networkManager.BattleSnapshotAcknowledged += OnBattleSnapshotAcknowledged;
            _networkManager.CampaignPresenceReceived += OnCampaignPresenceReceived;
            _uiManager.GameManager.GameEnded += OnGameEnded;
            _uiManager.GameManager.ActionSubmitted += OnGameActionSubmitted;
            _uiManager.GameManager.PlayerActionRequired += OnTutorialActionRequired;
            _uiManager.GameManager.MessageUpdated += OnTutorialMessageUpdated;
        }

        private void OnGameEnded(object? sender, GameEndEventArgs e)
        {
            bool rulesOverPlaying = _stateMachine.CurrentState == State.Rules &&
                _stateMachine.RulesReturnState == State.Playing;
            bool rulesOverOnlinePlaying = _stateMachine.CurrentState == State.Rules &&
                _stateMachine.RulesReturnState == State.OnlinePlaying;
            bool localBattle = _stateMachine.CurrentState == State.Playing || rulesOverPlaying;
            bool activeOnlineBattle = _networkGameStarted &&
                (_stateMachine.CurrentState is State.OnlinePlaying or State.Paused || rulesOverOnlinePlaying);
            bool onlineCampaign = activeOnlineBattle && _networkManager.CurrentGameMode == 2;
            bool onlineDuel = activeOnlineBattle && _networkManager.CurrentGameMode == 0;
            bool onlineCoop = activeOnlineBattle && _networkManager.CurrentGameMode == 1;
            if (!localBattle && !onlineCampaign && !onlineDuel && !onlineCoop) return;
            var game = _uiManager.GameManager;
            bool won = !e.IsDraw && game.AreAllies(e.WinnerPlayerId, game.LocalPlayerId);
            string resultMessage = e.Message;
            if (_disconnectForfeitPending)
            {
                won = true;
                resultMessage = "Opponent disconnected. Victory awarded.";
            }
            else if (_campaignDisconnectPending)
            {
                won = false;
                resultMessage = "Partner disconnected. This node was not cleared.";
            }
            if (won && (localBattle || onlineCampaign))
                CampaignRuntime.Complete(GameSetup.SelectedNode, onlineCampaign ? CampaignRuntime.Online : CampaignRuntime.Local);
            _stateMachine.SetLastBattleOutcome(new Logic.BattleOutcome(won, resultMessage, onlineDuel));
            _disconnectForfeitPending = false;
            _campaignDisconnectPending = false;
            _playingSceneManager.IsInitialized = false;
            _networkGameStarted = false;
            if (onlineCampaign)
            {
                _networkManager.IsCampaignSharedBattle = false;
                _networkManager.SendCampaignPresence(new CampaignPresence(_networkManager.GetLocalPlayerId(),
                    CampaignOnlinePhase.Map, string.Empty, false));
            }
            _stateMachine.SwitchToState(State.GameOver);
        }

        /// <summary>
        /// 网络断开时处理：
        /// - 如果在联机游戏中，强制结束游戏并宣布本地玩家获胜
        /// - 如果在大厅中，返回主菜单
        /// </summary>
        private void OnNetworkDisconnected(string message) =>
            _pendingNetworkDisconnects.Enqueue(message ?? string.Empty);

        private void OnCampaignPresenceReceived(CampaignPresence presence)
        {
            if (_networkManager.CurrentGameMode != 2 || presence.CompletedNodes == null) return;
            if (CampaignRuntime.MergeOnlineProgress(presence.CompletedNodes))
                _networkManager.RepublishCampaignPresence();
        }

        /// <summary>Apply socket events on the game thread so disconnects cannot race UI/state updates.</summary>
        public void ProcessPendingNetworkEvents()
        {
            while (_pendingNetworkDisconnects.TryDequeue(out _))
            {
                if (_stateMachine.CurrentState == State.OnlinePlaying ||
                    (_stateMachine.CurrentState == State.Rules &&
                     _stateMachine.RulesReturnState == State.OnlinePlaying) ||
                    (_stateMachine.CurrentState == State.Paused && _networkGameStarted))
                {
                    if (_networkManager.CurrentGameMode == 2 && !_networkManager.IsCampaignSharedBattle)
                        continue; // independent campaign battle continues locally

                    if (_networkManager.CurrentGameMode == 2 && _networkManager.IsCampaignSharedBattle)
                    {
                        // Drain actions sent immediately before the socket closed so the
                        // surviving peer resumes from the same authoritative state.
                        var game = _uiManager.GameManager;
                        _networkManager.ProcessMessages(game);
                        if (game.IsGameEnded) continue;

                        _networkManager.IsCampaignSharedBattle = false;
                        _networkManager.IsCampaignBattleContinuingLocally = true;
                        _controlsCampaignAgents = true;
                        _campaignDisconnectPending = false;
                        _playingSceneManager.ContinueCampaignAfterPartnerDisconnect();
                        continue;
                    }

                    _disconnectForfeitPending = _networkManager.CurrentGameMode == 0;
                    _campaignDisconnectPending = _networkManager.CurrentGameMode == 2;
                    _playingSceneManager.HandleRemotePlayerDisconnect();
                }
                else if (_stateMachine.CurrentState == State.OnlineLobby ||
                    _stateMachine.CurrentState == State.DuelDraft)
                {
                    _networkManager.Cleanup();
                    _stateMachine.GoToMainMenu();
                }
                else if (_stateMachine.CurrentState == State.Connecting || _stateMachine.CurrentState == State.Hosting)
                {
                    // Relay rejected the room code, the room was full, or connection failed.
                    _networkManager.Cleanup();
                    _stateMachine.SwitchToState(State.OnlineMenu);
                }
            }
        }

        /// <summary>
        /// 确保单机游戏已初始化
        /// </summary>
        public void EnsureGameStarted()
        {
            // A battle can end while HandlePlayerInput is still unwinding. Never
            // rebuild gameplay UI after the state machine has entered results.
            if (_stateMachine.CurrentState != State.Playing) return;
            if (!_playingSceneManager.IsInitialized)
            {
                _playingSceneManager.InitializeCampaign(GameSetup.LoadCampaignBattle());
                _playingSceneManager.StartGame();
            }
        }

        /// <summary>
        /// 确保联机游戏已初始化
        /// </summary>
        public void EnsureOnlineGameStarted()
        {
            if (_stateMachine.CurrentState != State.OnlinePlaying) return;
            if (_networkGameStarted && !_playingSceneManager.IsInitialized)
                _networkGameStarted = false;
            if (_networkGameStarted) return;
            _networkGameStarted = true;
            _networkManager.IsCampaignBattleContinuingLocally = false;

            int localId = _networkManager.GetLocalPlayerId();
            int gameMode = _networkManager.CurrentGameMode;

            if (gameMode == 2)
            {
                bool shared = _networkManager.IsCampaignSharedBattle;
                if (shared && string.IsNullOrWhiteSpace(_networkManager.CampaignSessionId))
                {
                    _networkManager.ReportStatus("Co-op start failed: missing shared session ID.");
                    _networkGameStarted = false;
                    return;
                }
                _uiManager.GameManager.LocalPlayerId = shared ? localId : 0;
                var setup = shared
                    ? CampaignBattleFactory.CreateCooperative(GameSetup.SelectedNode,
                        localId == 0 ? GameSetup.SelectedLocalSkills : _networkManager.RemoteCampaignPresence.Skills ?? [],
                        localId == 1 ? GameSetup.SelectedLocalSkills : _networkManager.RemoteCampaignPresence.Skills ?? [])
                    : CampaignBattleFactory.Create(GameSetup.SelectedNode, GameSetup.SelectedLocalSkills);
                var pendingSnapshot = _networkManager.TakePendingBattleSnapshot();
                _controlsCampaignAgents = pendingSnapshot == null &&
                    (!shared || _networkManager.IsHost);
                if (pendingSnapshot != null)
                {
                    var template = CampaignBattleFactory.Create(GameSetup.SelectedNode, []);
                    _campaignSessionId = pendingSnapshot.SessionId;
                    _networkManager.IsCampaignSharedBattle = true;
                    _playingSceneManager.InitializeCampaignFromSnapshot(template, pendingSnapshot,
                        pendingSnapshot.JoiningPlayerId, pendingSnapshot.BattleOwnerPlayerId,
                        controlAgentsLocally: false);
                    _networkManager.AcknowledgeBattleSnapshot(pendingSnapshot.SessionId);
                }
                else
                {
                    _campaignSessionId = shared
                        ? _networkManager.CampaignSessionId
                        : Guid.NewGuid().ToString("N");
                    _playingSceneManager.InitializeCampaign(setup, shared ? 1 - localId : -1,
                        controlAgentsLocally: _controlsCampaignAgents);
                }
                _networkManager.SendCampaignPresence(new CampaignPresence(localId, CampaignOnlinePhase.Battle,
                    GameSetup.SelectedNodeId, false, GameSetup.SelectedLocalSkills, _campaignSessionId));
                _playingSceneManager.StartGame();
                return;
            }

            if (gameMode == 0)
            {
                // PvP 模式：两个玩家对战
                var players = new List<Player>
                {
                    new Player(0, "Player 1", 11,
                        OnlineModeRules.GetDuelLoadout(0).Select(SkillFactory.CreateSkill).ToList()),
                    new Player(1, "Player 2", 11,
                        OnlineModeRules.GetDuelLoadout(1).Select(SkillFactory.CreateSkill).ToList())
                };

                _uiManager.GameManager.LocalPlayerId = localId;

                // 初始化玩家控制器（联机模式：本地玩家 + 远程玩家）
                _playingSceneManager.InitializeControllers(players, localId);
                _playingSceneManager.Initialize(players);
            }

            _playingSceneManager.StartGame();
        }

        /// <summary>
        /// 处理单机游戏输入
        /// </summary>
        public void HandlePlayerInput(GameTime gameTime)
        {
            _playingSceneManager.Update(gameTime);
            if (_stateMachine.CurrentState != State.Playing) return;
            var gm = _uiManager.GameManager;
            _uiManager.UpdateUIButtons(gm);
            TrackTutorialReinforcements(gm);

            if (HandleTutorialDialogueInput(gameTime)) return;

            if (_uiManager.GetButtonByName("pauseBattle")?.IsClicked() == true)
            {
                _stateMachine.SwitchToState(State.Paused);
                return;
            }

            if (_playingSceneManager.IsPresentationBlocking)
                return;

            // 手柄输入更新
            _uiManager.UpdateControllerInput(gameTime);

            if (!gm.Query.IsLocalPlayerSource())
                return;

            var action = _uiManager.UpdateMouseAndCheckClicks(gm);

            // 手柄确认键
            if (action.Type == ActionType.None)
                action = _uiManager.CheckControllerConfirm(gm);

            if (action.Type != ActionType.None)
                gm.UpdateGame(action);
        }

        /// <summary>
        /// 处理联机游戏输入
        /// </summary>
        public void HandleOnlinePlayingInput(GameTime gameTime)
        {
            if (_stateMachine.CurrentState != State.OnlinePlaying) return;
            var gm = _uiManager.GameManager;
            _networkManager.ProcessMessages(gm);
            if (_awaitingBattleJoinAcknowledgement) return;
            _playingSceneManager.Update(gameTime);
            if (_stateMachine.CurrentState != State.OnlinePlaying) return;
            if (!_playingSceneManager.IsPresentationBlocking) TrySendBattleJoinSnapshot();
            if (_awaitingBattleJoinAcknowledgement) return;
            _uiManager.UpdateUIButtons(gm);

            if (HandleTutorialDialogueInput(gameTime)) return;

            if (_uiManager.GetButtonByName("pauseBattle")?.IsClicked() == true)
            {
                _stateMachine.SwitchToState(State.Paused);
                return;
            }

            if (_playingSceneManager.IsPresentationBlocking)
                return;

            // 手柄输入更新
            _uiManager.UpdateControllerInput(gameTime);

            if (!gm.Query.IsLocalPlayerSource())
                return;

            var action = _uiManager.UpdateMouseAndCheckClicks(gm);

            // 手柄确认键
            if (action.Type == ActionType.None)
                action = _uiManager.CheckControllerConfirm(gm);

            if (action.Type != ActionType.None) gm.UpdateGame(action);
        }

        private bool HandleTutorialDialogueInput(GameTime gameTime)
        {
            var dialogue = _uiManager.TutorialDialogue;
            if (dialogue == null) return false;

            dialogue.IsClicked();
            _uiManager.UpdateControllerInput(gameTime);
            KeyboardState keyboard = Keyboard.GetState();
            bool keyboardConfirm = (keyboard.IsKeyDown(Keys.Enter) && !_tutorialPreviousKeyboard.IsKeyDown(Keys.Enter)) ||
                (keyboard.IsKeyDown(Keys.Space) && !_tutorialPreviousKeyboard.IsKeyDown(Keys.Space));
            bool skip = keyboard.IsKeyDown(Keys.Escape) && !_tutorialPreviousKeyboard.IsKeyDown(Keys.Escape);
            _tutorialPreviousKeyboard = keyboard;

            if (skip) dialogue.Finish();
            else if (keyboardConfirm || _uiManager.GetControllerInput()?.IsConfirmPressed() == true)
                dialogue.Advance();
            return true;
        }

        private void OnTutorialActionRequired(object? sender, PlayerActionRequiredEventArgs e)
        {
            if (_stateMachine.CurrentState != State.Playing || !GameSetup.SelectedNode.IsTutorial ||
                e.SourcePlayerId != _uiManager.GameManager.LocalPlayerId)
                return;

            var node = GameSetup.SelectedNode;
            var game = _uiManager.GameManager;
            if (node.Id == "T01" && game.CurrentState is ChooseNumberIndexState chooseNumbers)
            {
                if (chooseNumbers.Stage == 0 && MarkTutorialHint(node.Id + ":number-first"))
                    _uiManager.ShowTutorialHint(node,
                        "Step 1 of 2: tap either number on your card. This is the number you will replace.");
                else if (chooseNumbers.Stage == 1 && MarkTutorialHint(node.Id + ":number-second"))
                    _uiManager.ShowTutorialHint(node,
                        "Step 2 of 2: tap a number on any living unit. Add the values; totals wrap at 10, and only your first number changes.");
            }
            else if (node.Id == "T01" && game.CurrentState is TargetSelectionState target &&
                     target.DamageValue > 0 && MarkTutorialHint(node.Id + ":target"))
            {
                _uiManager.ShowTutorialHint(node,
                    "Now attack: tap the hostile unit's card. Its health drops by your attack value.");
            }
            else if (node.Id == "T02" && game.CurrentState is PlusAttackSkillState &&
                     MarkTutorialHint(node.Id + ":attack-skill"))
            {
                _uiManager.ShowTutorialHint(node,
                    "Attack Up triggered automatically. Tap APPLY to add 1 damage, or PASS to decline it.");
            }
        }

        private void OnTutorialMessageUpdated(object? sender, GameEventArgs e)
        {
            if (_stateMachine.CurrentState != State.Playing || !GameSetup.SelectedNode.IsTutorial ||
                GameSetup.SelectedNode.Id != "T02")
                return;

            var game = _uiManager.GameManager;
            bool isDefenceTrigger = game.Players.Any(player =>
                string.Equals(e.Message, GameText.SkillActivated(player.Name, SkillType.PlusDefence),
                    StringComparison.Ordinal));
            if (isDefenceTrigger && MarkTutorialHint("T02:defence-skill"))
                _uiManager.ShowTutorialHint(GameSetup.SelectedNode,
                    "Defense Up triggers automatically when this unit is attacked. It reduces the hit by 1 (minimum 1); no button is needed.");
        }

        private void TrackTutorialReinforcements(GameManager game)
        {
            var node = GameSetup.SelectedNode;
            if (!node.IsTutorial) return;
            if (!string.Equals(_tutorialTrackedNodeId, node.Id, StringComparison.Ordinal))
            {
                _tutorialTrackedNodeId = node.Id;
                _tutorialTrackedRosterSize = game.Players.Count;
                _tutorialHintsShown.Clear();
                return;
            }

            if (node.Id == "T03" && game.Players.Count > _tutorialTrackedRosterSize &&
                MarkTutorialHint(node.Id + ":reinforcement"))
            {
                _uiManager.ShowTutorialHint(node,
                    "Reinforcement arrived! A new enemy card has joined the field. Defeat every hostile unit to clear the encounter.");
            }
            _tutorialTrackedRosterSize = game.Players.Count;
        }

        private bool MarkTutorialHint(string key) => _tutorialHintsShown.Add(key);

        private void OnGameActionSubmitted(GameAction action, GameActionOrigin origin)
        {
            if (_stateMachine.CurrentState != State.OnlinePlaying || origin == GameActionOrigin.RemoteNetwork)
                return;

            if (_networkManager.CurrentGameMode == 2)
            {
                if (!_networkManager.IsCampaignSharedBattle) return;
                // Only the battle owner simulates campaign AI. The joining peer waits
                // for the owner's AI actions just as it waits for the owner's snapshot.
                if (origin == GameActionOrigin.Automated && !_controlsCampaignAgents) return;
            }
            else if (origin == GameActionOrigin.Automated)
            {
                return;
            }

            _networkManager.SendAction(action);
        }

        private void OnBattleJoinRequested(string sessionId)
        {
            var presence = _networkManager.RemoteCampaignPresence;
            if (_stateMachine.CurrentState != State.OnlinePlaying || _networkManager.CurrentGameMode != 2 ||
                string.IsNullOrEmpty(_campaignSessionId) || sessionId != _campaignSessionId ||
                presence.Phase != CampaignOnlinePhase.Loadout || !presence.Ready ||
                !string.Equals(presence.NodeId, GameSetup.SelectedNodeId, StringComparison.OrdinalIgnoreCase)) return;
            _pendingBattleJoinSession = sessionId;
        }

        private void TrySendBattleJoinSnapshot()
        {
            if (string.IsNullOrEmpty(_pendingBattleJoinSession) ||
                !_uiManager.GameManager.Query.IsChooseFirstNumberIndexState()) return;

            var game = _uiManager.GameManager;
            int joiningPlayerId = game.Players.Count == 0 ? 1 : game.Players.Max(player => player.PlayerId) + 1;
            var joiner = new Player(joiningPlayerId, "Player 2", 11,
                (_networkManager.RemoteCampaignPresence.Skills ?? Array.Empty<SkillType>())
                .Distinct().OrderBy(skill => (int)skill).Select(SkillFactory.CreateSkill).ToList())
            { PortraitKey = "arithmetist" };
            _playingSceneManager.AddCampaignRemotePlayer(joiner, 0);
            _networkManager.IsCampaignSharedBattle = true;
            var snapshot = BattleSnapshot.Capture(game, _campaignSessionId, GameSetup.SelectedNodeId,
                _playingSceneManager.CampaignRound);
            snapshot.JoiningPlayerId = joiningPlayerId;
            snapshot.BattleOwnerPlayerId = game.LocalPlayerId;
            _networkManager.SendBattleSnapshot(snapshot);
            _pendingBattleJoinSession = string.Empty;
            _awaitingBattleJoinAcknowledgement = true;
        }

        private void OnBattleSnapshotAcknowledged(string sessionId)
        {
            if (sessionId != _campaignSessionId) return;
            _awaitingBattleJoinAcknowledgement = false;
        }
    }
}
