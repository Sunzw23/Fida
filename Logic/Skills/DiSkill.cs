using System;
using System.Collections.Generic;

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
}
