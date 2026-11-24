using Fida.Campaign;
using Fida.Logic;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;
using Xunit;

namespace Fida.Tests;

public class CompanionTests
{
    private static CampaignNode Node(string id) => Assert.IsType<CampaignNode>(CampaignCatalog.GetNode(id));

    /// <summary>Second companion fixture: the roster is data-driven, so tests can add one.</summary>
    private static CampaignNode PartyNode(params string[] allowed) =>
        new() { Companions = allowed };

    [Fact]
    public void ShippedRoster_OffersMaraVennWithArtAndDescription()
    {
        var mara = Assert.IsType<CampaignCompanion>(CampaignCompanions.Get("mara_venn"));
        Assert.Equal(4, CampaignCompanions.All.Count);

        Assert.Equal("mara_venn", mara.Id);
        Assert.Equal("Mara Venn", mara.Name);
        Assert.Equal(10, mara.Health);
        Assert.Empty(mara.Skills);
        Assert.Equal(CampaignAgentType.Adaptive, mara.AgentType);
        Assert.Equal("dialogue_mara", mara.ArtKey);
        Assert.Equal(1, mara.AtlasCell);
        Assert.False(string.IsNullOrWhiteSpace(mara.Description));
        Assert.Equal(mara.Id, CampaignCompanions.Get("MARA_VENN")!.Id);
        Assert.Null(CampaignCompanions.Get(""));
        Assert.Null(CampaignCompanions.Get("nobody"));
        Assert.Equal(2, CampaignCompanions.PartyLimit);
    }

    [Fact]
    public void Roster_IsGlobalButANodeCanNarrowOrForbidIt()
    {
        // Absent `companions` means the whole roster may join every campaign battle.
        Assert.Equal(CampaignCompanions.All, CampaignCompanions.Available(Node("A1")));
        Assert.Equal(CampaignCompanions.All, CampaignCompanions.Available(new CampaignNode()));
        // An explicit empty list keeps a battle alone.
        Assert.Empty(CampaignCompanions.Available(Node("P")));
        // A node may also name a subset, and the party limit never exceeds what it offers.
        Assert.Single(CampaignCompanions.Available(PartyNode("mara_venn")));
        Assert.Equal(1, CampaignCompanions.PartyLimitFor(PartyNode("mara_venn")));
        Assert.Equal(0, CampaignCompanions.PartyLimitFor(new CampaignNode { Companions = [] }));
        // The limit is the smaller of the shared party cap and the roster this node offers.
        Assert.Equal(Math.Min(CampaignCompanions.PartyLimit, CampaignCompanions.All.Count),
            CampaignCompanions.PartyLimitFor(Node("A1")));
    }

    [Fact]
    public void Parse_SkipsUnplayableAndDuplicateEntries()
    {
        const string json = """
            [
              { "id": "mara_venn", "name": "Mara Venn", "health": 8 },
              { "id": "mara_venn", "name": "Impostor", "health": 9 },
              { "id": "ghost", "name": "Ghost", "health": 0 },
              { "id": "", "name": "Nameless", "health": 5 },
              { "id": "ally", "name": "Ally", "health": 4, "skills": ["PlusAttack"], "agentType": "Scripted" }
            ]
            """;

        var roster = CampaignCompanions.Parse(json);

        Assert.Equal(["mara_venn", "ally"], roster.Select(companion => companion.Id));
        Assert.Equal(SkillType.PlusAttack, roster[1].Skills.Single());
        Assert.Equal(CampaignAgentType.Scripted, roster[1].AgentType);
        Assert.Equal("tutorial_ally", roster[0].PortraitKey);
        Assert.Equal("", roster[0].ArtKey);
    }

    [Fact]
    public void PartyRules_OrderLimitAvailabilityAndToggle()
    {
        var node = Node("A1");
        Assert.Equal(["mara_venn"], CampaignLoadout.SanitizeCompanions(node, ["  MARA_VENN  "]));
        Assert.Empty(CampaignLoadout.SanitizeCompanions(node, ["nobody"]));
        Assert.Empty(CampaignLoadout.SanitizeCompanions(node, [""]));
        Assert.Empty(CampaignLoadout.SanitizeCompanions(node, null));
        Assert.Equal(["mara_venn"], CampaignLoadout.SanitizeCompanions(node, ["mara_venn", "mara_venn"]));
        // A node that forbids companions can never return anyone.
        Assert.Empty(CampaignLoadout.SanitizeCompanions(Node("P"), ["mara_venn"]));

        var added = CampaignLoadout.ToggleCompanion(node, [], "mara_venn");
        Assert.Equal(["mara_venn"], added);
        var removed = CampaignLoadout.ToggleCompanion(node, added, "mara_venn");
        Assert.Empty(removed);
        Assert.False(CampaignLoadout.CanSelectCompanion(Node("P"), [], "mara_venn"));
        Assert.True(CampaignLoadout.CanSelectCompanion(node, [], "mara_venn"));
        Assert.True(CampaignLoadout.CanSelectCompanion(node, ["mara_venn"], "mara_venn"));
    }

