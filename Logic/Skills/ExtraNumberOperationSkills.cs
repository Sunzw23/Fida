namespace Fida.Logic
{
    /// <summary>After receiving an attack, take one extra normal number-addition action.</summary>
    public sealed class SengSkill : Skill
    {
        public SengSkill()
        {
            Type = SkillType.Seng;
            Value = 1;
            Trigger = SkillTrigger.Damaged;
            Usage = new SkillUsageState(SkillUsageLimit.PerTurn);
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            Usage.MarkUsed();
            return new ExtraNumberOperationState(gameManager, context.SourcePlayerId, Type);
        }
    }

    /// <summary>After completing an attack, take one extra normal number-addition action.</summary>
    public sealed class DaoSkill : Skill
    {
        public DaoSkill()
        {
            Type = SkillType.Dao;
            Value = 1;
            Trigger = SkillTrigger.AttackCompleted;
            Usage = new SkillUsageState(SkillUsageLimit.PerTurn);
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            Usage.MarkUsed();
            return new ExtraNumberOperationState(gameManager, context.SourcePlayerId, Type);
        }
    }

    internal sealed class ExtraNumberOperationState : GameState
    {
        private readonly SkillType _skillType;

        public ExtraNumberOperationState(GameManager gameManager, int playerId, SkillType skillType)
            : base(gameManager, playerId) => _skillType = skillType;

        public override void Enter()
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, _skillType));
            GameManager.TransitionTo(new ChooseNumberIndexState(GameManager, SourcePlayerId));
        }
    }
}
