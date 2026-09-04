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
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new XianSkillState(gameManager, context);
        }
    }

    // 仙
    public class XianSkillState : GameState
    {
        public XianSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.SkillActivatedNumberTurnFrom(SourcePlayer.Name, SkillType.Xian)
            });
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = [.. Enumerable.Range(0, 10)],
                Prompt = GameText.SkillActivatedNumberTurnInto(SourcePlayer.Name, SkillType.Xian)
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
