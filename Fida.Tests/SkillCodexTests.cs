using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class SkillCodexTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "fida-codex-" + Guid.NewGuid().ToString("N"));

    public SkillCodexTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        SkillCodex.UseStore(null);
        try { Directory.Delete(_directory, true); } catch { }
    }

    [Fact]
    public void Record_IsIdempotentAndRemembersTheFirstSource()
    {
        SkillCodex.UseStore(null);
        SkillCodex.RecordSkills([SkillType.DoubleAttack, SkillType.Xue], "A1");
        SkillCodex.RecordSkills([SkillType.DoubleAttack], "B2");

        Assert.Contains(SkillType.DoubleAttack, SkillCodex.Encountered);
        Assert.Contains(SkillType.Xue, SkillCodex.Encountered);
        Assert.Equal("A1", SkillCodex.FirstSeen(SkillType.DoubleAttack));
    }

    [Fact]
    public void Record_IgnoresSkillsWithoutAnImplementation()
    {
        SkillCodex.UseStore(null);
        SkillCodex.RecordSkills([SkillType.Gui, SkillType.Huo, SkillType.Long], "duel");

        Assert.DoesNotContain(SkillType.Gui, SkillCodex.Encountered);
        Assert.DoesNotContain(SkillType.Huo, SkillCodex.Encountered);
        Assert.Contains(SkillType.Long, SkillCodex.Encountered);
    }

    [Fact]
    public void Store_RoundTripsNamesInsteadOfEnumValues()
    {
        var store = new SkillCodexStore(Path.Combine(_directory, "codex.json"));
        SkillCodex.UseStore(store);
        SkillCodex.RecordSkills([SkillType.Shen, SkillType.Shi], "A3");
        SkillCodex.MarkRead(SkillType.Shen);

        string json = File.ReadAllText(store.Path);
        Assert.Contains("\"God\"", json);
        Assert.Contains("\"Stone\"", json);

        SkillCodex.UseStore(store);
        Assert.Contains(SkillType.Shen, SkillCodex.Encountered);
        Assert.False(SkillCodex.IsNew(SkillType.Shen));
        Assert.True(SkillCodex.IsNew(SkillType.Shi));
        Assert.Equal("A3", SkillCodex.FirstSeen(SkillType.Shi));
    }

    [Fact]
    public void Load_ToleratesBrokenFilesAndUnknownNames()
    {
        string path = Path.Combine(_directory, "broken.json");
        File.WriteAllText(path, "{ not json");
        SkillCodex.UseStore(new SkillCodexStore(path));
        Assert.Empty(SkillCodex.Encountered);

        File.WriteAllText(path, "{\"Encountered\":[\"NotASkill\",\"Blood\"]}");
        SkillCodex.UseStore(new SkillCodexStore(path));
        Assert.Single(SkillCodex.Encountered);
        Assert.Contains(SkillType.Xue, SkillCodex.Encountered);
    }

    [Fact]
    public void DeriveFromProgress_IncludesRewardsEnemiesAndReinforcements()
    {
        var node = CampaignCatalog.Nodes.First(n => n.Reinforcements.Any(r => r.Skills.Count > 0));
        var progress = new CampaignProgress();
        progress.CompletedNodes.Add(node.Id);

        var derived = SkillCodex.DeriveFromProgress(progress).ToHashSet();

        foreach (var reward in node.Rewards) Assert.Contains(reward, derived);
        foreach (var enemy in node.Enemies) foreach (var skill in enemy.Skills) Assert.Contains(skill, derived);
        foreach (var enemy in node.Reinforcements) foreach (var skill in enemy.Skills) Assert.Contains(skill, derived);
    }

    [Fact]
    public void DeriveFromProgress_IgnoresUnfinishedNodes()
    {
        var node = CampaignCatalog.Nodes.First(n => n.Rewards.Count > 0);
        var progress = new CampaignProgress();

        Assert.DoesNotContain(node.Rewards[0], SkillCodex.DeriveFromProgress(progress));
    }
}
