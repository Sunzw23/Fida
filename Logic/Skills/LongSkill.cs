using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    public class LongSkill : Skill
    {
        public LongSkill()
        {
            Type = SkillType.Long;
            Value = 3;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new LongSkillState(gameManager, context);
        }
    }

    // 龙
    public class LongSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public LongSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.DecideApply,
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.Long)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count > 0 && actions[0].IsApply && _ctx.Numbers != null)
                _ctx.Damage = _ctx.Numbers[0] + _ctx.Numbers[1];

            GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
        }
    }
}
