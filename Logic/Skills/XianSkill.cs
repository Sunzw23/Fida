using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    public class XianSkill : Skill
    {
        public XianSkill()
        {
            Type = SkillType.Xian;
            Value = 1;
            Trigger = SkillTrigger.OrderedAttack;
            Usage = new SkillUsageState(SkillUsageLimit.PerTurn);
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            Usage.MarkUsed();
            return new XianSkillState(gameManager, context);
        }
    }

    // 仙
    public class XianSkillState : NumberRewriteState
    {
        public XianSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx, SkillType.Xian) { }
    }
}
