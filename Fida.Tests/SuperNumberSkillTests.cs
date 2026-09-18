using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class SuperNumberSkillTests
{
    [Theory]
    [InlineData(SkillType.SuperGod, SkillTrigger.Damaged)]
    [InlineData(SkillType.SuperImmortal, SkillTrigger.OrderedAttack)]
    [InlineData(SkillType.SuperEmperor, SkillTrigger.TurnStart)]
    public void SuperSkill_RewritesBothNumbersAndChecksEffects(
        SkillType type, SkillTrigger trigger)
    {
        var source = new Player(0, "Source", 11, []);
        var opponent = new Player(1, "Opponent", 11, []);
        var game = new GameManager();
        game.Initialize([source, opponent]);
        Skill skill = SkillFactory.CreateSkill(type);

        Assert.Equal(trigger, skill.Trigger);
        game.TransitionTo(skill.CreateState(game, new SkillContext(source.PlayerId)));
        game.UpdateGame(new GameAction(ActionType.ChooseNumber) { Number = 2 });
        game.UpdateGame(new GameAction(ActionType.ChooseNumber) { Number = 3 });

        Assert.Equal(2, source.Numbers[0]);
        Assert.Equal(3, source.Numbers[1]);
    }

    [Theory]
    [InlineData(SkillType.SuperGod)]
    [InlineData(SkillType.SuperImmortal)]
    [InlineData(SkillType.SuperEmperor)]
    public void SuperSkill_CostsSixSp(SkillType type)
    {
        Assert.Equal(6, CampaignSkillBalance.Cost(type));
    }

    [Fact]
    public void SuperAndBaseVersions_Conflict()
    {
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.Shen], SkillType.SuperGod));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.SuperGod], SkillType.Shen));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.Xian], SkillType.SuperImmortal));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.SuperImmortal], SkillType.Xian));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.Di], SkillType.SuperEmperor));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.SuperEmperor], SkillType.Di));
    }
}
