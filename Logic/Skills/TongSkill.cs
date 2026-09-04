using System;
using System.Collections.Generic;

namespace Fida.Logic
{

    public class TongSkill : Skill
    {
        public TongSkill()
        {
            Type = SkillType.Tong;
            Value = 2;
            Trigger = SkillTrigger.TurnStart;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new TongSkillState(gameManager, context);
        }
    }
}
