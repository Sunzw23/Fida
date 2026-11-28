using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// Answers the attack that just landed: 1 point of true damage, always aimed at the attacker,
    /// with no target prompt. True damage keeps it outside the shield and defense pipeline, so it
    /// can never bounce back the way a reflected normal attack would.
    /// </summary>
    public sealed class CiSkill : Skill
    {
        public CiSkill()
        {
            Type = SkillType.Ci;
            Value = 1;
            Trigger = SkillTrigger.Damaged;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context) =>
            new CiSkillState(gameManager, context);
    }

    public sealed class CiSkillState : GameState
    {
        private const int Retaliation = 1;
        private readonly SkillContext _context;

        public CiSkillState(GameManager gameManager, SkillContext context)
            : base(gameManager, context.SourcePlayerId) => _context = context;

        protected override void Execute(List<GameAction> actions)
        {
            Player? attacker = GameManager.Players
                .FirstOrDefault(player => player.PlayerId == _context.PipelineOriginId);

            // A self-inflicted hit, or an attacker who is already down, leaves nothing to answer.
            if (attacker == null || attacker.PlayerId == SourcePlayerId || !attacker.IsAlive())
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            GameManager.TransitionTo(new TrueDamageState(GameManager,
                new SkillContext(SourcePlayerId)
                {
                    TargetPlayerId = attacker.PlayerId,
                    Damage = Retaliation
                }));
        }
    }
}
