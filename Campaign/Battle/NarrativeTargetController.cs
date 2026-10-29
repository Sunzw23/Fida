using System.Linq;
using Fida.Agent;
using Fida.Logic;

namespace Fida.Campaign.Battle;

/// <summary>The Withered King's intent constrains his target; the table still selects numbers and skills.</summary>
public sealed class NarrativeTargetController : IAutomatedPlayerController
{
    private readonly LightweightAgentController _model;
    public int PlayerId => _model.PlayerId;

    public NarrativeTargetController(int playerId) => _model = new(playerId);

    public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args)
    {
        if (args.SourcePlayerId != PlayerId) return;
        if (args.RequiredActionType == ActionType.ChooseTarget &&
            game.CurrentState is TargetSelectionState { DamageValue: > 0 } &&
            args.ValidTargets.Contains(PlayerId))
        {
            game.UpdateGame(new GameAction(ActionType.ChooseTarget) { TargetPlayerId = PlayerId },
                GameActionOrigin.Automated);
            return;
        }
        _model.RequestAction(game, args);
    }
}
