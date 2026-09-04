using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    // 魔
    public class MoSkillState : GameState
    {
        private readonly SkillContext _ctx;
        private readonly int _damageReturned;
        public MoSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
            _damageReturned = _ctx.Damage - _ctx.Damage / 2;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseTarget,
                ValidTargets = GameManager.Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList(),
                Prompt = GameText.SkillActivatedChooseTarget(SourcePlayer.Name, SkillType.Mo) + GameText.MoSkillDamageReturn(_damageReturned)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count == 0)
            {
                return;
            }

            GameManager.TransitionTo(new TrueDamageState(GameManager,
                new SkillContext(SourcePlayerId) { TargetPlayerId = actions[0].TargetPlayerId, Damage = _damageReturned }));
        }
    }
}
