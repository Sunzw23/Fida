using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>Changes both numbers after the owner takes damage.</summary>
    public sealed class SuperGodSkill : Skill
    {
        public SuperGodSkill()
        {
            Type = SkillType.SuperGod;
            Trigger = SkillTrigger.Damaged;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context) =>
            new DualNumberRewriteState(gameManager, context.SourcePlayerId, Type);
    }

    /// <summary>Changes both numbers after the owner completes an attack.</summary>
    public sealed class SuperImmortalSkill : Skill
    {
        public SuperImmortalSkill()
        {
            Type = SkillType.SuperImmortal;
            Trigger = SkillTrigger.OrderedAttack;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context) =>
            new DualNumberRewriteState(gameManager, context.SourcePlayerId, Type);
    }

    /// <summary>Changes both numbers at the start of the owner's turn.</summary>
    public sealed class SuperEmperorSkill : Skill
    {
        public SuperEmperorSkill()
        {
            Type = SkillType.SuperEmperor;
            Trigger = SkillTrigger.TurnStart;
            Usage = new SkillUsageState();
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context) =>
            new DualNumberRewriteState(gameManager, context.SourcePlayerId, Type);
    }

    /// <summary>
    /// Shared two-step interaction for high-tier number rewriting. Values are collected
    /// before either number changes, then committed together and checked once.
    /// </summary>
    public sealed class DualNumberRewriteState : GameState
    {
        private readonly SkillType _skillType;

        public DualNumberRewriteState(GameManager gameManager, int sourcePlayerId,
            SkillType skillType) : base(gameManager, sourcePlayerId) => _skillType = skillType;

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = Enumerable.Range(1, 9).ToList(),
                Prompt = $"{SourcePlayer.Name} triggered {GameText.SkillName(_skillType)}. Choose the left number."
            });
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumber,
                ValidNumbers = Enumerable.Range(1, 9).ToList(),
                Prompt = $"{SourcePlayer.Name} triggered {GameText.SkillName(_skillType)}. Choose the right number."
            });
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count < 2) return;
            SourcePlayer.SetNumber(0, actions[0].Number);
            SourcePlayer.SetNumber(1, actions[1].Number);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
