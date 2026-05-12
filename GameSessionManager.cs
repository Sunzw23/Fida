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
            int remoteId = localId == 0 ? 1 : 0;

            var players = new List<Player>
            {
                new Player(localId, "YOU"),
                new Player(remoteId, "OPPONENT")
            };

            // 确保 Player 列表按 ID 排序
            if (localId == 1)
                players.Reverse();

            _playingSceneManager.Initialize(players);
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

            if (gm.CurrentPlayerId != 0)
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

            if (gm.CurrentPlayerId != _networkManager.GetLocalPlayerId())
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