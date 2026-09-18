using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class GameEndTests
{
    [Fact]
    public void GroupBattle_DoesNotEndWhileEveryTeamHasPositiveHealth()
    {
        var local = new Player(0, "The Arithmetist", 11, []);
        var enemy = new Player(1, "The Withered King", 3, []);
        var game = new GameManager();
        game.Initialize(
            [local, enemy],
            [new Group(0, "The Arithmetist", [local]), new Group(1, "Echoes", [enemy])]);
        int endedCount = 0;
        game.GameEnded += (_, _) => endedCount++;

        game.UpdateGame(new GameAction(ActionType.None));

        Assert.False(game.IsGameEnded);
        Assert.Equal(0, endedCount);
        Assert.True(enemy.IsAlive());
    }

    [Fact]
    public void GroupBattle_EndsAtZeroAndIsLockedBeforeEventIsRaised()
    {
        var local = new Player(0, "The Arithmetist", 11, []);
        var enemy = new Player(1, "The Withered King", 3, []);
        var game = new GameManager();
        game.Initialize(
            [local, enemy],
            [new Group(0, "The Arithmetist", [local]), new Group(1, "Echoes", [enemy])]);
        GameEndEventArgs? result = null;
        bool endedWasLockedInsideEvent = false;
        game.GameEnded += (_, e) =>
        {
            result = e;
            endedWasLockedInsideEvent = game.IsGameEnded;
        };

        enemy.TakeDamage(enemy.Health);
        game.UpdateGame(new GameAction(ActionType.None));

        Assert.False(enemy.IsAlive());
        Assert.True(game.IsGameEnded);
        Assert.True(endedWasLockedInsideEvent);
        Assert.Equal(local.PlayerId, result!.WinnerPlayerId);
    }

    [Fact]
    public void DeferredEndCheck_WaitsForImpactPresentation()
    {
        var local = new Player(0, "Attacker", 11, []);
        var enemy = new Player(1, "Target", 1, []);
        var game = new GameManager();
        game.Initialize(
            [local, enemy],
            [new Group(0, "Allies", [local]), new Group(1, "Enemies", [enemy])]);

        game.DeferGameEndCheck();
        enemy.TakeDamage(1);
        game.UpdateGame(new GameAction(ActionType.None));
        Assert.False(game.IsGameEnded);

        game.ResumeGameEndCheck();
        Assert.True(game.IsGameEnded);
    }
}
