using System.Collections.Generic;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class EffectNumberConsumptionTests
{
    [Fact]
    public void PairAttack_AnnouncesConsumptionBeforeResettingNumbers()
    {
        var source = new Player(0, "Source", 11, []);
        var target = new Player(1, "Target", 11, []);
        source.SetNumber(0, 1);
        source.SetNumber(1, 6);
        var game = CreateGame(source, target);
        var order = new List<string>();
        EffectNumbersConsumedEventArgs? consumed = null;
        game.EffectNumbersConsumed += (_, e) =>
        {
            consumed = e;
            order.Add("consume");
        };
        source.NumberChanged += (_, _, _, _) => order.Add("reset");

        game.TransitionTo(new ApplyingEffectState(game, source.PlayerId));

        Assert.NotNull(consumed);
        Assert.Equal([0, 1], consumed.NumberIndices);
        Assert.Equal("consume", order[0]);
        Assert.Equal(["consume", "reset", "reset"], order);
        Assert.Equal(1, source.Numbers[0]);
        Assert.Equal(1, source.Numbers[1]);
    }

    [Fact]
    public void SingleHeal_AnnouncesOnlyConsumedNumber()
    {
        var source = new Player(0, "Source", 8, []);
        var target = new Player(1, "Target", 11, []);
        source.SetNumber(0, 9);
        source.SetNumber(1, 4);
        var game = CreateGame(source, target);
        EffectNumbersConsumedEventArgs? consumed = null;
        game.EffectNumbersConsumed += (_, e) => consumed = e;

        game.TransitionTo(new ApplyingEffectState(game, source.PlayerId));

        Assert.Equal([0], consumed!.NumberIndices);
        Assert.Equal(1, source.Numbers[0]);
        Assert.Equal(4, source.Numbers[1]);
        Assert.IsType<TargetSelectionState>(game.CurrentState);
    }

    private static GameManager CreateGame(Player source, Player target)
    {
        var game = new GameManager();
        game.Initialize([source, target],
            [new Group(0, "Source", [source]), new Group(1, "Target", [target])]);
        return game;
    }
}
