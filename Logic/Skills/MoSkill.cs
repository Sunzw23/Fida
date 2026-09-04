using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class MoSkill : Skill
    {
        public MoSkill()
        {
            Type = SkillType.Mo;
            Value = 1;
            Trigger = SkillTrigger.Damaged;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new MoSkillState(gameManager, context);
        }
    }
}
