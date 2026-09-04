using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class DoubleHealthSkill : Skill
    {
        public DoubleHealthSkill()
        {
            Type = SkillType.DoubleHealth;
            Value = 1;
            Trigger = SkillTrigger.GameStart;
            Usage = new SkillUsageState(maxUses: 1);
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new DoubleHealthSkillState(gameManager, context);
        }
    }
}
