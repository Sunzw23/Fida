using System;
using System.Collections.Generic;
using Fida.Agent;
using Fida.Logic;
using Fida.UI;
using Microsoft.Xna.Framework.Graphics;
using Fida.Campaign;
using Fida.Campaign.Battle;
using Microsoft.Xna.Framework;

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
        private readonly CampaignEncounterCoordinator _campaignEncounter;
        private readonly AutomatedActionPresenter _actionPresenter = new();
        private CampaignBattleSetup _campaignSetup;
        private int _deferredCuePlayerId = -1;
        private string _deferredCueMessage = "";
        private bool _deferredGameEndForCardAction;

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
            _campaignEncounter = new CampaignEncounterCoordinator(gameManager);
            _campaignEncounter.ReinforcementsDeployed += OnReinforcementsDeployed;
            _actionPresenter.PresentationStarted += OnAutomatedPresentationStarted;
            _actionPresenter.PresentationEnded += OnAutomatedPresentationEnded;
            _gameManager.NumberOperationResolved += OnNumberOperationResolved;
            _gameManager.CardActionStarted += OnCardActionStarted;

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
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
            ResetPresentation();
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
            ResetPresentation();
            IsInitialized = true;
            _gameManager.Initialize(players, groups);
            _uiManager.BuildPlayingUI(_gameManager);

            // 如果没有显式初始化控制器，使用默认的单机模式
            if (_controllers.Count == 0)
            {
                InitializeControllers(players);
            }
        }

        /// <summary>使用战役编成初始化本地小队战。</summary>
        public void InitializeCampaign(CampaignBattleSetup setup)
        {
            ResetPresentation();
            _campaignSetup = setup;
            _controllers.Clear();
            foreach (var player in setup.Players)
                _controllers[player.PlayerId] = CreateCampaignController(player.PlayerId, setup);

            IsInitialized = true;
            _gameManager.Initialize(setup.Players, setup.Groups);
            _campaignEncounter.Configure(setup);
            _uiManager.BuildPlayingUI(_gameManager);
        }

        private IPlayerController CreateCampaignController(int playerId, CampaignBattleSetup setup)
        {
            if (playerId == _gameManager.LocalPlayerId) return new LocalPlayerController(playerId);
            var type = setup.AgentTypes.TryGetValue(playerId, out var configured)
                ? configured
                : CampaignAgentType.Rule;
            return CampaignControllerFactory.Create(playerId, type);
        }

        private void ResetPresentation()
        {
            _actionPresenter.Reset();
            _deferredCuePlayerId = -1;
            _deferredCueMessage = "";
            _deferredGameEndForCardAction = false;
        }

        private void OnReinforcementsDeployed(IReadOnlyList<Player> players)
        {
            if (_campaignSetup == null) return;
            foreach (var player in players)
                _controllers[player.PlayerId] = CreateCampaignController(player.PlayerId, _campaignSetup);
            _uiManager.RebuildPlayingUI(_gameManager);
        }

        public void StartGame()
        {
            _gameManager.StartGame();
        }

        /// <summary>Advances AI presentation timing once per rendered frame.</summary>
        public void Update(GameTime gameTime)
        {
            if (_uiManager.IsBlockingPresentationActive) return;

            if (_deferredGameEndForCardAction)
            {
                _deferredGameEndForCardAction = false;
                _gameManager.ResumeGameEndCheck();
                if (_gameManager.IsGameEnded) return;
            }

            if (_deferredCuePlayerId >= 0)
            {
                _uiManager.ShowActionCue(_deferredCuePlayerId, _deferredCueMessage);
                _deferredCuePlayerId = -1;
                _deferredCueMessage = "";
            }
            _actionPresenter.Update(_gameManager, (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        public bool IsPresentationBlocking => _uiManager.IsBlockingPresentationActive;

        #region Event Handlers

        private void OnMessageUpdated(object sender, GameEventArgs e)
        {
            _uiManager.UpdateGameMessage(e.Message);
        }

        private void OnPlayerActionRequired(object sender, PlayerActionRequiredEventArgs e)
        {
            if (_controllers.TryGetValue(e.SourcePlayerId, out var controller))
            {
                if (e.RequiredActionType == ActionType.ChooseNumberIndex)
                {
                    ShowCueWhenAvailable(e.SourcePlayerId,
                        BuildNumberSelectionCue(e.SourcePlayerId));
                }

                if (controller is IAutomatedPlayerController automated)
                {
                    _actionPresenter.Schedule(automated, e,
                        AutomatedActionPresenter.GetDelay(_gameManager, e));
                }
                else
                {
                    controller.RequestAction(_gameManager, e);
                }
            }
        }

        private void OnAutomatedPresentationStarted(int playerId, PlayerActionRequiredEventArgs args)
        {
            ShowCueWhenAvailable(playerId, BuildAutomatedActionCue(playerId, args));
        }

        private void OnAutomatedPresentationEnded()
        {
            if (!_uiManager.IsBlockingPresentationActive) _uiManager.HideActionCue();
        }

        private void OnNumberOperationResolved(object sender, NumberOperationEventArgs e)
        {
            _uiManager.HideActionCue();
            _uiManager.PlayNumberFlow(
                e.AddendPlayerId,
                e.AddendNumberIndex,
                e.SourcePlayerId,
                e.BaseNumberIndex);
        }

        private void OnCardActionStarted(object sender, CardActionEventArgs e)
        {
            if (e.IsDamaging && !_deferredGameEndForCardAction)
            {
                _gameManager.DeferGameEndCheck();
                _deferredGameEndForCardAction = true;
            }
            _uiManager.PlayCardAction(e.SourcePlayerId, e.TargetPlayerId, e.IsDamaging);
        }

        private void ShowCueWhenAvailable(int playerId, string message)
        {
            if (_uiManager.IsBlockingPresentationActive)
            {
                _deferredCuePlayerId = playerId;
                _deferredCueMessage = message;
                return;
            }
            _uiManager.ShowActionCue(playerId, message);
        }

        private string BuildNumberSelectionCue(int playerId)
        {
            var player = _gameManager.Players.Find(p => p.PlayerId == playerId);
            string name = player?.Name ?? "Player";
            return _gameManager.Query.GetFirstChosenNumberIndex() >= 0
                ? $"{name}  -  select a number to add"
                : $"{name}  -  select the number to change";
        }

        private string BuildAutomatedActionCue(int playerId, PlayerActionRequiredEventArgs args)
        {
            var player = _gameManager.Players.Find(p => p.PlayerId == playerId);
            string name = player?.Name ?? "Enemy";

            if (args.RequiredActionType == ActionType.ChooseNumberIndex)
            {
                return _gameManager.Query.GetFirstChosenNumberIndex() >= 0
                    ? $"{name}  -  choosing a number to add"
                    : $"{name}  -  choosing the number to change";
            }

            return args.RequiredActionType switch
            {
                ActionType.ChooseTarget => $"{name} is choosing a target",
                ActionType.ChooseNumber => $"{name} is invoking a number",
                ActionType.DecideApply => $"{name} is considering a skill",
                _ => $"{name} is acting"
            };
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
