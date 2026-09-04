using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    public class DoubleAttackSkill : Skill
    {
        public DoubleAttackSkill()
        {
            Type = SkillType.DoubleAttack;
            Value = 1;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new DoubleAttackSkillState(gameManager, context);
        }
    }

    public class DoubleAttackSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public DoubleAttackSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.DecideApply,
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleAttack)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count > 0 && actions[0].IsApply)
                _ctx.Damage *= 2;

            GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
        }
    }
}
