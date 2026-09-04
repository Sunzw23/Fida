using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class DoubleDefenceSkill : Skill
    {
        public DoubleDefenceSkill()
        {
            Type = SkillType.DoubleDefence;
            Value = 1;
            Trigger = SkillTrigger.Defending;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new DoubleDefenceSkillState(gameManager, context);
        }
    }
}
