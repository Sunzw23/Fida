using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class OnlineCampaignTests
{
    [Fact]
    public void CooperativeCampaign_CreatesTwoAlliesAndKeepsEnemiesOnOpposingTeam()
    {
        var node = CampaignCatalog.GetNode("A1");
        var setup = CampaignBattleFactory.CreateCooperative(node,
            [SkillType.PlusAttack], [SkillType.PlusDefence]);

        Assert.Equal(new[] { 0, 1 }, setup.Groups[0].Players.Select(player => player.PlayerId));
        Assert.Equal(node.Enemies.Count, setup.Groups[1].Players.Count);
        Assert.All(setup.Groups[1].Players, enemy => Assert.True(enemy.PlayerId >= 2));
        Assert.Contains(setup.Players[0].Skills, skill => skill.Type == SkillType.PlusAttack);
        Assert.Contains(setup.Players[1].Skills, skill => skill.Type == SkillType.PlusDefence);
    }

    [Fact]
    public void OnlineSoloCampaign_UsesSameStableBattleRosterAsOffline()
    {
        var node = CampaignCatalog.GetNode("A1");
        var offline = CampaignBattleFactory.Create(node, []);
        var guestSolo = CampaignBattleFactory.Create(node, [], localPlayerId: 1);

        Assert.Equal(offline.Players.Select(player => player.PlayerId),
            guestSolo.Players.Select(player => player.PlayerId));
        Assert.Equal(offline.Groups[1].Players.Select(player => player.PlayerId),
            guestSolo.Groups[1].Players.Select(player => player.PlayerId));
    }
}
