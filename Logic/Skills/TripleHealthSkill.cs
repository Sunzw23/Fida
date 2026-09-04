using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class TripleHealthSkill : Skill
    {
        public TripleHealthSkill()
        {
            Type = SkillType.TripleHealth;
            Value = 1;
            Trigger = SkillTrigger.GameStart;
            Usage = new SkillUsageState(maxUses: 1);
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new TripleHealthSkillState(gameManager, context);
        }
    }
}
