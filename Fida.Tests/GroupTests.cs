using Fida.Logic;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// 覆盖 Group 的成员管理与存活判定。
/// </summary>
public class GroupTests
{
    [Fact]
    public void Constructor_AcceptsPlayers()
    {
        var a = new Player(0, "a");
        var b = new Player(1, "b");
        var g = new Group(10, "team", new[] { a, b });

        Assert.Equal(10, g.GroupId);
        Assert.Equal("team", g.Name);
        Assert.Equal(2, g.Players.Count);
    }

    [Fact]
    public void AddPlayer_DoesNotDuplicate()
    {
        var a = new Player(0, "a");
        var g = new Group(1, "team");
        g.AddPlayer(a);
        g.AddPlayer(a);
        Assert.Single(g.Players);
    }

    [Fact]
    public void IsAlive_TrueIfAnyMemberAlive()
    {
        var a = new Player(0, "a");
        var b = new Player(1, "b");
        a.Health = 0; // 死亡
        b.Health = 5; // 存活
        var g = new Group(1, "team", new[] { a, b });

        Assert.True(g.IsAlive());

        b.Health = 0; // 全员死亡
        Assert.False(g.IsAlive());
    }

    [Fact]
    public void IsAlive_FalseForEmptyGroup()
    {
        var g = new Group(1, "empty");
        Assert.False(g.IsAlive());
    }

    [Fact]
    public void ChangeName_UpdatesName()
    {
        var g = new Group(1, "old");
        g.ChangeName("new");
        Assert.Equal("new", g.Name);
    }
}
