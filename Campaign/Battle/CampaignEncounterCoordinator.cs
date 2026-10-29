using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign.Battle
{
    /// <summary>
    /// 仅负责关卡阶段与增援，不处理绘制或输入。场景层监听 ReinforcementsDeployed 更新 UI 和控制器。
    /// </summary>
    public sealed class CampaignEncounterCoordinator
    {
        private readonly GameManager _game;
        private readonly List<Player> _observedPlayers = [];
        private CampaignBattleSetup? _setup;
        private Group? _alliedGroup;
        private bool _deployed;
        private bool _deploymentDeferred;
        private bool _deploymentPending;
        private int _round;
        public int RoundNumber => _round;

        public event Action<IReadOnlyList<Player>>? ReinforcementsDeployed;

        public CampaignEncounterCoordinator(GameManager game)
        {
            _game = game;
            _game.TurnStarted += OnTurnStarted;
        }

        public void Configure(CampaignBattleSetup setup, int initialRound = 0)
        {
            foreach (var player in _observedPlayers) player.HealthChanged -= OnHealthChanged;
            _observedPlayers.Clear();
            _setup = setup;
            _alliedGroup = setup.Groups.FirstOrDefault(group =>
                group.Players.Any(player => player.PlayerId == 0)) ?? setup.Groups.FirstOrDefault();
            _deployed = false;
            _deploymentDeferred = false;
            _deploymentPending = false;
            _round = Math.Max(0, initialRound);

            foreach (var player in setup.Players.Where(p => !_game.AreAllies(p.PlayerId, _game.LocalPlayerId)))
            {
                player.HealthChanged += OnHealthChanged;
                _observedPlayers.Add(player);
            }
        }

        private void OnTurnStarted(object? sender, TurnStartEventArgs e)
        {
            if (_setup == null || _deployed) return;
            // Count a round from shared game state, never from the local player's
            // identity; the latter differs between host and client.
            int roundAnchor = _alliedGroup?.Players.Where(player => player.IsAlive())
                .OrderBy(player => player.PlayerId).Select(player => player.PlayerId)
                .FirstOrDefault(-1) ?? -1;
            if (e.SourcePlayerId == roundAnchor) _round++;
            TryDeploy();
        }

        private void OnHealthChanged(Player player)
        {
            if (player.Health == 0) TryDeploy();
        }

        public void DeferDeployment() => _deploymentDeferred = true;

        public void ResumeDeployment()
        {
            _deploymentDeferred = false;
            if (_deploymentPending)
            {
                _deploymentPending = false;
                TryDeploy();
            }
        }

        private void TryDeploy()
        {
            if (_setup == null || _deployed || _setup.Reinforcements.Count == 0) return;
            int deaths = _setup.Players.Count(p => !_game.AreAllies(p.PlayerId, _game.LocalPlayerId) && !p.IsAlive());
            bool byRound = _setup.Node.ReinforcementRound > 0 && _round >= _setup.Node.ReinforcementRound;
            bool byDeaths = _setup.Node.ReinforcementAfterEnemyDeaths > 0 &&
                            deaths >= _setup.Node.ReinforcementAfterEnemyDeaths;
            if (!byRound && !byDeaths) return;

            if (_deploymentDeferred)
            {
                _deploymentPending = true;
                return;
            }

            _deployed = true;
            foreach (var reinforcement in _setup.Reinforcements) _game.AddPlayer(reinforcement, 1);
            _game.SetGameMessage($"Reinforcements arrived: {string.Join(", ", _setup.Reinforcements.Select(p => p.Name))}");
            ReinforcementsDeployed?.Invoke(_setup.Reinforcements);
        }
    }
}
