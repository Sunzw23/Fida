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

        public void Initialize(List<Player> players)
        {
            IsInitialized = true;
            _gameManager.Initialize(players);
            _uiManager.BuildPlayingUI(_gameManager);
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
            if (e.SourcePlayerId == 0)
            {
                // 玩家操作：按钮会通过 EnabledChecker 自动询问环境来决定是否启用
                // 无需手动设置按钮状态
            }
            else
            {
                // 获取 Agent 的动作并执行
                var agentAction = _agentManager.GetAgentAction(_gameManager, e.SourcePlayerId);
                _gameManager.UpdateGame(agentAction);
            }
        }

        #endregion
    }
}
