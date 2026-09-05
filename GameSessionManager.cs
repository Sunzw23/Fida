using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Fida.Agent;
using Fida.Logic;
using Fida.Network;
using Fida.Scene;
using Fida.UI;

namespace Fida
{
    /// <summary>
    /// 游戏会话管理 - 负责单机和联机游戏的初始化与输入处理
    /// 支持 PvP（与好友对战）和 Co-op（与好友合作对战Agent）两种联机模式
    /// </summary>
    public class GameSessionManager
    {
        private readonly UIManager _uiManager;
        private readonly PlayingSceneManager _playingSceneManager;
        private readonly NetworkManager _networkManager;
        private readonly GameStateMachine _stateMachine;

        private bool _networkGameStarted;

        public GameSessionManager(UIManager uiManager, PlayingSceneManager playingSceneManager,
            NetworkManager networkManager, GameStateMachine stateMachine)
        {
            _uiManager = uiManager;
            _playingSceneManager = playingSceneManager;
            _networkManager = networkManager;
            _stateMachine = stateMachine;

            // 监听网络断开事件，在游戏中时强制结束游戏
            _networkManager.Disconnected += OnNetworkDisconnected;
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
            if (!_playingSceneManager.IsInitialized)
            {
                _playingSceneManager.Initialize(SceneConfig.PlayersScene1.Players);
                _playingSceneManager.StartGame();
            }
        }

        /// <summary>
        /// 确保联机游戏已初始化
        /// </summary>
        public void EnsureOnlineGameStarted()
        {
            if (_networkGameStarted) return;
            _networkGameStarted = true;

            int localId = _networkManager.GetLocalPlayerId();
            int gameMode = _networkManager.CurrentGameMode;

            if (gameMode == 0)
            {
                // PvP 模式：两个玩家对战
                var players = new List<Player>
                {
                    new Player(0, "玩家1"),
                    new Player(1, "玩家2")
                };

                _uiManager.GameManager.LocalPlayerId = localId;

                // 初始化玩家控制器（联机模式：本地玩家 + 远程玩家）
                _playingSceneManager.InitializeControllers(players, localId);
                _playingSceneManager.Initialize(players);
            }
            else
            {
                // Co-op 模式：两个人类玩家合作对战两个 Agent
                var players = new List<Player>
                {
                    new Player(0, "玩家1"),   // 主机
                    new Player(1, "玩家2"),   // 客机
                    new Player(2, "Agent 1"), // AI 对手
                    new Player(3, "Agent 2")  // AI 对手
                };

                // 分组：人类玩家一组，Agent 一组
                var groups = new List<Group>
                {
                    new Group(0, "人类联盟", [players[0], players[1]]),
                    new Group(1, "Agent 联盟", [players[2], players[3]])
                };

                _uiManager.GameManager.LocalPlayerId = localId;

                // 初始化玩家控制器（Co-op 模式：本地玩家 + 远程玩家 + Agent 玩家）
                _playingSceneManager.InitializeControllers(players, localId, true);
                _playingSceneManager.Initialize(players, groups);
            }

            _playingSceneManager.StartGame();
        }

        /// <summary>
        /// 处理单机游戏输入
        /// </summary>
        public void HandlePlayerInput(GameTime gameTime)
        {
            var gm = _uiManager.GameManager;
            _uiManager.UpdateUIButtons(gm);

            // 手柄输入更新
            _uiManager.UpdateControllerInput(gameTime);

            if (gm.Query.CurrentPlayerId != 0)
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
            var gm = _uiManager.GameManager;
            _uiManager.UpdateUIButtons(gm);

            // 手柄输入更新
            _uiManager.UpdateControllerInput(gameTime);

            _networkManager.ProcessMessages(gm);

            if (gm.Query.CurrentPlayerId != _networkManager.GetLocalPlayerId())
                return;

            var action = _uiManager.UpdateMouseAndCheckClicks(gm);

            // 手柄确认键
            if (action.Type == ActionType.None)
                action = _uiManager.CheckControllerConfirm(gm);

            if (action.Type != ActionType.None)
            {
                _networkManager.SendAction(action);
                gm.UpdateGame(action);
            }
        }
    }
}