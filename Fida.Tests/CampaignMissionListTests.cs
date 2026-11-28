using Fida.Campaign;
using Fida.UI.Campaign;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// The phone mission list used to hide every mission it had already cleared, which also
/// hid the only way back into those battles. These checks pin the rule that a row is
/// never dropped: the printed state changes, the row stays tappable.
/// </summary>
public sealed class CampaignMissionListTests
{
    private static CampaignNode Node(string id) =>
        Assert.IsType<CampaignNode>(CampaignCatalog.GetNode(id));

    private static int IndexOf(string id) =>
        CampaignCatalog.Nodes.ToList().FindIndex(node => node.Id == id);

    [Fact]
    public void ClearedMissionKeepsItsRowAndOffersAReplay()
    {
        var progress = new CampaignProgress();
        progress.Complete(Node("P"));
        int index = IndexOf("P");

        var entries = CampaignMissionList.Build(CampaignCatalog.Nodes, new[] { index }, progress);

        var entry = Assert.Single(entries);
        Assert.Equal(index, entry.Index);
        Assert.Equal(CampaignMissionState.Cleared, entry.State);
        Assert.True(entry.Cleared);
        Assert.Equal("Replay mission", CampaignMissionList.ActionLabel(entry.State));
    }

    [Fact]
    public void LockedMissionKeepsItsRowButCannotStart()
    {
        var progress = new CampaignProgress();
        int index = IndexOf("A2");

        var entries = CampaignMissionList.Build(CampaignCatalog.Nodes, new[] { index }, progress);

        var entry = Assert.Single(entries);
        Assert.Equal(CampaignMissionState.Locked, entry.State);
        Assert.Equal("Locked", CampaignMissionList.ActionLabel(entry.State));
    }

    [Fact]
    public void AClearOutranksAMissingParentGate()
    {
        var progress = new CampaignProgress();
        var node = Node("A2");
        // Co-op clears merge from the partner, so a lane can hold a clear without its parents.
        progress.Complete(node);

        Assert.Equal(CampaignMissionState.Cleared, CampaignMissionList.StateOf(progress, node));
    }

    [Fact]
    public void FocusKeepsTheCurrentRowThenPrefersTheNextOpenMission()
    {
        var nodes = new[] { Node("P"), Node("T01"), Node("T02"), Node("T03") };
        var progress = new CampaignProgress();
        progress.Complete(Node("P"));
        var entries = CampaignMissionList.Build(nodes, new[] { 0, 1, 2, 3 }, progress);

        Assert.Equal(1, CampaignMissionList.InitialFocus(entries, -1));
        Assert.Equal(0, CampaignMissionList.InitialFocus(entries, 0));

        foreach (var node in nodes) progress.Complete(node);
        entries = CampaignMissionList.Build(nodes, new[] { 0, 1, 2, 3 }, progress);
        Assert.Equal(3, CampaignMissionList.InitialFocus(entries, -1));
    }

    [Fact]
    public void EveryRowPrintsTheStateAndCostItWasGiven()
    {
        Assert.Equal("CLEARED", CampaignMissionList.BadgeLabel(CampaignMissionState.Cleared, 3));
        Assert.Equal("LOCKED", CampaignMissionList.BadgeLabel(CampaignMissionState.Locked, 3));
        Assert.Equal("3 SP", CampaignMissionList.BadgeLabel(CampaignMissionState.Open, 3));
        Assert.Equal("NO SP", CampaignMissionList.BadgeLabel(CampaignMissionState.Open, 0));
    }

    /// <summary>
    /// The ticket opens the whole preparation flow (companions, loadout, or straight into
    /// battle), so it is not named after the loadout step.
    /// </summary>
    [Fact]
    public void TheOpenTicketNamesTheMissionRatherThanTheLoadout() =>
        Assert.Equal("Begin mission", CampaignMissionList.ActionLabel(CampaignMissionState.Open));
}
