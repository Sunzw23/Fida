using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
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
