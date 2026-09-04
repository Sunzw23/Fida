using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    // 变
    public class BianSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public BianSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = [.. Enumerable.Range(1, 5)],
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.Bian)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count == 0)
            {
                return;
            }

            _ctx.Damage = actions[0].Number;
            GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
        }
    }
}
