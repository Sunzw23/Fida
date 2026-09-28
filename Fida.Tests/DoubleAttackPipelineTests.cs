using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class DoubleAttackPipelineTests
{
    [Fact]
    public void DoubleAttack_AloneDoublesTriggeredDamage()
    {
        var source = new Player(0, "Source", 11, [new DoubleAttackSkill()]);
        var target = new Player(1, "Target", 11, []);
        var game = CreateGame(source, target);
        var context = new SkillContext(source.PlayerId, damage: 2, numbers: [1, 6]);

        game.TransitionTo(new OrderingAttackState(game, context));
        ApplyCurrentDecision(game, source.PlayerId);

        var selection = Assert.IsType<TargetSelectionState>(game.CurrentState);
        Assert.Equal(4, selection.DamageValue);
    }

    [Fact]
    public void AttackModifiersResolveInLoadoutOrder()
    {
        // Transform is selected second and therefore replaces the doubled damage.
        var source = new Player(0, "Source", 11,
            [new DoubleAttackSkill(), new BianSkill()]);
        var target = new Player(1, "Target", 11, []);
        var game = CreateGame(source, target);
        var context = new SkillContext(source.PlayerId, damage: 2, numbers: [1, 6]);

        game.TransitionTo(new OrderingAttackState(game, context));
        Assert.IsType<DoubleAttackSkillState>(game.CurrentState);
        ApplyCurrentDecision(game, source.PlayerId);
        Assert.IsType<BianSkillState>(game.CurrentState);
        game.UpdateGame(new GameAction(ActionType.ChooseNumber) { Number = 5 });

        var selection = Assert.IsType<TargetSelectionState>(game.CurrentState);
        Assert.Equal(5, selection.DamageValue);
    }

    [Fact]
    public void DoubleAttackThenAdditiveModifierFollowLoadoutOrder()
    {
        var source = new Player(0, "Source", 11,
            [new DoubleAttackSkill(), new PlusAttackSkill()]);
        var target = new Player(1, "Target", 11, []);
        var game = CreateGame(source, target);
        var context = new SkillContext(source.PlayerId, damage: 2, numbers: [1, 6]);

        game.TransitionTo(new OrderingAttackState(game, context));
        Assert.IsType<DoubleAttackSkillState>(game.CurrentState);
        ApplyCurrentDecision(game, source.PlayerId);
        Assert.IsType<PlusAttackSkillState>(game.CurrentState);
        ApplyCurrentDecision(game, source.PlayerId);

        var selection = Assert.IsType<TargetSelectionState>(game.CurrentState);
        Assert.Equal(5, selection.DamageValue);
    }

    private static GameManager CreateGame(Player source, Player target)
    {
        var game = new GameManager();
        game.Initialize([source, target],
            [new Group(0, "Source", [source]), new Group(1, "Target", [target])]);
        return game;
    }

    private static void ApplyCurrentDecision(GameManager game, int actorId) =>
        game.UpdateGame(new GameAction(ActionType.DecideApply)
        {
            TargetPlayerId = actorId,
            IsApply = true
        });
}
