using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class CardActionTests
{
    [Fact]
    public void AttackTarget_CanBeTheAttackerItself()
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var enemy = new Player(1, "Enemy", 11, []);
        var game = new GameManager();
        game.Initialize([attacker, enemy],
            [new Group(0, "Allies", [attacker]), new Group(1, "Enemies", [enemy])]);
        PlayerActionRequiredEventArgs? requested = null;
        game.PlayerActionRequired += (_, e) => requested = e;
        var state = new TargetSelectionState(game,
            new SkillContext(attacker.PlayerId, damage: 2));

        state.Enter();

        Assert.Contains(attacker.PlayerId, requested!.ValidTargets);
        state.Handle(new GameAction(ActionType.ChooseTarget)
        {
            TargetPlayerId = attacker.PlayerId
        });
        Assert.Equal(9, attacker.Health);
    }

    [Fact]
    public void ValidatedAttackTarget_RaisesCardActionWithCorrectDirection()
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var target = new Player(1, "Target", 5, []);
        var game = new GameManager();
        game.Initialize(
            [attacker, target],
            [new Group(0, "Allies", [attacker]), new Group(1, "Enemies", [target])]);
        CardActionEventArgs? presentation = null;
        game.CardActionStarted += (_, e) => presentation = e;
        var state = new TargetSelectionState(game,
            new SkillContext(attacker.PlayerId, damage: 2));
        state.Enter();

        state.Handle(new GameAction(ActionType.ChooseTarget)
        {
            TargetPlayerId = target.PlayerId
        });

        Assert.NotNull(presentation);
        Assert.Equal(attacker.PlayerId, presentation!.SourcePlayerId);
        Assert.Equal(target.PlayerId, presentation.TargetPlayerId);
        Assert.Equal(2, presentation.Damage);
        Assert.True(presentation.IsDamaging);
    }
}
