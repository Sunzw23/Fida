using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Fida.UI.Rules;
using Xunit;

namespace Fida.Tests;

public class ThornSkillTests
{
    private static (GameManager game, Player attacker, Player target) Duel(List<Skill> targetSkills)
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var target = new Player(1, "Target", 11, targetSkills);
        var game = new GameManager();
        game.Initialize([attacker, target]);
        return (game, attacker, target);
    }

    private static void Strike(GameManager game, Player attacker, Player target, int damage) =>
        game.TransitionTo(new DamagingState(game,
            new SkillContext(attacker.PlayerId, damage) { TargetPlayerId = target.PlayerId }));

    [Fact]
    public void Thorn_DealsExactlyOneTrueDamageToTheAttacker()
    {
        var (game, attacker, target) = Duel([new CiSkill()]);

        Strike(game, attacker, target, 5);

        Assert.Equal(6, target.Health);
        Assert.Equal(10, attacker.Health);
    }

    [Fact]
    public void Thorn_IgnoresTheAttackersDefenceSkills()
    {
        var attacker = new Player(0, "Attacker", 11, [new DoubleDefenceSkill()]);
        var target = new Player(1, "Target", 11, [new CiSkill()]);
        var game = new GameManager();
        game.Initialize([attacker, target]);

        Strike(game, attacker, target, 4);

        Assert.Equal(7, target.Health);
        Assert.Equal(10, attacker.Health);
    }

    [Fact]
    public void ShieldedAttack_DoesNotTriggerThorn()
    {
        var (game, attacker, target) = Duel([new CiSkill()]);
        target.SetNumber(0, 0);

        Strike(game, attacker, target, 5);

        Assert.Equal(11, target.Health);
        Assert.Equal(11, attacker.Health);
    }

    [Fact]
    public void WardedAttack_DoesNotTriggerThorn()
    {
        var (game, attacker, target) = Duel([new CiSkill()]);
        target.WardsFirstHit = true;

        Strike(game, attacker, target, 5);

        Assert.Equal(11, target.Health);
        Assert.Equal(11, attacker.Health);
    }

    [Fact]
    public void Thorn_IsSelectable_CostsTwoSp_AndHasAnIconAndCodexPage()
    {
        Assert.Contains(SkillType.Ci, SkillFactory.SelectableSkills);
        Assert.IsType<CiSkill>(SkillFactory.CreateSkill(SkillType.Ci));
        Assert.True(SkillCatalog.FromName("刺") == SkillType.Ci);
        Assert.True(SkillCatalog.FromName("Thorn") == SkillType.Ci);
        Assert.Equal(2, CampaignSkillBalance.Cost(SkillType.Ci));
        Assert.Equal("T", UISkillIconCatalog.Glyph(SkillType.Ci));
        Assert.True(SkillCodexText.HasCopy(SkillType.Ci));
        Assert.Equal(SkillTrigger.Damaged, SkillFactory.CreateSkill(SkillType.Ci).Trigger);
    }
}