    [Fact]
    public void PartyLimit_AllowsAFullPartyAndRejectsOverflow()
    {
        // The shipped roster has one companion; the rule still caps at the roster size and
        // the shared limit, so a future roster of several can fill a party of PartyLimit.
        var node = Node("A1");
        var single = CampaignLoadout.SanitizeCompanions(node, ["mara_venn", "mara_venn", "other"]);
        Assert.Single(single);
        Assert.False(CampaignLoadout.CanSelectCompanion(node, single, "other"));
        Assert.True(CampaignCompanions.PartyLimitFor(node) <= CampaignCompanions.PartyLimit);
    }

    [Fact]
    public void BattleFactory_SeatsEveryInvitedCompanionThenTheEnemies()
    {
        var node = Node("T01");
        var solo = CampaignBattleFactory.Create(node, []);
        Assert.Single(solo.Groups[0].Players);
        Assert.Equal(1, solo.Groups[1].Players.Min(player => player.PlayerId));

        var invited = CampaignBattleFactory.Create(node, [], companionIds: ["mara_venn"]);
        var mara = invited.Players.Single(player => player.Name == "Mara Venn");
        Assert.Equal(1, mara.PlayerId);
        Assert.Equal(10, mara.Health);
        Assert.Contains(mara, invited.Groups[0].Players);
        Assert.Equal(CampaignAgentType.Adaptive, invited.AgentTypes[mara.PlayerId]);
        Assert.Equal("Echo Scout", invited.Groups[1].Players.Single().Name);
        Assert.Equal(2, invited.Groups[1].Players.Single().PlayerId);
        Assert.Equal(3, invited.Players.Count);
    }

    [Fact]
    public void BattleFactory_CooperativeBattleKeepsThePartnerSlotForTheHuman()
    {
        var setup = CampaignBattleFactory.CreateCooperative(Node("T01"), [], []);
        Assert.DoesNotContain(setup.Players, player => player.Name == "Mara Venn");
        Assert.Equal([0, 1], setup.Groups[0].Players.Select(player => player.PlayerId));
        Assert.All(setup.Groups[1].Players, enemy => Assert.True(enemy.PlayerId >= 2));
    }

    [Theory]
    [InlineData(1920, 1200)]
    [InlineData(1600, 1000)]
    [InlineData(1280, 720)]
    [InlineData(1024, 768)]
    [InlineData(1080, 2400)]
    [InlineData(720, 1280)]
    public void CompanionScreen_KeepsRosterPartyAndActionsApart(int width, int height)
    {
        var layout = new CompanionSelectLayout(width, height,
            new Rectangle(60, 30, 1480, 85), new Rectangle(60, 30, 72, 72));
        var screen = new Rectangle(0, 0, width, height);

        Assert.True(screen.Contains(layout.Roster));
        Assert.True(screen.Contains(layout.Details));
        Assert.True(screen.Contains(layout.Continue));
        Assert.True(screen.Contains(layout.Party));
        Assert.False(layout.Roster.Intersects(layout.Party));
        Assert.False(layout.Roster.Intersects(layout.Details));
        Assert.False(layout.Party.Intersects(layout.Details));
        Assert.False(layout.Continue.Intersects(layout.Roster));
        Assert.False(layout.Continue.Intersects(layout.Details));
        Assert.False(layout.Party.Intersects(layout.Back));

        var cards = Enumerable.Range(0, layout.PageSize).Select(layout.Card).ToArray();
        Assert.Equal(layout.PageSize, cards.Length);
        foreach (var card in cards)
        {
            Assert.True(layout.Roster.Contains(card));
            Assert.True(card.Width >= 64 && card.Height >= 64);
            Assert.DoesNotContain(cards, other => other != card && other.Intersects(card));
            Assert.True(card.Contains(layout.CardName(Array.IndexOf(cards, card))));
        }
        // Art stays inside its card on both orientations.
        for (int i = 0; i < cards.Length; i++)
            Assert.True(cards[i].Contains(layout.CardArt(i)));
        Assert.True(layout.Details.Contains(layout.DetailArt()));
        Assert.True(layout.Details.Contains(layout.DetailText()));
        Assert.False(layout.DetailArt().Intersects(layout.DetailText()));
    }

    [Fact]
    public void PartyStrip_SharesTheLoadoutBandWithoutTouchingTheGrid()
    {
        var layout = new SkillSelectionLayout(1600, 1000,
            new Rectangle(60, 30, 1480, 85), new Rectangle(60, 30, 72, 72));
        Assert.True(layout.Party.Width > 0 && layout.Party.Height > 0);
        Assert.False(layout.Party.Intersects(layout.Grid));
        Assert.False(layout.Party.Intersects(layout.Details));
        Assert.False(layout.Party.Intersects(layout.Footer));
        Assert.False(layout.Party.Intersects(layout.Start));
    }
}
