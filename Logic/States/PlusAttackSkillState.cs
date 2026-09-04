using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{

    public class PlusAttackSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public PlusAttackSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.DecideApply,
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.PlusAttack)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count > 0 && actions[0].IsApply)
                _ctx.Damage += 1;

            GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
        }
    }
}
