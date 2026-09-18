using System.Collections.Generic;

namespace Fida.Logic
{
    /// <summary>
    /// Allows the first incoming attack in a global turn and blocks every later one.
    /// The counter belongs to Player so multiple attackers and online play share the
    /// same authoritative rule. Shielded attacks never reach this counter.
    /// </summary>
    public sealed class StoneSkill : Skill
    {
        public StoneSkill()
        {
            Type = SkillType.Shi;
            Trigger = SkillTrigger.Defending;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context) =>
            new StoneSkillState(gameManager, context);
    }

    public sealed class StoneSkillState : GameState
    {
        private readonly SkillContext _context;

        public StoneSkillState(GameManager gameManager, SkillContext context)
            : base(gameManager, context.SourcePlayerId) => _context = context;

        protected override void Execute(List<GameAction> actions)
        {
            if (SourcePlayer.AttacksReceivedThisTurn >= 1) _context.Damage = 0;
            _context.SourcePlayerId = _context.PipelineOriginId;
            GameManager.TransitionTo(new PreDamageState(GameManager, _context));
        }
    }
}
