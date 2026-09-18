using System;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign.Battle
{
    /// <summary>面向多敌人小队战的规则控制器，不依赖特定关卡或 UI。</summary>
    public sealed class RuleBasedCampaignController : IAutomatedPlayerController
    {
        public int PlayerId { get; }

        public RuleBasedCampaignController(int playerId) => PlayerId = playerId;

        public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args)
        {
            GameAction action = args.RequiredActionType switch
            {
                ActionType.ChooseNumberIndex => ChooseNumberIndex(game, args),
                ActionType.ChooseTarget => ChooseTarget(game, args),
                ActionType.DecideApply => new GameAction(ActionType.DecideApply)
                {
                    TargetPlayerId = PlayerId,
                    IsApply = true
                },
                ActionType.ChooseNumber => ChooseNumber(args),
                _ => new GameAction(ActionType.None)
            };
            if (action.Type != ActionType.None) game.UpdateGame(action);
        }

        private GameAction ChooseNumberIndex(GameManager game, PlayerActionRequiredEventArgs args)
        {
            if (game.Query.IsChooseFirstNumberIndexState())
                return new GameAction(ActionType.ChooseNumberIndex) { TargetPlayerId = PlayerId, NumberIndex = 0 };

            var source = game.Players.First(p => p.PlayerId == PlayerId);
            int first = game.Query.GetFirstChosenNumberIndex();
            if (first < 0)
            {
                // A skill such as God is asking which of the owner's numbers to
                // rewrite. This is not the second operand of the normal turn flow.
                int targetId = args.ValidTargets.Contains(PlayerId)
                    ? PlayerId
                    : args.ValidTargets.FirstOrDefault(PlayerId);
                var target = game.Players.FirstOrDefault(p => p.PlayerId == targetId) ?? source;
                int index = target.Numbers.Length > 1 && target.Numbers[1] < target.Numbers[0] ? 1 : 0;
                return new GameAction(ActionType.ChooseNumberIndex)
                {
                    TargetPlayerId = target.PlayerId,
                    NumberIndex = index
                };
            }
            var candidates = game.Players
                .Where(p => p.IsAlive() && (args.ValidTargets.Count == 0 || args.ValidTargets.Contains(p.PlayerId)))
                .SelectMany(p => new[] { (player: p, index: 0), (player: p, index: 1) })
                .Where(c => c.player.PlayerId != PlayerId || c.index != first)
                .OrderByDescending(c => Score(source, first, c.player.Numbers[c.index]))
                .ToList();

            var best = candidates.FirstOrDefault();
            return new GameAction(ActionType.ChooseNumberIndex)
            {
                TargetPlayerId = best.player?.PlayerId ?? PlayerId,
                NumberIndex = best.player == null ? 1 - first : best.index
            };
        }

        private static int Score(Player source, int firstIndex, int addend)
        {
            int result = (source.Numbers[firstIndex] + addend) % 10;
            int other = source.Numbers[1 - firstIndex];
            int a = firstIndex == 0 ? result : other;
            int b = firstIndex == 0 ? other : result;
            if (a == 6 && b == 6) return 60;
            if (a == 5 && b == 5) return 50;
            if ((a == 1 && b == 6) || (a == 6 && b == 1)) return 40;
            if (result is 7 or 8) return 30;
            if (result == 9 && source.Health < source.MaxHealth) return 20;
            if (result == 0) return 15;
            return result;
        }

        private GameAction ChooseTarget(GameManager game, PlayerActionRequiredEventArgs args)
        {
            var candidates = game.Players
                .Where(p => p.IsAlive() && args.ValidTargets.Contains(p.PlayerId))
                .ToList();
            Player target;
            if (game.Query.IsHealing())
            {
                target = candidates.OrderBy(p => (double)p.Health / Math.Max(1, p.MaxHealth))
                    .FirstOrDefault();
            }
            else
            {
                // Self-targeting is legal for tactical triggers, but automated enemies
                // still prefer a hostile target whenever one is available.
                var hostile = candidates.Where(p => p.PlayerId != PlayerId).ToList();
                target = (hostile.Count > 0 ? hostile : candidates)
                    .OrderBy(p => p.Health).FirstOrDefault();
            }
            return new GameAction(ActionType.ChooseTarget) { TargetPlayerId = target?.PlayerId ?? PlayerId };
        }

        private static GameAction ChooseNumber(PlayerActionRequiredEventArgs args)
        {
            int[] priority = [8, 9, 0, 5, 1];
            int number = priority.FirstOrDefault(n => args.ValidNumbers.Count == 0 || args.ValidNumbers.Contains(n));
            if (args.ValidNumbers.Count > 0 && !args.ValidNumbers.Contains(number)) number = args.ValidNumbers[0];
            return new GameAction(ActionType.ChooseNumber) { Number = number };
        }
    }
}
