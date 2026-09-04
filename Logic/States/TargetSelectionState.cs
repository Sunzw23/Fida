using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 选择目标状态
    /// </summary>
    public class TargetSelectionState : GameState
    {
        private readonly SkillContext _ctx;

        public TargetSelectionState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        public int DamageValue => _ctx.Damage;
        public int HealingValue => _ctx.Healing;

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseTarget,
                ValidTargets = GameManager.Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList(),
                Prompt = _ctx.Damage > 0 ? GameText.ChooseTargetForDamaging : GameText.ChooseTargetForHealing
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count == 0)
            {
                return;
            }

            var targetId = actions[0].TargetPlayerId;
            var targetPlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == targetId);
            if (targetPlayer == null || !targetPlayer.IsAlive()) return;

            _ctx.TargetPlayerId = targetId;

            if (_ctx.Damage > 0)
            {
                GameManager.TransitionTo(new DamagingState(GameManager, _ctx));
            }
            else
            {
                GameManager.TransitionTo(new HealingState(GameManager, _ctx));
            }
        }
    }
}
