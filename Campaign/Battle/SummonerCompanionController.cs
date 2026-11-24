#nullable enable
using Fida.Agent;
using Fida.Logic;

namespace Fida.Campaign.Battle
{
    /// <summary>
    /// Sable spends her turns writing the next Ink Echo instead of acting. The skip goes
    /// through the same action path as a human skip, so turn order and presentation stay
    /// intact.
    /// </summary>
    public sealed class SummonerCompanionController : IAutomatedPlayerController
    {
        private readonly CampaignBattleRules _rules;
        private readonly LightweightAgentController _inner;

        public int PlayerId { get; }

        public SummonerCompanionController(int playerId, CampaignBattleRules rules)
        {
            PlayerId = playerId;
            _rules = rules;
            _inner = new LightweightAgentController(playerId);
        }

        public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args)
        {
            if (args.SourcePlayerId != PlayerId) return;
            if (_rules.ShouldChannel(PlayerId))
            {
                game.UpdateGame(new GameAction(ActionType.SkipTurn) { TargetPlayerId = PlayerId },
                    GameActionOrigin.Automated);
                return;
            }
            _inner.RequestAction(game, args);
        }
    }
}
