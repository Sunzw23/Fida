#nullable enable
using System;
using System.Collections.Generic;
using Fida.Campaign;

namespace Fida.UI.Campaign;

/// <summary>How one campaign mission reads in the phone list.</summary>
public enum CampaignMissionState
{
    /// <summary>Unlocked and not yet cleared: the next mission to play.</summary>
    Open,
    /// <summary>Cleared at least once; the row stays so the battle can be replayed.</summary>
    Cleared,
    /// <summary>Still gated by its parents; the row prints as grey stock but stays readable.</summary>
    Locked
}

/// <summary>A mission list row: the catalogue index plus the stock it prints on.</summary>
public readonly record struct CampaignMissionEntry(int Index, CampaignMissionState State)
{
    public bool Cleared => State == CampaignMissionState.Cleared;
    public bool Locked => State == CampaignMissionState.Locked;
}

/// <summary>
/// The phone mission list. Unlike the old filter, a mission is never dropped from the page:
/// a cleared mission is the only way to play it again, and a locked row tells the player what
/// the chapter still holds. Campaign order is preserved; only the printed state changes.
///
/// Pure data so the replay rule can be tested without a graphics device.
/// </summary>
public static class CampaignMissionList
{
    /// <summary>
    /// A clear outranks the parent gate: a save can hold a co-op clear whose parents are
    /// missing from this lane, and a cleared battle must stay replayable either way.
    /// </summary>
    public static CampaignMissionState StateOf(CampaignProgress progress, CampaignNode node) =>
        progress.IsCompleted(node.Id) ? CampaignMissionState.Cleared
        : !CampaignCatalog.IsUnlocked(progress, node) ? CampaignMissionState.Locked
        : CampaignMissionState.Open;

    public static IReadOnlyList<CampaignMissionEntry> Build(IReadOnlyList<CampaignNode> nodes,
        IReadOnlyList<int> visible, CampaignProgress progress)
    {
        if (nodes.Count == 0 || visible.Count == 0) return Array.Empty<CampaignMissionEntry>();
        var entries = new CampaignMissionEntry[visible.Count];
        for (int row = 0; row < visible.Count; row++)
        {
            int index = visible[row];
            entries[row] = new CampaignMissionEntry(index, index >= 0 && index < nodes.Count
                ? StateOf(progress, nodes[index])
                : CampaignMissionState.Locked);
        }
        return entries;
    }

    /// <summary>
    /// Focus the row the player left on; otherwise the first mission still open, then the
    /// last cleared one, so even a finished chapter opens on something playable.
    /// </summary>
    public static int InitialFocus(IReadOnlyList<CampaignMissionEntry> entries, int selected)
    {
        if (entries.Count == 0) return -1;
        foreach (var entry in entries)
            if (entry.Index == selected) return selected;
        foreach (var entry in entries)
            if (entry.State == CampaignMissionState.Open) return entry.Index;
        for (int row = entries.Count - 1; row >= 0; row--)
            if (entries[row].State == CampaignMissionState.Cleared) return entries[row].Index;
        return entries[0].Index;
    }

    /// <summary>
    /// Ticket copy for the mission sheet. A mission is entered through the companion screen
    /// and/or the loadout (or straight into battle), so the open row says "Begin mission"
    /// rather than naming one of those steps. A cleared mission can be played again.
    /// </summary>
    public static string ActionLabel(CampaignMissionState state) => state switch
    {
        CampaignMissionState.Cleared => "Replay mission",
        CampaignMissionState.Locked => "Locked",
        _ => "Begin mission"
    };

    /// <summary>The stamp printed on a row's artwork, in the chapter cards' count slot.</summary>
    public static string BadgeLabel(CampaignMissionState state, int skillBudget) => state switch
    {
        CampaignMissionState.Cleared => "CLEARED",
        CampaignMissionState.Locked => "LOCKED",
        _ => skillBudget > 0 ? $"{skillBudget} SP" : "NO SP"
    };
}
