using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class XianSkill : Skill
    {
        public XianSkill()
        {
            Type = SkillType.Xian;
            Value = 1;
            Trigger = SkillTrigger.OrderedAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new XianSkillState(gameManager, context);
        }
    }
}
