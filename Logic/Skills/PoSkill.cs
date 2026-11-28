using System.Collections.Generic;

namespace Fida.Logic
{
    /// <summary>
    /// Marks the current attack as unstoppable: it passes the target's shield digit and skips
    /// every <see cref="SkillTrigger.Defending"/> skill. The gate passive (WardsFirstHit) and the
    /// post-damage reactions still apply — this only removes the two layers that stand between the
    /// target and its health.
    /// </summary>
    public sealed class PoSkill : Skill
    {
        public PoSkill()
        {
            Type = SkillType.Po;
            Value = 1;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context) =>
            new PoSkillState(gameManager, context);
    }

    public sealed class PoSkillState : GameState
    {
        private readonly SkillContext _context;

        public PoSkillState(GameManager gameManager, SkillContext context)
            : base(gameManager, context.SourcePlayerId) => _context = context;

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.DecideApply,
                Prompt = GameText.SkillActivatedUseIf(SourcePlayer.Name, SkillType.Po)
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count > 0 && actions[0].IsApply) _context.Pierce = true;
            GameManager.TransitionTo(new OrderingAttackState(GameManager, _context));
        }
    }
}
