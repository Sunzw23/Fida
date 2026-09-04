using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class PlusAttackSkill : Skill
    {
        public PlusAttackSkill()
        {
            Type = SkillType.PlusAttack;
            Value = 1;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public SkillUsageLimit SkillUsageLimit { get; }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new PlusAttackSkillState(gameManager, context);
        }
    }
}
