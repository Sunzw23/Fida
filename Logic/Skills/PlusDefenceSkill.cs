using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class PlusDefenceSkill : Skill
    {
        public PlusDefenceSkill()
        {
            Type = SkillType.PlusDefence;
            Value = 1;
            Trigger = SkillTrigger.Defending;
            Usage = new SkillUsageState();
        }
        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new PlusDefenceSkillState(gameManager, context);
        }
    }
}
