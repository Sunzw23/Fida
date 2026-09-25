using Fida.Logic;
using Fida.Network;
using Xunit;

namespace Fida.Tests;

public class BattleSnapshotTests
{
    [Fact]
    public void Snapshot_RoundTripsBattleRosterAndMutableCombatState()
    {
        var attackUp = SkillFactory.CreateSkill(SkillType.PlusAttack);
        attackUp.Usage.MarkUsed();
        var local = new Player(0, "Arithmetist", 15, [attackUp]) { PortraitKey = "arithmetist" };
        local.SetNumber(0, 7);
        local.SetNumber(1, 2);
        local.TakeDamage(4);
        local.RecordAttackReceived();
        var enemy = new Player(3, "Sentinel", 8, [SkillFactory.CreateSkill(SkillType.Shi)]);
        enemy.SetNumber(0, 0);

        var game = new GameManager();
        game.Initialize([local, enemy],
        [
            new Group(0, "Allies", [local]),
            new Group(1, "Enemies", [enemy])
        ]);

        var captured = BattleSnapshot.Capture(game, "session-42", "node-a1");
        captured.JoiningPlayerId = 3;
        captured.BattleOwnerPlayerId = 0;
        var snapshot = BattleSnapshot.Deserialize(captured.Serialize());

        Assert.Equal("session-42", snapshot.SessionId);
        Assert.Equal("node-a1", snapshot.NodeId);
        Assert.Equal(0, snapshot.CurrentPlayerId);
        Assert.Equal(3, snapshot.JoiningPlayerId);
        Assert.Equal(0, snapshot.BattleOwnerPlayerId);
        Assert.Equal(2, snapshot.Players.Count);
        Assert.Equal(11, snapshot.Players[0].Health);
        Assert.Equal(15, snapshot.Players[0].MaxHealth);
        Assert.Equal(7, snapshot.Players[0].Number0);
        Assert.Equal(2, snapshot.Players[0].Number1);
        Assert.Equal(1, snapshot.Players[0].AttacksReceivedThisTurn);
        Assert.True(snapshot.Players[0].Skills[0].IsActivated);
        Assert.Equal(2, snapshot.Groups.Count);
        Assert.Equal(3, Assert.Single(snapshot.Groups[1].PlayerIds));

        var restored = snapshot.Players[0].CreatePlayer();
        Assert.Equal("arithmetist", restored.PortraitKey);
        Assert.Equal(11, restored.Health);
        Assert.Equal(7, restored.Numbers[0]);
        Assert.True(restored.Skills[0].Usage.IsActivated);

        var joinedGame = new GameManager();
        snapshot.RestoreInto(joinedGame, localPlayerId: 3, resume: false);
        Assert.Equal(2, joinedGame.Players.Count);
        Assert.Equal(3, joinedGame.LocalPlayerId);
        Assert.Equal(2, joinedGame.Players[0].Numbers[1]);
        Assert.Equal("Allies", joinedGame.Groups[0].Name);
    }

    [Fact]
    public void Snapshot_RejectsUnsupportedVersionAndMissingCurrentPlayer()
    {
        Assert.Throws<InvalidDataException>(() => BattleSnapshot.Deserialize([0, 0, 0, 0]));

        var invalid = new BattleSnapshot { CurrentPlayerId = 5 };
        Assert.Throws<InvalidDataException>(() => BattleSnapshot.Deserialize(invalid.Serialize()));
    }
}
