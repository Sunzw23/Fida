using System;
using System.Collections.Generic;
using Fida.Agent;
using Fida.Logic;
using Fida.UI;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.Scene
{
    /// <summary>
    /// PlayingSceneManager 类负责管理游戏进行场景的事件处理和 UI 交互。
    /// 参考 Godot 版本的 Playing.cs 设计。
    /// </summary>
    public class PlayingSceneManager
    {
        private readonly GameManager _gameManager;
        private readonly UIManager _uiManager;
        private readonly AgentManager _agentManager;

        /// <summary>
        /// 玩家控制器字典 — 通过 IPlayerController 接口统一处理不同模式玩家的操作请求
        /// </summary>
        private Dictionary<int, IPlayerController> _controllers = new();

        public bool IsInitialized { get; set; }

        public PlayingSceneManager(GameManager gameManager, UIManager uiManager, AgentManager agentManager)
        {
            _gameManager = gameManager;
            _uiManager = uiManager;
            _agentManager = agentManager;

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _gameManager.MessageUpdated -= OnMessageUpdated;
            _gameManager.MessageUpdated += OnMessageUpdated;
            _gameManager.PlayerActionRequired -= OnPlayerActionRequired;
            _gameManager.PlayerActionRequired += OnPlayerActionRequired;
        }

        /// <summary>
        /// 初始化玩家控制器（单机模式：本地玩家 + AI 代理）
        /// </summary>
        public void InitializeControllers(List<Player> players)
        {
            _controllers.Clear();
            foreach (var player in players)
            {
                if (player.PlayerId == _gameManager.LocalPlayerId)
                {
                    _controllers[player.PlayerId] = new LocalPlayerController(player.PlayerId);
                }
                else
                {
                    _controllers[player.PlayerId] = new AgentPlayerController(player.PlayerId, _agentManager);
                }
            }
        }

        /// <summary>
        /// 初始化玩家控制器（联机模式：本地玩家 + 远程玩家）
        /// </summary>
        public void InitializeControllers(List<Player> players, int localPlayerId)
        {
            _controllers.Clear();
            foreach (var player in players)
            {
                if (player.PlayerId == localPlayerId)
                {
                    _controllers[player.PlayerId] = new LocalPlayerController(player.PlayerId);
                }
                else
                {
                    _controllers[player.PlayerId] = new RemotePlayerController(player.PlayerId);
                }
            }
        }

        /// <summary>
        /// 初始化玩家控制器（Co-op 联机模式：本地玩家 + 远程玩家 + Agent 玩家）
        /// </summary>
        public void InitializeControllers(List<Player> players, int localPlayerId, bool coopMode)
        {
            _controllers.Clear();
            foreach (var player in players)
            {
                if (player.PlayerId == localPlayerId)
                {
                    _controllers[player.PlayerId] = new LocalPlayerController(player.PlayerId);
                }
                else if (player.PlayerId < 2)
                {
                    // 另一个人类玩家（远程）
                    _controllers[player.PlayerId] = new RemotePlayerController(player.PlayerId);
                }
                else
                {
                    // Agent 玩家
                    _controllers[player.PlayerId] = new AgentPlayerController(player.PlayerId, _agentManager);
                }
            }
        }

        /// <summary>
        /// 初始化游戏（单机或 PvP 联机）
        /// </summary>
        public void Initialize(List<Player> players)
        {
            IsInitialized = true;
            _gameManager.Initialize(players);
            _uiManager.BuildPlayingUI(_gameManager);

            // 如果没有显式初始化控制器，使用默认的单机模式
            if (_controllers.Count == 0)
            {
                InitializeControllers(players);
            }
        }

        /// <summary>
        /// 初始化游戏（Co-op 联机模式，带分组）
        /// </summary>
        public void Initialize(List<Player> players, List<Group> groups)
        {
            IsInitialized = true;
            _gameManager.Initialize(players, groups);
            _uiManager.BuildPlayingUI(_gameManager);

            // 如果没有显式初始化控制器，使用默认的单机模式
            if (_controllers.Count == 0)
            {
                InitializeControllers(players);
            }
        }

        public void StartGame()
        {
            _gameManager.StartGame();
        }

        #region Event Handlers

        private void OnMessageUpdated(object sender, GameEventArgs e)
        {
            _uiManager.UpdateGameMessage(e.Message);
        }

        private void OnPlayerActionRequired(object sender, PlayerActionRequiredEventArgs e)
        {
            if (_controllers.TryGetValue(e.SourcePlayerId, out var controller))
            {
                controller.RequestAction(_gameManager, e);
            }
        }

        /// <summary>
        /// 处理远程玩家断开连接：强制结束游戏，本地玩家获胜
        /// </summary>
        public void HandleRemotePlayerDisconnect()
        {
            int localPlayerId = _gameManager.LocalPlayerId;
            _gameManager.ForceEndGame(localPlayerId);
        }

        #endregion
    }
}
