using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class NumberPairAttackTests
{
    [Theory]
    [InlineData(1, 6)]
    [InlineData(6, 1)]
    public void OneAndSix_DealsTwoDamageAfterTargetSelection(int first, int second)
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var target = new Player(1, "Target", 11, []);
        attacker.SetNumber(0, first);
        attacker.SetNumber(1, second);
        var game = new GameManager();
        game.Initialize([attacker, target]);

        game.TransitionTo(new ApplyingEffectState(game, attacker.PlayerId));

        var targetSelection = Assert.IsType<TargetSelectionState>(game.CurrentState);
        Assert.Equal(2, targetSelection.DamageValue);
        game.UpdateGame(new GameAction(ActionType.ChooseTarget)
        {
            TargetPlayerId = target.PlayerId
        });

        Assert.Equal(9, target.Health);
        Assert.Equal(1, attacker.Numbers[0]);
        Assert.Equal(1, attacker.Numbers[1]);
    }
}
