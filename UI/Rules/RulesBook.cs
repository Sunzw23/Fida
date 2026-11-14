#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;

namespace Fida.UI.Rules;

/// <summary>
/// Page model for the rules screen: three reference pages, one skill index, then one page per
/// discovered skill. Discovery is owned (campaign unlocks) plus encountered (codex and the
/// skills implied by finished campaign nodes).
/// </summary>
public sealed class RulesBook
{
    public const int ReferencePages = 3;
    public const int TimingPage = 2;
    public const int IndexPage = ReferencePages;

    public static readonly string[] Chapters = { "Your turn", "Numbers", "Timing", "Skills" };

    private readonly HashSet<SkillType> _owned;

    /// <summary>Owned skills first, then only-seen ones; each group ordered by family then name.</summary>
    public IReadOnlyList<SkillType> Entries { get; }

    public int TotalPages => ReferencePages + 1 + Entries.Count;
    public int TotalSkills => SkillFactory.SelectableSkills.Count;

    private RulesBook(IEnumerable<SkillType> owned, IEnumerable<SkillType> encountered)
    {
        _owned = new HashSet<SkillType>(owned.Where(SkillCatalog.CanCreate));
        var seen = new HashSet<SkillType>(encountered.Where(SkillCatalog.CanCreate));
        seen.UnionWith(_owned);

        Entries = _owned.OrderBy(OrderKey)
            .Concat(seen.Except(_owned).OrderBy(OrderKey))
            .ToArray();
    }

    public bool Owns(SkillType skill) => _owned.Contains(skill);

    /// <summary>Explicit sets, for tests and preview tooling; the screen uses <see cref="Current"/>.</summary>
    public static RulesBook From(IEnumerable<SkillType> owned, IEnumerable<SkillType> encountered) =>
        new(owned, encountered);

    public int IndexOf(SkillType skill)
    {
        for (int i = 0; i < Entries.Count; i++)
            if (Entries[i] == skill) return i;
        return -1;
    }

    public SkillType? SkillAtPage(int page)
    {
        int index = page - (IndexPage + 1);
        return index >= 0 && index < Entries.Count ? Entries[index] : null;
    }

    public bool IsSkillPage(int page) => SkillAtPage(page) != null;
    public static int PageForIndex(int index) => IndexPage + 1 + index;

    /// <summary>Snapshot used by every screen build; never writes a save.</summary>
    public static RulesBook Current()
    {
        var owned = new List<SkillType>(CampaignRuntime.Progress.UnlockedSkills);
        owned.AddRange(CampaignRuntime.OnlineProgress.UnlockedSkills);
        var encountered = new List<SkillType>(SkillCodex.Encountered);
        encountered.AddRange(SkillCodex.DeriveFromProgress(
            CampaignRuntime.Progress, CampaignRuntime.OnlineProgress));
        return new RulesBook(owned, encountered);
    }

    private static (int family, string name) OrderKey(SkillType skill) =>
        ((int)SkillCodexText.FamilyOf(skill), SkillCatalog.EnglishName(skill));
}
