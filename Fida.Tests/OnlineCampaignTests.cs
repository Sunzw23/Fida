using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class OnlineCampaignTests
{
    private static CampaignNode Node(string id) => Assert.IsType<CampaignNode>(CampaignCatalog.GetNode(id));
    [Fact]
    public void CooperativeCampaign_CreatesTwoAlliesAndKeepsEnemiesOnOpposingTeam()
    {
        var node = Node("A1");
        var setup = CampaignBattleFactory.CreateCooperative(node,
            [SkillType.PlusAttack], [SkillType.PlusDefence]);

        Assert.Equal(new[] { 0, 1 }, setup.Groups[0].Players.Select(player => player.PlayerId));
        Assert.Equal(node.Enemies.Count, setup.Groups[1].Players.Count);
        Assert.All(setup.Groups[1].Players, enemy => Assert.True(enemy.PlayerId >= 2));
        Assert.Contains(setup.Players[0].Skills, skill => skill.Type == SkillType.PlusAttack);
        Assert.Contains(setup.Players[1].Skills, skill => skill.Type == SkillType.PlusDefence);
    }

    [Fact]
    public void CooperativeCampaign_HostAndClientBuildIdenticalStableRoster()
    {
        var node = Node("A1");
        var host = CampaignBattleFactory.CreateCooperative(node,
            [SkillType.PlusAttack], [SkillType.PlusDefence]);
        var client = CampaignBattleFactory.CreateCooperative(node,
            [SkillType.PlusAttack], [SkillType.PlusDefence]);

        Assert.Equal(host.Players.Select(PlayerSignature), client.Players.Select(PlayerSignature));
        Assert.Equal(host.Groups.SelectMany(group => group.Players.Select(player => player.PlayerId)),
            client.Groups.SelectMany(group => group.Players.Select(player => player.PlayerId)));

        var hostGame = new GameManager();
        var clientGame = new GameManager();
        hostGame.Initialize(host.Players, host.Groups);
        clientGame.Initialize(client.Players, client.Groups);
        Assert.Equal(0, hostGame.GetCurrentPlayer().PlayerId);
        Assert.Equal(hostGame.GetCurrentPlayer().PlayerId, clientGame.GetCurrentPlayer().PlayerId);
    }

    private static string PlayerSignature(Player player) =>
        $"{player.PlayerId}:{player.Name}:{player.Numbers[0]}:{player.Numbers[1]}:" +
        string.Join(',', player.Skills.Select(skill => (int)skill.Type));

    [Fact]
    public void OnlineSoloCampaign_UsesSameStableBattleRosterAsOffline()
    {
        var node = Node("A1");
        var offline = CampaignBattleFactory.Create(node, []);
        var guestSolo = CampaignBattleFactory.Create(node, [], localPlayerId: 1);

        Assert.Equal(offline.Players.Select(player => player.PlayerId),
            guestSolo.Players.Select(player => player.PlayerId));
        Assert.Equal(offline.Groups[1].Players.Select(player => player.PlayerId),
            guestSolo.Groups[1].Players.Select(player => player.PlayerId));
        Assert.Equal(guestSolo.Players.Count, guestSolo.Players.Select(player => player.PlayerId).Distinct().Count());
    }
}
