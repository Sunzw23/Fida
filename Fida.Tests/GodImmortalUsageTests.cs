using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class GodImmortalUsageTests
{
    [Theory]
    [InlineData(SkillType.Shen)]
    [InlineData(SkillType.Xian)]
    public void Skill_CanActivateOnlyOnceDuringEachPlayerAction(SkillType type)
    {
        Skill skill = SkillFactory.CreateSkill(type);
        var owner = new Player(0, "Owner", 11, [skill]);
        var opponent = new Player(1, "Opponent", 11, []);
        var game = new GameManager();
        game.Initialize([owner, opponent]);

        Assert.Equal(SkillUsageLimit.PerTurn, skill.Usage.UsageLimit);
        Assert.True(skill.CanUse(game, owner));

        skill.CreateState(game, new SkillContext(owner.PlayerId));

        Assert.False(skill.CanUse(game, owner));
        Assert.Equal(0, skill.Usage.RemainingUses);
    }

    [Theory]
    [InlineData(SkillType.Shen)]
    [InlineData(SkillType.Xian)]
    public void Skill_ResetsWhenTheNextPlayerActionBegins(SkillType type)
    {
        Skill skill = SkillFactory.CreateSkill(type);
        var owner = new Player(0, "Owner", 11, [skill]);
        var opponent = new Player(1, "Opponent", 11, []);
        var game = new GameManager();
        game.Initialize([owner, opponent]);
        skill.CreateState(game, new SkillContext(owner.PlayerId));

        game.TransitionTo(new TurnStartState(game, opponent.PlayerId));

        Assert.True(skill.CanUse(game, owner));
        Assert.Equal(1, skill.Usage.RemainingUses);
    }
}
