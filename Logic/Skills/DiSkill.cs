using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    public class DiSkill : Skill
    {
        public DiSkill()
        {
            Type = SkillType.Di;
            Value = 2;
            Trigger = SkillTrigger.TurnStart;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new DiSkillState(gameManager, context);
        }
    }

    public class DiSkillState : NumberRewriteState
    {
        public DiSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx, SkillType.Di) { }
    }
}
