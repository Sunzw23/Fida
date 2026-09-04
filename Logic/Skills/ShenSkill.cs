using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class ShenSkill : Skill
    {
        public ShenSkill()
        {
            Type = SkillType.Shen;
            Value = 2;
            Trigger = SkillTrigger.Damaged;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new ShenSkillState(gameManager, context);
        }
    }
}
