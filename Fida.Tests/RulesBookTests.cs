using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;
using Fida.UI.Rules;
using Xunit;

namespace Fida.Tests;

public class RulesBookTests
{
    [Fact]
    public void OwnedSkillsComeFirstAndOnlyAppearOnce()
    {
        var book = RulesBook.From(
            [SkillType.Xue, SkillType.PlusAttack],
            [SkillType.Xue, SkillType.Long, SkillType.Shi]);

        Assert.Equal([SkillType.PlusAttack, SkillType.Xue, SkillType.Long, SkillType.Shi], book.Entries);
        Assert.Equal(book.Entries.Count, book.Entries.Distinct().Count());
    }

    [Fact]
    public void EntriesAreOrderedByFamilyThenNameInsideEachGroup()
    {
        var book = RulesBook.From(
            [SkillType.Shen, SkillType.PlusDefence, SkillType.DoubleAttack],
            [SkillType.Mo, SkillType.TripleHealth, SkillType.Bian]);

        Assert.Equal(
            [SkillType.DoubleAttack, SkillType.PlusDefence, SkillType.Shen, SkillType.Bian, SkillType.TripleHealth, SkillType.Mo],
            book.Entries);
    }

    [Fact]
    public void PageCountIsFourPlusTheDiscoveredSkills()
    {
        var empty = RulesBook.From([], []);
        Assert.Equal(4, empty.TotalPages);
        Assert.Equal(RulesBook.IndexPage, RulesBook.ReferencePages);
        Assert.False(empty.IsSkillPage(RulesBook.IndexPage));

        var book = RulesBook.From([SkillType.Xue], [SkillType.Long]);
        Assert.Equal(6, book.TotalPages);
        Assert.Equal(20, book.TotalSkills);
    }

    [Fact]
    public void SkillPagesMapBackToTheirIndex()
    {
        var book = RulesBook.From([SkillType.Xue, SkillType.Long], []);

        for (int i = 0; i < book.Entries.Count; i++)
        {
            int page = RulesBook.PageForIndex(i);
            Assert.True(book.IsSkillPage(page));
            Assert.Equal(book.Entries[i], book.SkillAtPage(page));
            Assert.Equal(i, book.IndexOf(book.Entries[i]));
        }
        Assert.False(book.IsSkillPage(book.TotalPages));
        Assert.Null(book.SkillAtPage(book.TotalPages));
        Assert.Null(book.SkillAtPage(-1));
    }

    [Fact]
    public void EverySelectableSkillHasASinglePageOfCopy()
    {
        Assert.Empty(SkillCodexText.MissingCopy());
        foreach (var skill in SkillFactory.SelectableSkills)
        {
            var entry = SkillCodexText.For(skill);
            Assert.False(string.IsNullOrWhiteSpace(entry.Effect));
            Assert.False(string.IsNullOrWhiteSpace(entry.Example));
            Assert.True(entry.Effect.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 18,
                $"{skill} effect sentence is longer than 18 words.");
            Assert.Contains("→", entry.Example);
        }
    }

    [Fact]
    public void FootnoteCarriesStageLimitAndSource()
    {
        foreach (var skill in SkillFactory.SelectableSkills)
        {
            string footnote = SkillCodexText.Footnote(skill, owned: false);
            Assert.Equal(3, footnote.Split('·').Length);
            Assert.Contains(SkillCodexText.Stage(SkillFactory.CreateSkill(skill).Trigger), footnote);
        }
    }

    [Fact]
    public void StageNamesCoverEveryImplementedTrigger()
    {
        foreach (var skill in SkillFactory.SelectableSkills)
        {
            var trigger = SkillFactory.CreateSkill(skill).Trigger;
            Assert.NotEqual("In battle", SkillCodexText.Stage(trigger));
        }
    }
}
