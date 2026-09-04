using System;
using System.Collections.Generic;

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
}
