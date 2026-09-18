using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class GameEndTests
{
    [Fact]
    public void LethalDamage_DoesNotAllowGodOwnerToAct()
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var target = new Player(1, "God Owner", 1,
            [SkillFactory.CreateSkill(SkillType.Shen)]);
        var game = new GameManager();
        game.Initialize(
            [attacker, target],
            [new Group(0, "Allies", [attacker]), new Group(1, "Enemies", [target])]);
        int deadActorRequests = 0;
        game.PlayerActionRequired += (_, args) =>
        {
            if (args.SourcePlayerId == target.PlayerId)
                deadActorRequests++;
        };
        var context = new SkillContext(attacker.PlayerId, damage: 1)
        {
            TargetPlayerId = target.PlayerId
        };

        game.TransitionTo(new PreDamageState(game, context));

        Assert.False(target.IsAlive());
        Assert.Equal(0, deadActorRequests);
        Assert.DoesNotContain(game.StateStack,
            state => state.ActorId == target.PlayerId);
    }

    [Fact]
    public void QueuedInteractiveState_IsSkippedWhenItsActorHasDied()
    {
        var survivor = new Player(0, "Survivor", 11, []);
        var dead = new Player(1, "Dead God", 1, []);
        var game = new GameManager();
        game.Initialize([survivor, dead]);
        dead.TakeDamage(1);
        int deadActorRequests = 0;
        game.PlayerActionRequired += (_, args) =>
        {
            if (args.SourcePlayerId == dead.PlayerId)
                deadActorRequests++;
        };

        game.TransitionTo(new ShenSkillState(game, new SkillContext(dead.PlayerId)));

        Assert.Equal(0, deadActorRequests);
        Assert.IsNotType<ShenSkillState>(game.CurrentState);
    }

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
