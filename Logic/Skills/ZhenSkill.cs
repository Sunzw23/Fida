namespace Fida.Logic
{
    /// <summary>
    /// Every attack this player makes resolves as true damage: no shield, no defense skill, no
    /// gate passive, and no post-damage reaction on either side. It is a passive, so it owns no
    /// state and never prompts; the damage pipeline reads <see cref="MakesAttacksTrue"/>.
    /// The trade-off is deliberate: nothing of the owner's own reacts either, so Blood stops
    /// healing with this skill equipped.
    /// </summary>
    public sealed class ZhenSkill : Skill
    {
        public ZhenSkill()
        {
            Type = SkillType.Zhen;
            Value = 1;
            Trigger = SkillTrigger.OrderingAttack;
            Usage = new SkillUsageState();
        }

        public override bool MakesAttacksTrue => true;

        /// <summary>A passive has no step of its own; every caller already handles a null state.</summary>
        public override GameState? CreateState(GameManager gameManager, SkillContext context) => null;
    }
}
