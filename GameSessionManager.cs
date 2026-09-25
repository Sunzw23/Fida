using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using Fida.Agent;
using Fida.Logic;
using Fida.Network;
using Fida.Scene;
using Fida.UI;
using Fida.Campaign;

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

        private bool _networkGameStarted;
        private string _campaignSessionId = string.Empty;
        private string _pendingBattleJoinSession = string.Empty;
        private bool _awaitingBattleJoinAcknowledgement;

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
            _uiManager.GameManager.GameEnded += OnGameEnded;
            _uiManager.GameManager.PlayerActionRequired += OnPlayerActionRequired;
        }

        private void OnGameEnded(object sender, GameEndEventArgs e)
        {
            bool onlineCampaign = _stateMachine.CurrentState == State.OnlinePlaying &&
                _networkManager.CurrentGameMode == 2;
            if (_stateMachine.CurrentState != State.Playing && !onlineCampaign) return;
            var game = _uiManager.GameManager;
            bool won = !e.IsDraw && game.AreAllies(e.WinnerPlayerId, game.LocalPlayerId);
            if (won) CampaignRuntime.Complete(GameSetup.SelectedNode);
            GameSetup.LastBattleWon = won;
            GameSetup.LastBattleMessage = e.Message;
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
        private void OnNetworkDisconnected(string message)
        {
            if (_stateMachine.CurrentState == State.OnlinePlaying)
            {
                _playingSceneManager.HandleRemotePlayerDisconnect();
            }
            else if (_stateMachine.CurrentState == State.OnlineLobby)
            {
                _stateMachine.GoToMainMenu();
            }
            else if (_stateMachine.CurrentState == State.Connecting || _stateMachine.CurrentState == State.Hosting)
            {
                // relay 拒绝（房间码无效/房间满）或连接失败 → 清理并回联机菜单
                _networkManager.Cleanup();
                _stateMachine.SwitchToState(State.OnlineMenu);
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

            int localId = _networkManager.GetLocalPlayerId();
            int gameMode = _networkManager.CurrentGameMode;

            if (gameMode == 2)
            {
                bool shared = _networkManager.IsCampaignSharedBattle;
                _uiManager.GameManager.LocalPlayerId = shared ? localId : 0;
                var setup = shared
                    ? CampaignBattleFactory.CreateCooperative(GameSetup.SelectedNode,
                        localId == 0 ? GameSetup.SelectedLocalSkills : _networkManager.RemoteCampaignPresence.Skills ?? [],
                        localId == 1 ? GameSetup.SelectedLocalSkills : _networkManager.RemoteCampaignPresence.Skills ?? [])
                    : CampaignBattleFactory.Create(GameSetup.SelectedNode, GameSetup.SelectedLocalSkills, localId);
                var pendingSnapshot = _networkManager.TakePendingBattleSnapshot();
                if (pendingSnapshot != null)
                {
                    var template = CampaignBattleFactory.Create(GameSetup.SelectedNode, []);
                    _campaignSessionId = pendingSnapshot.SessionId;
                    _networkManager.IsCampaignSharedBattle = true;
                    _playingSceneManager.InitializeCampaignFromSnapshot(template, pendingSnapshot,
                        pendingSnapshot.JoiningPlayerId, pendingSnapshot.BattleOwnerPlayerId);
                    _networkManager.AcknowledgeBattleSnapshot(pendingSnapshot.SessionId);
                }
                else
                {
                    _campaignSessionId = Guid.NewGuid().ToString("N");
                    _playingSceneManager.InitializeCampaign(setup, shared ? 1 - localId : -1,
                        controlAgentsLocally: true);
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
            _uiManager.UpdateUIButtons(gm);

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
            {
                if (_networkManager.CurrentGameMode != 2 || _networkManager.IsCampaignSharedBattle)
                    _networkManager.SendAction(action);
                gm.UpdateGame(action);
            }
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

        private void OnPlayerActionRequired(object sender, PlayerActionRequiredEventArgs args)
        {
            if (string.IsNullOrEmpty(_pendingBattleJoinSession) ||
                args.RequiredActionType != ActionType.ChooseNumberIndex ||
                _uiManager.GameManager.Query.GetFirstChosenNumberIndex() >= 0) return;

            var game = _uiManager.GameManager;
            int joiningPlayerId = game.Players.Count == 0 ? 1 : game.Players.Max(player => player.PlayerId) + 1;
            var joiner = new Player(joiningPlayerId, "Player 2", 11,
                (_networkManager.RemoteCampaignPresence.Skills ?? Array.Empty<SkillType>())
                .Distinct().OrderBy(skill => (int)skill).Select(SkillFactory.CreateSkill).ToList())
            { PortraitKey = "arithmetist" };
            _playingSceneManager.AddCampaignRemotePlayer(joiner, 0);
            _networkManager.IsCampaignSharedBattle = true;
            var snapshot = BattleSnapshot.Capture(game, _campaignSessionId, GameSetup.SelectedNodeId);
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
