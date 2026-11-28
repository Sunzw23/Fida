using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Fida.UI.Rules;
using Xunit;

namespace Fida.Tests;

public class PierceSkillTests
{
    private static (GameManager game, Player attacker, Player target) Duel(List<Skill> targetSkills)
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var target = new Player(1, "Target", 11, targetSkills);
        var game = new GameManager();
        game.Initialize([attacker, target]);
        return (game, attacker, target);
    }

    private static void Strike(GameManager game, Player attacker, Player target, int damage, bool pierce = true) =>
        game.TransitionTo(new DamagingState(game,
            new SkillContext(attacker.PlayerId, damage)
            {
                TargetPlayerId = target.PlayerId,
                Pierce = pierce
            }));

    [Fact]
    public void Pierce_LandsThroughAShield_WithoutSpendingIt()
    {
        var (game, attacker, target) = Duel([]);
        target.SetNumber(0, 0);

        Strike(game, attacker, target, 5);

        Assert.Equal(6, target.Health);
        Assert.Equal(0, target.Numbers[0]);
    }

    [Fact]
    public void WithoutPierce_TheShieldAbsorbsTheAttack()
    {
        var (game, attacker, target) = Duel([]);
        target.SetNumber(0, 0);

        Strike(game, attacker, target, 5, pierce: false);

        Assert.Equal(11, target.Health);
        Assert.Equal(1, target.Numbers[0]);
    }

    [Fact]
    public void Pierce_IgnoresEveryDefendingSkill()
    {
        var (game, attacker, target) = Duel([new DoubleDefenceSkill(), new PlusDefenceSkill()]);

        Strike(game, attacker, target, 5);

        Assert.Equal(6, target.Health);
    }

    [Fact]
    public void Pierce_StillRespectsTheGatePassive()
    {
        var (game, attacker, target) = Duel([]);
        target.WardsFirstHit = true;

        Strike(game, attacker, target, 5);

        Assert.Equal(11, target.Health);
    }

    [Fact]
    public void Pierce_StillTriggersPostDamageReactions()
    {
        var (game, attacker, target) = Duel([new CiSkill()]);

        Strike(game, attacker, target, 5);

        Assert.Equal(6, target.Health);
        Assert.Equal(10, attacker.Health);
    }

    [Fact]
    public void PierceSkillState_ArmsTheContextWhenConfirmed()
    {
        var (game, attacker, target) = Duel([]);
        var context = new SkillContext(attacker.PlayerId, 4) { TargetPlayerId = target.PlayerId };
        var state = new PoSkillState(game, context);
        game.TransitionTo(state);

        state.Handle(new GameAction(ActionType.DecideApply) { IsApply = true });

        Assert.True(context.Pierce);
    }

    [Fact]
    public void Pierce_IsSelectable_CostsOneSp_AndHasAnIconAndCodexPage()
    {
        Assert.Contains(SkillType.Po, SkillFactory.SelectableSkills);
        Assert.IsType<PoSkill>(SkillFactory.CreateSkill(SkillType.Po));
        Assert.True(SkillCatalog.FromName("破") == SkillType.Po);
        Assert.True(SkillCatalog.FromName("Pierce") == SkillType.Po);
        Assert.Equal(1, CampaignSkillBalance.Cost(SkillType.Po));
        Assert.Equal("P", UISkillIconCatalog.Glyph(SkillType.Po));
        Assert.True(SkillCodexText.HasCopy(SkillType.Po));
        Assert.Equal(SkillTrigger.OrderingAttack, SkillFactory.CreateSkill(SkillType.Po).Trigger);
    }
}
