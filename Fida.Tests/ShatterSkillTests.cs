using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Fida.UI.Rules;
using Xunit;

namespace Fida.Tests;

public class ShatterSkillTests
{
    private static (GameManager game, Player attacker, Player target) Duel(List<Skill> targetSkills)
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var target = new Player(1, "Target", 11, targetSkills);
        var game = new GameManager();
        game.Initialize([attacker, target]);
        return (game, attacker, target);
    }

    private static GameManager Strike(GameManager game, Player attacker, Player target, int damage = 5)
    {
        game.TransitionTo(new DamagingState(game,
            new SkillContext(attacker.PlayerId, damage) { TargetPlayerId = target.PlayerId }));
        return game;
    }

    [Fact]
    public void Shatter_TurnsTheBrokenShieldIntoAChosenNumber()
    {
        var (game, attacker, target) = Duel([new ShatterSkill()]);
        target.SetNumber(0, 0);

        Strike(game, attacker, target);
        game.UpdateGame(new GameAction(ActionType.ChooseNumber) { Number = 1 });

        Assert.Equal(11, target.Health);   // the shield still absorbed the hit
        Assert.Equal(1, target.Numbers[0]);
    }

    [Fact]
    public void Shatter_ReChecksTheEffectTable_SoTheNumberCanAttack()
    {
        var (game, attacker, target) = Duel([new ShatterSkill()]);
        target.SetNumber(0, 0);            // shield on the left
        target.SetNumber(1, 6);            // 6 on the right

        Strike(game, attacker, target);
        Assert.Equal(11, target.Health);

        game.UpdateGame(new GameAction(ActionType.ChooseNumber) { Number = 6 }); // 6 and 6
        Assert.IsType<TargetSelectionState>(game.CurrentState);

        game.UpdateGame(new GameAction(ActionType.ChooseTarget) { TargetPlayerId = attacker.PlayerId });
        Assert.Equal(6, attacker.Health);  // 6 and 6 deals 5
    }

    [Fact]
    public void Shatter_NeverOffersZero()
    {
        var (game, attacker, target) = Duel([new ShatterSkill()]);
        target.SetNumber(0, 0);

        Strike(game, attacker, target);

        Assert.Equal(9, game.CurrentStepValidNumbers.Count);
        Assert.DoesNotContain(0, game.CurrentStepValidNumbers);
        Assert.Equal(1, game.CurrentStepValidNumbers[0]);
        Assert.Equal(9, game.CurrentStepValidNumbers[^1]);
    }

    [Fact]
    public void WithoutShatter_TheBrokenShieldFallsToOne()
    {
        var (game, attacker, target) = Duel([]);
        target.SetNumber(0, 0);

        Strike(game, attacker, target);

        Assert.Equal(1, target.Numbers[0]);
        Assert.Empty(game.CurrentStepValidNumbers);
    }

    [Fact]
    public void RewriteSkills_OnlyOfferOneToNine()
    {
        foreach (var type in new[] { SkillType.Shen, SkillType.Xian, SkillType.Di })
        {
            var player = new Player(0, "Owner", 11, []);
            var game = new GameManager();
            game.Initialize([player, new Player(1, "Other", 11, [])]);
            var skill = SkillFactory.CreateSkill(type);
            game.TransitionTo(skill.CreateState(game, new SkillContext(player.PlayerId))!);
            game.UpdateGame(new GameAction(ActionType.ChooseNumberIndex) { NumberIndex = 0 });

            Assert.Equal(9, game.CurrentStepValidNumbers.Count);
            Assert.DoesNotContain(0, game.CurrentStepValidNumbers);
        }
    }

    [Fact]
    public void Shatter_IsSelectable_CostsTwoSp_AndHasAnIconAndCodexPage()
    {
        Assert.Contains(SkillType.Shatter, SkillFactory.SelectableSkills);
        Assert.IsType<ShatterSkill>(SkillFactory.CreateSkill(SkillType.Shatter));
        Assert.True(SkillFactory.CreateSkill(SkillType.Shatter).ReforgesBrokenShield);
        Assert.True(SkillCatalog.FromName("裂") == SkillType.Shatter);
        Assert.Equal(2, CampaignSkillBalance.Cost(SkillType.Shatter));
        Assert.True(CampaignSkillBalance.Cost(SkillType.Shatter) < CampaignSkillBalance.Cost(SkillType.Shen));
        Assert.Equal("SH", UISkillIconCatalog.Glyph(SkillType.Shatter));
        Assert.True(SkillCodexText.HasCopy(SkillType.Shatter));
    }
}
