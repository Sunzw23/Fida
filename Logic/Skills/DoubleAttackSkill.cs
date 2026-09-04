using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class DoubleAttackSkill : Skill
    {
        public DoubleAttackSkill()
        {
            Type = SkillType.DoubleAttack;
            Value = 1;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new DoubleAttackSkillState(gameManager, context);
        }
    }
}
