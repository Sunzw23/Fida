#nullable enable
using System;
using System.Collections.Generic;
using Fida.Agent;
using Fida.Logic;

namespace Fida.Campaign.Battle
{
    /// <summary>
    /// Mara has no active ability; her decision strength rises as the party's lowest
    /// health ratio falls. The profile is per-controller, so this never touches the
    /// global agent settings used by enemies.
    /// </summary>
    public sealed class AdaptiveCompanionController : IAutomatedPlayerController
    {
        private const int Weakest = 40;
        private const int Sharpest = 95;
        private LightweightAgent _agent;
        private int _strength = -1;

        public int PlayerId { get; }

        public AdaptiveCompanionController(int playerId)
        {
            PlayerId = playerId;
            _agent = new LightweightAgent(AgentDefaults.Profile with { Strength = Weakest });
        }

        /// <summary>Full party health keeps her distracted; a nearly dead ally sharpens her.</summary>
        public static int StrengthFor(double lowestHealthRatio)
        {
            double ratio = Math.Clamp(lowestHealthRatio, 0.0, 1.0);
            return (int)Math.Round(Math.Clamp(Weakest + (Sharpest - Weakest) * (1 - ratio),
                Weakest, Sharpest));
        }

        public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args)
        {
            if (args.SourcePlayerId != PlayerId) return;
            int strength = StrengthFor(LowestAllyRatio(game));
            if (strength != _strength)
            {
                _strength = strength;
                _agent = new LightweightAgent(AgentDefaults.Profile with { Strength = strength });
            }
            var action = _agent.Decide(game, args);
            if (action.Type != ActionType.None) game.UpdateGame(action, GameActionOrigin.Automated);
        }

        private double LowestAllyRatio(GameManager game)
        {
            double lowest = 1.0;
            foreach (var player in game.Players)
            {
                if (!game.AreAllies(PlayerId, player.PlayerId)) continue;
                if (player.MaxHealth <= 0) continue;
                lowest = Math.Min(lowest, Math.Clamp(player.Health / (double)player.MaxHealth, 0.0, 1.0));
            }
            return lowest;
        }
    }
}
