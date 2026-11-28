using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Fida.UI.Rules;
using Xunit;

namespace Fida.Tests;

public class ZhenSkillTests
{
    /// <summary>Runs one whole attack through target selection so the passive is exercised.</summary>
    private static GameManager Attack(Player attacker, Player target, int damage)
    {
        var game = new GameManager();
        game.Initialize([attacker, target]);
        var context = new SkillContext(attacker.PlayerId, damage) { TargetPlayerId = target.PlayerId };
        var selection = new TargetSelectionState(game, context);
        game.TransitionTo(selection);
        selection.Handle(new GameAction(ActionType.ChooseTarget) { TargetPlayerId = target.PlayerId });
        return game;
    }

    private static Player Attacker(params Skill[] skills) =>
        new(0, "Attacker", 11, [.. skills]);

    [Fact]
    public void TrueDamage_PassesTheShield_WithoutSpendingIt()
    {
        var attacker = Attacker(new ZhenSkill());
        var target = new Player(1, "Target", 11, []);
        target.SetNumber(0, 0);

        Attack(attacker, target, 5);

        Assert.Equal(6, target.Health);
        Assert.Equal(0, target.Numbers[0]);
    }

    [Fact]
    public void TrueDamage_IgnoresDefendingSkills()
    {
        var attacker = Attacker(new ZhenSkill());
        var target = new Player(1, "Target", 11, [new DoubleDefenceSkill(), new PlusDefenceSkill()]);

        Attack(attacker, target, 5);

        Assert.Equal(6, target.Health);
    }

    [Fact]
    public void TrueDamage_IgnoresTheGatePassive()
    {
        var attacker = Attacker(new ZhenSkill());
        var target = new Player(1, "Target", 11, []) { WardsFirstHit = true };

        Attack(attacker, target, 5);

        Assert.Equal(6, target.Health);
    }

    [Fact]
    public void TrueDamage_SuppressesPostDamageReactions()
    {
        var attacker = Attacker(new ZhenSkill());
        var target = new Player(1, "Target", 11, [new CiSkill()]);

        Attack(attacker, target, 5);

        Assert.Equal(6, target.Health);
        Assert.Equal(11, attacker.Health);
    }

    [Fact]
    public void TrueDamage_DisablesTheOwnersLifesteal()
    {
        // The documented cost of the passive: Blood is a reaction, so nothing reacts.
        var attacker = Attacker(new ZhenSkill(), new XueSkill());
        var target = new Player(1, "Target", 11, []);

        Attack(attacker, target, 5);

        Assert.Equal(6, target.Health);
        Assert.Equal(11, attacker.Health);
    }

    [Fact]
    public void WithoutThePassive_TheSameAttackIsReduced()
    {
        var attacker = Attacker();
        var target = new Player(1, "Target", 11, [new DoubleDefenceSkill()]);

        Attack(attacker, target, 5);

        Assert.Equal(9, target.Health);
    }

    [Fact]
    public void Zhen_IsSelectable_CostsThreeSp_AndConflictsWithBlood()
    {
        Assert.Contains(SkillType.Zhen, SkillFactory.SelectableSkills);
        Assert.IsType<ZhenSkill>(SkillFactory.CreateSkill(SkillType.Zhen));
        Assert.Null(SkillFactory.CreateSkill(SkillType.Zhen).CreateState(new GameManager(), new SkillContext(0)));
        Assert.True(SkillFactory.CreateSkill(SkillType.Zhen).MakesAttacksTrue);
        Assert.True(SkillCatalog.FromName("真") == SkillType.Zhen);
        Assert.Equal(3, CampaignSkillBalance.Cost(SkillType.Zhen));
        Assert.Equal("TD", UISkillIconCatalog.Glyph(SkillType.Zhen));
        Assert.True(SkillCodexText.HasCopy(SkillType.Zhen));
        Assert.False(CampaignSkillBalance.Conflicts([], SkillType.Zhen));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.Xue], SkillType.Zhen));
        Assert.True(CampaignSkillBalance.Conflicts([SkillType.Zhen], SkillType.Xue));
    }
}
