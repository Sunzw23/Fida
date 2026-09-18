using Fida.Campaign.Battle;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class DecideApplyTests
{
    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 2)]
    public void PlusAttackDecision_AppliesOrPasses(bool apply, int expectedDamage)
    {
        var source = new Player(0, "Source", 11, []);
        var target = new Player(1, "Target", 5, []);
        var game = new GameManager();
        game.Initialize(
            [source, target],
            [new Group(0, "Allies", [source]), new Group(1, "Enemies", [target])]);
        var context = new SkillContext(source.PlayerId, damage: 2);
        game.TransitionTo(new PlusAttackSkillState(game, context));

        game.UpdateGame(new GameAction(ActionType.DecideApply)
        {
            TargetPlayerId = source.PlayerId,
            IsApply = apply
        });

        Assert.Equal(expectedDamage, context.Damage);
    }

    [Fact]
    public void AutomatedDecision_UsesActorIdForValidatedDecision()
    {
        var local = new Player(0, "Local", 11, []);
        var actor = new Player(1, "Actor", 5, []);
        var game = new GameManager();
        game.Initialize(
            [local, actor],
            [new Group(0, "Allies", [local]), new Group(1, "Enemies", [actor])]);
        game.StateStack.Push(new ChooseNumberIndexState(game, actor.PlayerId));
        game.TransitionTo(new TongSkillState(game, new SkillContext(actor.PlayerId)));
        var controller = new RuleBasedCampaignController(actor.PlayerId);

        controller.RequestAction(game, new PlayerActionRequiredEventArgs
        {
            SourcePlayerId = actor.PlayerId,
            RequiredActionType = ActionType.DecideApply,
            ValidTargets = [actor.PlayerId]
        });

        Assert.IsType<ChooseNumberIndexState>(game.CurrentState);
        Assert.Equal(actor.PlayerId, game.CurrentState.ActorId);
    }
}
