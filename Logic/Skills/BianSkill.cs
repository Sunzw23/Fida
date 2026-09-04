using System;
using System.Collections.Generic;

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
}
