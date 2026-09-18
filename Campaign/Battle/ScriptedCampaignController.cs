using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Campaign.Battle
{
    /// <summary>序章用的有限动作队列。队列耗尽后角色只跳过回合。</summary>
    public sealed class ScriptedCampaignController : IAutomatedPlayerController
    {
        private readonly IReadOnlyList<(int first, int second)> _turns =
            new List<(int, int)> { (0, 1), (1, 0), (0, 1) };
        private int _turnIndex = -1;
        public int PlayerId { get; }

        public ScriptedCampaignController(int playerId) => PlayerId = playerId;

        public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args)
        {
            if (args.RequiredActionType == ActionType.ChooseNumberIndex && game.Query.IsChooseFirstNumberIndexState())
            {
                _turnIndex++;
                if (_turnIndex >= _turns.Count)
                {
                    game.SkipCurrentTurn();
                    return;
                }
            }

            var plan = _turnIndex >= 0 && _turnIndex < _turns.Count
                ? _turns[_turnIndex]
                : (first: 0, second: 1);
            GameAction action = args.RequiredActionType switch
            {
                ActionType.ChooseNumberIndex when game.Query.IsChooseFirstNumberIndexState() =>
                    new GameAction(ActionType.ChooseNumberIndex) { TargetPlayerId = PlayerId, NumberIndex = plan.first },
                ActionType.ChooseNumberIndex =>
                    new GameAction(ActionType.ChooseNumberIndex) { TargetPlayerId = PlayerId, NumberIndex = plan.second },
                ActionType.ChooseTarget =>
                    new GameAction(ActionType.ChooseTarget) { TargetPlayerId = args.ValidTargets.Count > 0 ? args.ValidTargets[0] : 0 },
                ActionType.DecideApply => new GameAction(ActionType.DecideApply)
                {
                    TargetPlayerId = PlayerId,
                    IsApply = true
                },
                ActionType.ChooseNumber => new GameAction(ActionType.ChooseNumber) { Number = args.ValidNumbers.Count > 0 ? args.ValidNumbers[0] : 8 },
                _ => new GameAction(ActionType.None)
            };
            if (action.Type != ActionType.None) game.UpdateGame(action);
        }
    }
}
