using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class XueSkill : Skill
    {
        public XueSkill()
        {
            Type = SkillType.Xue;
            Value = 1;
            Trigger = SkillTrigger.Damaging;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new XueSkillState(gameManager, context);
        }
    }
}
