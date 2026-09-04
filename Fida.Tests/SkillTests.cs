using Fida.Logic;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// 覆盖 Skill 的用法状态（SkillUsageState）与技能上下文派生（SkillContext.Derive）。
/// </summary>
public class SkillTests
{
    [Fact]
    public void Unlimited_UsageAlwaysCanUse()
    {
        var u = new SkillUsageState(); // 默认 Unlimited
        u.MarkUsed();
        Assert.True(u.CanUse());
    }

    [Fact]
    public void PerTurn_SingleUseThenResets()
    {
        var u = new SkillUsageState(SkillUsageLimit.PerTurn);
        Assert.Equal(1, u.MaxUses);
        Assert.Equal(1, u.RemainingUses);
        Assert.True(u.CanUse());

        u.MarkUsed();
        Assert.False(u.CanUse());
        Assert.True(u.IsActivated);
        Assert.Equal(0, u.RemainingUses);

        u.Reset();
        Assert.True(u.CanUse());
        Assert.False(u.IsActivated);
        Assert.Equal(1, u.RemainingUses);
    }

    [Fact]
    public void Limited_MultipleUses()
    {
        var u = new SkillUsageState(maxUses: 3);
        Assert.Equal(3, u.MaxUses);
        Assert.True(u.CanUse());

        u.MarkUsed();
        Assert.Equal(2, u.RemainingUses);
        u.MarkUsed();
        Assert.Equal(1, u.RemainingUses);
        u.MarkUsed();
        Assert.Equal(0, u.RemainingUses);
        Assert.False(u.CanUse());

        u.Reset();
        Assert.Equal(3, u.RemainingUses);
        Assert.True(u.CanUse());
    }

    [Fact]
    public void Skill_CanUse_FalseWhenUsageExhausted()
    {
        var skill = new PlusAttackSkill();
        skill.Usage = new SkillUsageState(SkillUsageLimit.PerTurn);
        skill.Usage.MarkUsed(); // 用完
        var p = new Player(0, "a");
        Assert.False(skill.CanUse(null, p));
    }

    [Fact]
    public void Skill_CanUse_TrueWhenUsageAvailable()
    {
        var skill = new PlusAttackSkill(); // 默认 Unlimited
        var p = new Player(0, "a");
        Assert.True(skill.CanUse(null, p));
    }

    [Fact]
    public void Skill_TypesAndTriggers_AreCorrect()
    {
        Assert.Equal(SkillType.PlusAttack, new PlusAttackSkill().Type);
        Assert.Equal(SkillTrigger.OrderingAttack, new PlusAttackSkill().Trigger);

        Assert.Equal(SkillType.PlusDefence, new PlusDefenceSkill().Type);
        Assert.Equal(SkillTrigger.Defending, new PlusDefenceSkill().Trigger);

        Assert.Equal(SkillType.DoubleHealth, new DoubleHealthSkill().Type);
        Assert.Equal(SkillTrigger.GameStart, new DoubleHealthSkill().Trigger);
    }

    [Fact]
    public void CreateState_ReturnsStateInstance()
    {
        var gm = new GameManager();
        var ctx = new SkillContext(0);
        var s = new PlusAttackSkill().CreateState(gm, ctx);
        Assert.NotNull(s);
        Assert.IsAssignableFrom<GameState>(s);
    }

    [Fact]
    public void Context_Derive_SwapsSourceButKeepsPipelineOrigin()
    {
        var ctx = new SkillContext(0, damage: 5) { TargetPlayerId = 1, Healing = 2 };
        var derived = ctx.Derive(2);

        Assert.Equal(2, derived.SourcePlayerId);
        Assert.Equal(0, derived.PipelineOriginId);
        Assert.Equal(1, derived.TargetPlayerId);
        Assert.Equal(5, derived.Damage);
        Assert.Equal(2, derived.Healing);
    }

    [Fact]
    public void Context_InitialSourceMatchesPipelineOrigin()
    {
        var ctx = new SkillContext(7);
        Assert.Equal(7, ctx.SourcePlayerId);
        Assert.Equal(7, ctx.PipelineOriginId);
    }
}
