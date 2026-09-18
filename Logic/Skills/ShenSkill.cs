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
    public class ShenSkillState : GameState
    {
        public ShenSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.SkillActivatedNumberTurnFrom(SourcePlayer.Name, SkillType.Shen)
            });
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = Enumerable.Range(0, 10).ToList(),
                Prompt = GameText.SkillActivatedNumberTurnInto(SourcePlayer.Name, SkillType.Shen)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count < 2)
            {
                return;
            }

            var numberIndexToChange = actions[0].NumberIndex;
            var numberToApply = actions[1].Number;
            SourcePlayer.SetNumber(numberIndexToChange, numberToApply);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
