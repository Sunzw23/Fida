using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class ExtraNumberOperationSkillTests
{
    [Theory]
    [InlineData(SkillType.Seng, SkillTrigger.Damaged)]
    [InlineData(SkillType.Dao, SkillTrigger.AttackCompleted)]
    public void SkillsAreSelectableAndLimitedToOncePerTurn(SkillType type, SkillTrigger trigger)
    {
        var skill = SkillFactory.CreateSkill(type);

        Assert.Contains(type, SkillFactory.SelectableSkills);
        Assert.Equal(trigger, skill.Trigger);
        Assert.Equal(SkillUsageLimit.PerTurn, skill.Usage.UsageLimit);
        Assert.Equal(2, CampaignSkillBalance.Cost(type));
    }

    [Fact]
    public void BothSkillsAreAlternativesToTheirStrongerCounterparts()
    {
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.Shen], SkillType.Seng));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.Seng], SkillType.Shen));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.Xian], SkillType.Dao));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.Dao], SkillType.Xian));
    }

    [Fact]
    public void ExtraActionUsesTheNormalNumberAdditionFlow()
    {
        var source = new Player(0, "Source");
        var other = new Player(1, "Other");
        var game = new GameManager();
        game.Initialize([source, other]);
        var skill = SkillFactory.CreateSkill(SkillType.Seng);

        game.TransitionTo(skill.CreateState(game, new SkillContext(source.PlayerId)));
        Assert.Equal(ActionType.ChooseNumberIndex, game.CurrentActionType);

        game.UpdateGame(new GameAction(ActionType.ChooseNumberIndex)
        {
            TargetPlayerId = source.PlayerId,
            NumberIndex = 0
        });
        game.UpdateGame(new GameAction(ActionType.ChooseNumberIndex)
        {
            TargetPlayerId = other.PlayerId,
            NumberIndex = 1
        });

        Assert.Equal(2, source.Numbers[0]);
        Assert.Equal(1, source.Numbers[1]);
        Assert.False(skill.Usage.CanUse());
    }

    [Fact]
    public void DaoResolvesAfterDamageAndTargetReactionFinishes()
    {
        var dao = new DaoSkill();
        var seng = new SengSkill();
        var attacker = new Player(0, "Attacker", 11, [dao]);
        var target = new Player(1, "Target", 11, [seng]);
        attacker.SetNumber(0, 1);
        attacker.SetNumber(1, 6);
        var game = new GameManager();
        game.Initialize([attacker, target]);

        game.TransitionTo(new ApplyingEffectState(game, attacker.PlayerId));
        game.UpdateGame(new GameAction(ActionType.ChooseTarget)
        {
            TargetPlayerId = target.PlayerId
        });

        Assert.Equal(9, target.Health);
        Assert.True(seng.Usage.IsActivated);
        Assert.False(dao.Usage.IsActivated);

        // Finish Seng's extra operation; Dao should only start after that reaction resolves.
        game.UpdateGame(new GameAction(ActionType.ChooseNumberIndex)
        {
            TargetPlayerId = target.PlayerId,
            NumberIndex = 0
        });
        game.UpdateGame(new GameAction(ActionType.ChooseNumberIndex)
        {
            TargetPlayerId = attacker.PlayerId,
            NumberIndex = 0
        });

        Assert.True(dao.Usage.IsActivated);
        Assert.Equal(ActionType.ChooseNumberIndex, game.CurrentActionType);
    }

    [Fact]
    public void PostAttackSkillsResolveInLoadoutOrder()
    {
        var xian = new XianSkill();
        var dao = new DaoSkill();
        var attacker = new Player(0, "Attacker", 11, [xian, dao]);
        var target = new Player(1, "Target", 11, []);
        attacker.SetNumber(0, 1);
        attacker.SetNumber(1, 6);
        var game = new GameManager();
        game.Initialize([attacker, target]);

        game.TransitionTo(new ApplyingEffectState(game, attacker.PlayerId));
        game.UpdateGame(new GameAction(ActionType.ChooseTarget)
        {
            TargetPlayerId = target.PlayerId
        });

        Assert.Contains(GameText.SkillName(SkillType.Xian), game.GameMessage);
        game.UpdateGame(new GameAction(ActionType.ChooseNumberIndex)
        {
            TargetPlayerId = attacker.PlayerId,
            NumberIndex = 0
        });
        game.UpdateGame(new GameAction(ActionType.ChooseNumber) { Number = 2 });

        // Xian resolves first; after its number change is processed, Dao starts its
        // normal two-number operation.
        Assert.Equal(ActionType.ChooseNumberIndex, game.CurrentActionType);
        game.UpdateGame(new GameAction(ActionType.ChooseNumberIndex)
        {
            TargetPlayerId = attacker.PlayerId,
            NumberIndex = 1
        });
        game.UpdateGame(new GameAction(ActionType.ChooseNumberIndex)
        {
            TargetPlayerId = attacker.PlayerId,
            NumberIndex = 0
        });

        Assert.Equal(3, attacker.Numbers[1]);
    }
}
