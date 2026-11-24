#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign.Battle
{
    /// <summary>
    /// Companion battle passives and the summoner loop. Round-based so host and client
    /// derive the same result; the scene layer reacts to <see cref="Summoned"/> and
    /// <see cref="Removed"/> to rebuild cards and controllers.
    /// </summary>
    public sealed class CampaignBattleRules
    {
        // Sable needs two of her own turns to write the echo back.
        private const int ChannelTurns = 2;

        private readonly GameManager _game;
        private CampaignBattleSetup? _setup;
        private int _localId;
        private int _alliedGroupId;
        private int _round;
        private int _sableId = -1;
        private int _ilaId = -1;
        private int _bramId = -1;
        private int _echoId = -1;
        private Player? _echo;
        private int _ilaDamagedRound = -1;
        private int _channelTurns;
        private bool _removalPending;
        private int _nextEchoId = 1000;

        public event Action<Player>? Summoned;
        public event Action<int>? Removed;

        public CampaignBattleRules(GameManager game)
        {
            _game = game;
            _game.TurnStarted += OnTurnStarted;
            _game.ActionSubmitted += OnActionSubmitted;
            _game.CardActionStarted += OnCardActionStarted;
        }

        public bool IsSummoner(int playerId) => _sableId >= 0 && playerId == _sableId;

        /// <summary>
        /// Called by Sable's controller when it is her turn. Consumes one channel turn
        /// and tells the controller to skip instead of acting.
        /// </summary>
        public bool ShouldChannel(int playerId)
        {
            if (_setup == null || playerId != _sableId) return false;
            if (_echo is { } echo && echo.IsAlive()) return false;
            if (_channelTurns <= 0) return false;
            _channelTurns--;
            return true;
        }

        public void Configure(CampaignBattleSetup setup, int initialRound = 0)
        {
            if (_echo != null) _echo.HealthChanged -= OnEchoHealthChanged;
            _setup = setup;
            _round = Math.Max(0, initialRound);
            _localId = setup.Players.FirstOrDefault(player => player.PlayerId == 0)?.PlayerId
                       ?? setup.Players.FirstOrDefault()?.PlayerId ?? 0;
            _alliedGroupId = setup.Groups.FirstOrDefault(group =>
                group.Players.Any(player => player.PlayerId == 0))?.GroupId ?? 0;
            _sableId = _ilaId = _bramId = _echoId = -1;
            _echo = null;
            _channelTurns = 0;
            _removalPending = false;
            _ilaDamagedRound = -1;
            _nextEchoId = Math.Max(1000, setup.Players.Max(player => player.PlayerId) + 1);

            foreach (var (playerId, companionId) in setup.CompanionIds)
            {
                switch (companionId)
                {
                    case "sable": _sableId = playerId; break;
                    case "ila": _ilaId = playerId; break;
                    case "bram": _bramId = playerId; break;
                }
            }

            if (_sableId >= 0 && setup.Summons.TryGetValue(_sableId, out int echoId))
            {
                _echoId = echoId;
                _echo = setup.Players.FirstOrDefault(player => player.PlayerId == echoId);
                if (_echo != null)
                {
                    _echo.HealthChanged += OnEchoHealthChanged;
                    if (!_echo.IsAlive())
                    {
                        _removalPending = true;
                        _channelTurns = ChannelTurns;
                    }
                }
            }
        }

        private void OnTurnStarted(object? sender, TurnStartEventArgs e)
        {
            if (_setup == null) return;
            bool newRound = e.SourcePlayerId == RoundAnchor();
            if (newRound)
            {
                _round++;
                if (_removalPending) RemoveEcho();
            }
            UpdateBramWard();
            var local = Find(_localId);
            if (local != null)
                local.DoubleStrikeArmed = e.SourcePlayerId == _localId && IlaStruckRecently();
            if (e.SourcePlayerId == _sableId) OnSableTurn();
        }

        private void OnActionSubmitted(GameAction action, GameActionOrigin origin) => UpdateBramWard();

        private void OnCardActionStarted(object? sender, CardActionEventArgs e)
        {
            if (e.SourcePlayerId == _ilaId && e.IsDamaging) _ilaDamagedRound = _round;
        }

        private void OnEchoHealthChanged(Player player)
        {
            if (player.Health > 0 || _removalPending) return;
            // Start writing immediately: her next two turns are spent, even before the
            // body is cleared at the round boundary.
            _removalPending = true;
            _channelTurns = ChannelTurns;
        }

        private void UpdateBramWard()
        {
            var bram = Find(_bramId);
            if (bram == null) return;
            bram.WardsFirstHit = bram.Numbers[0] == 1 && bram.Numbers[1] == 1;
        }

        private bool IlaStruckRecently() =>
            _ilaDamagedRound >= 0 && _ilaDamagedRound >= _round - 1;

        private void OnSableTurn()
        {
            var sable = Find(_sableId);
            if (sable == null || !sable.IsAlive()) return;
            if (_echo is { } echo && echo.IsAlive()) return;
            if (_channelTurns > 0) return;
            SummonEcho();
        }

        private void SummonEcho()
        {
            var sable = Find(_sableId);
            if (sable == null || !sable.IsAlive()) return;
            var summon = CampaignCompanions.Get("sable")?.Summons ?? new CampaignSummon();
            var echo = new Player(_nextEchoId++, summon.Name, summon.Health,
                summon.Skills.Select(SkillFactory.CreateSkill).ToList())
            {
                PortraitKey = summon.PortraitKey
            };
            _echo = echo;
            _echoId = echo.PlayerId;
            _echo.HealthChanged += OnEchoHealthChanged;
            _game.AddPlayer(echo, _alliedGroupId);
            _game.SetGameMessage($"{sable.Name} writes: {echo.Name} takes the field.");
            Summoned?.Invoke(echo);
        }

        private void RemoveEcho()
        {
            _removalPending = false;
            if (_echo == null) return;
            _echo.HealthChanged -= OnEchoHealthChanged;
            int id = _echo.PlayerId;
            _echo = null;
            _echoId = -1;
            _game.RemovePlayer(id);
            _game.SetGameMessage("The Ink Echo comes apart. Sable begins writing again.");
            Removed?.Invoke(id);
        }

        private int RoundAnchor()
        {
            var group = _game.Groups.FirstOrDefault(g => g.GroupId == _alliedGroupId);
            int anchor = group?.Players.Where(player => player.IsAlive())
                .OrderBy(player => player.PlayerId).Select(player => player.PlayerId)
                .FirstOrDefault(-1) ?? -1;
            return anchor >= 0 ? anchor : _localId;
        }

        private Player? Find(int playerId) =>
            playerId < 0 ? null : _game.Players.FirstOrDefault(player => player.PlayerId == playerId);
    }
}
