using System;
using System.Collections.Generic;
using System.Linq;

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

    // 同
    public class TongSkillState : GameState
    {
        public TongSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.DecideApply,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.SkillActivated(SourcePlayer.Name, SkillType.Tong)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count > 0 && actions[0].IsApply)
            {
                GameManager.StateStack.Pop();
                GameManager.StateStack.Push(new ChooseNumberIndexState(GameManager, SourcePlayerId, 0, -1, true));
            }
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
