using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class StoneSkillTests
{
    private sealed class CounterProbeState(GameManager game, Player player, System.Action<int> capture)
        : GameState(game)
    {
        public override void Enter() => capture(player.AttacksReceivedThisTurn);
    }

    [Fact]
    public void FirstAttackDealsDamage_SubsequentAttackIsBlocked()
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var target = new Player(1, "Target", 11, [new StoneSkill()]);
        var game = new GameManager();
        game.Initialize([attacker, target]);

        var firstAttack = new SkillContext(attacker.PlayerId, 3) { TargetPlayerId = target.PlayerId };
        game.TransitionTo(new StoneSkillState(game, firstAttack.Derive(target.PlayerId)));
        Assert.Equal(8, target.Health);

        // Recreate the same-turn prior-attack condition after the state stack unwinds.
        target.RecordAttackReceived();
        int healthBefore = target.Health;
        var secondAttack = new SkillContext(attacker.PlayerId, 3) { TargetPlayerId = target.PlayerId };
        game.TransitionTo(new StoneSkillState(game, secondAttack.Derive(target.PlayerId)));
        Assert.Equal(healthBefore, target.Health);
    }

    [Fact]
    public void Stone_IsSelectableAndCostsTwoSp()
    {
        Assert.Contains(SkillType.Shi, SkillFactory.SelectableSkills);
        Assert.IsType<StoneSkill>(SkillFactory.CreateSkill(SkillType.Shi));
        Assert.Equal(2, CampaignSkillBalance.Cost(SkillType.Shi));
    }

    [Fact]
    public void ShieldedAttack_DoesNotConsumeStoneAttackAllowance()
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var target = new Player(1, "Target", 11, [new StoneSkill()]);
        target.SetNumber(0, 0);
        var game = new GameManager();
        game.Initialize([attacker, target]);
        int observedCount = -1;
        game.PushState(new CounterProbeState(game, target, value => observedCount = value));

        game.TransitionTo(new DamagingState(game,
            new SkillContext(attacker.PlayerId, 3) { TargetPlayerId = target.PlayerId }));

        Assert.Equal(0, observedCount);
        Assert.Equal(11, target.Health);
    }
}
