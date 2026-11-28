using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// When this player's shield breaks, the digit it leaves behind is chosen instead of dropping
    /// to 1, and the usual effect check runs on the result — so a shield break can turn straight
    /// into an attack. The skill reports on the "when hit" stage for the rules page but owns no
    /// state: the shield stage reads <see cref="ReforgesBrokenShield"/> and builds the choice.
    /// Zero is never offered, because a shield must not shatter into another shield.
    /// </summary>
    public sealed class ShatterSkill : Skill
    {
        public ShatterSkill()
        {
            Type = SkillType.Shatter;
            Value = 1;
            Trigger = SkillTrigger.Defending;
            Usage = new SkillUsageState();
        }

        public override bool ReforgesBrokenShield => true;

        /// <summary>A passive has no step of its own; every caller already handles a null state.</summary>
        public override GameState? CreateState(GameManager gameManager, SkillContext context) => null;
    }

    /// <summary>Asks the shield's owner what the broken digit becomes, then re-checks the effect table.</summary>
    public sealed class ShieldReforgeState : GameState
    {
        private readonly int _shieldIndex;

        public ShieldReforgeState(GameManager gameManager, int playerId, int shieldIndex)
            : base(gameManager, playerId) => _shieldIndex = shieldIndex;

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = Enumerable.Range(1, 9).ToList(),
                Prompt = $"{SourcePlayer.Name} triggered {GameText.SkillName(SkillType.Shatter)}. Choose the new number."
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count == 0)
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            SourcePlayer.SetNumber(_shieldIndex, actions[0].Number);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
