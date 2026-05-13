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

        public bool IsInitialized { get; private set; }

        public PlayingSceneManager(GameManager gameManager, UIManager uiManager, AgentManager agentManager)
        {
            _gameManager = gameManager;
            _uiManager = uiManager;
            _agentManager = agentManager;

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _gameManager.NumberChanged += OnNumberChanged;
            _gameManager.AttackExecuted += OnAttackExecuted;
            _gameManager.HealExecuted += OnHealExecuted;
            _gameManager.MessageUpdated += OnMessageUpdated;
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

        public void StartGame()
        {
            _gameManager.StartGame();
        }

        #region Event Handlers

        private void OnNumberChanged(object sender, NumberChangeEventArgs e)
        {
            var uiPlayer = _uiManager.GetUIPlayer(e.SourcePlayerId);
            uiPlayer?.UpdateNumbers();
            uiPlayer?.UpdateHealth();
        }

        private void OnAttackExecuted(object sender, AttackEventArgs e)
        {
            var uiPlayer = _uiManager.GetUIPlayer(e.TargetPlayerId);
            uiPlayer?.UpdateHealth();
        }

        private void OnHealExecuted(object sender, HealEventArgs e)
        {
            var uiPlayer = _uiManager.GetUIPlayer(e.TargetPlayerId);
            uiPlayer?.UpdateHealth();
        }

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

        #endregion
    }
}
