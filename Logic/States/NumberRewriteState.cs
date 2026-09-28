using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// Shared two-step state for single-number rewriting skills (Shen/Xian/Di):
    /// pick a number index on the source player, then pick the new value.
    /// The only difference between the skills is the SkillType used in the prompts.
    /// </summary>
    public class NumberRewriteState : GameState
    {
        private readonly SkillType _skillType;

        public NumberRewriteState(GameManager gameManager, SkillContext ctx, SkillType skillType)
            : base(gameManager, ctx.SourcePlayerId)
        {
            _skillType = skillType;
        }

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.SkillActivatedNumberTurnFrom(SourcePlayer.Name, _skillType)
            });
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = Enumerable.Range(0, 10).ToList(),
                Prompt = GameText.SkillActivatedNumberTurnInto(SourcePlayer.Name, _skillType)
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
