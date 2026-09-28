using System.Linq;

namespace Fida.Logic
{
    /// <summary>Runs attacker skills after the selected attack and its reactions resolve.</summary>
    public sealed class AttackCompletedState : GameState
    {
        public AttackCompletedState(GameManager gameManager, int attackerId)
            : base(gameManager, attackerId) { }

        public override void Enter()
        {
            if (SourcePlayer == null || !SourcePlayer.IsAlive())
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            var triggeredStates = SourcePlayer.Skills.Where(skill =>
                         (skill.Trigger == SkillTrigger.OrderedAttack ||
                          skill.Trigger == SkillTrigger.AttackCompleted) &&
                         skill.CanUse(GameManager, SourcePlayer))
                .Select(skill => skill.CreateState(GameManager, new SkillContext(SourcePlayerId)))
                .Where(state => state != null)
                .ToList();
            GameManager.PushStatesInOrder(triggeredStates);

            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
