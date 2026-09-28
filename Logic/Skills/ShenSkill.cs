using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    public class ShenSkill : Skill
    {
        public ShenSkill()
        {
            Type = SkillType.Shen;
            Value = 2;
            Trigger = SkillTrigger.Damaged;
            Usage = new SkillUsageState(SkillUsageLimit.PerTurn);
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            Usage.MarkUsed();
            return new ShenSkillState(gameManager, context);
        }
    }

    // 神
    public class ShenSkillState : NumberRewriteState
    {
        public ShenSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx, SkillType.Shen) { }
    }
}
