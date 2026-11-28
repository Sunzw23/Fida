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
            bool damaging = _ctx.Damage > 0;
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseTarget,
                ValidTargets = GameManager.Players
                    .Where(p => p.IsAlive() && (damaging
                        ? p.PlayerId == SourcePlayerId ||
                          !GameManager.AreAllies(SourcePlayerId, p.PlayerId)
                        : GameManager.AreAllies(SourcePlayerId, p.PlayerId)))
                    .Select(p => p.PlayerId).ToList(),
                Prompt = damaging ? GameText.ChooseTargetForDamaging : GameText.ChooseTargetForHealing
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
            GameManager.OnCardActionStarted(new CardActionEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                TargetPlayerId = targetId,
                Damage = _ctx.Damage,
                Healing = _ctx.Healing
            });

            if (_ctx.Damage > 0)
            {
                // Resolve post-attack skills only after damage, shields, and reactions finish.
                GameManager.StateStack.Push(new AttackCompletedState(GameManager, SourcePlayerId));
                // A true-damage passive skips the shield, the defense skills, the gate passive, and
                // every post-damage reaction on both sides. Piercing only covers the first two.
                GameManager.TransitionTo(SourcePlayer.AttacksAreTrue
                    ? new TrueDamageState(GameManager, _ctx)
                    : new DamagingState(GameManager, _ctx));
            }
            else
            {
                GameManager.TransitionTo(new HealingState(GameManager, _ctx));
            }
        }
    }
}
