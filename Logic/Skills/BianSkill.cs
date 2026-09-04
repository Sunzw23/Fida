using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    public class BianSkill : Skill
    {
        public BianSkill()
        {
            Type = SkillType.Bian;
            Value = 3;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new BianSkillState(gameManager, context);
        }
    }

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
