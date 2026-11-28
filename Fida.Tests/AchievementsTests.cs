using System.IO;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class AchievementsTests
{
    private static Achievement Row(string id) => Achievements.All.First(a => a.Id == id);

    private static void UseRows(params string[] rows)
    {
        // Loading happens once per process; do it before seeding so the seed wins.
        Achievements.EnsureLoaded();
        CombinationLedger.Restore(rows);
    }

    [Fact]
    public void Ledger_RecordsEachRowOnceAndReportsCompletion()
    {
        UseRows("7", "7", "7");
        Assert.Equal(1, CombinationLedger.Count);
        Assert.False(CombinationLedger.Complete);

        UseRows(CombinationLedger.Rows.ToArray());
        Assert.Equal(8, CombinationLedger.Count);
        Assert.True(CombinationLedger.Complete);
        Assert.Empty(CombinationLedger.Missing);
    }

    [Fact]
    public void Ledger_RecordPairSortsTheDigitsAndIgnoresUnknownRows()
    {
        UseRows();
        CombinationLedger.RecordPair(6, 1);
        CombinationLedger.RecordPair(9, 9);
        CombinationLedger.RecordPair(0, 0);
        CombinationLedger.Record("99");
        CombinationLedger.Record("bogus");

        Assert.True(CombinationLedger.Has("1&6"));
        Assert.True(CombinationLedger.Has("9&9") == false);
        Assert.True(CombinationLedger.Has("0&0"));
        Assert.Equal(2, CombinationLedger.Count);
    }

    [Fact]
    public void FirstBattle_NeedsOneClearedMission()
    {
        var progress = new CampaignProgress();
        UseRows();

        Assert.False(Achievements.Earned(Row("first_battle"), progress));

        progress.CompletedNodes.Add("A1");
        Assert.True(Achievements.Earned(Row("first_battle"), progress));
    }

    [Fact]
    public void VowAchievements_ReadTheVowOffTheClearedNode()
    {
        UseRows();
        var bare = new CampaignProgress();
        bare.CompletedNodes.Add("BS2");   // bare_hands
        Assert.True(Achievements.Earned(Row("bare_hands"), bare));
        Assert.False(Achievements.Earned(Row("alone"), bare));

        var alone = new CampaignProgress();
        alone.CompletedNodes.Add("AS3");   // alone
        Assert.True(Achievements.Earned(Row("alone"), alone));
        Assert.False(Achievements.Earned(Row("bare_hands"), alone));

        var wounded = new CampaignProgress();
        wounded.CompletedNodes.Add("AS5"); // wounded
        Assert.True(Achievements.Earned(Row("wounded"), wounded));
    }

    [Fact]
    public void EveryCombination_IsTheEasterEgg_AndNeedsTheWholeTable()
    {
        var progress = new CampaignProgress();

        UseRows("7", "8", "9", "0", "1&6", "5&5", "6&6");
        Assert.False(Achievements.Earned(Row(Achievements.AllCombinationsId), progress));
        Assert.False(Achievements.EasterEggUnlocked(progress));
        Assert.Equal("7 / 8 rows", Achievements.Detail(Row(Achievements.AllCombinationsId), progress));

        UseRows(CombinationLedger.Rows.ToArray());
        Assert.True(Achievements.Earned(Row(Achievements.AllCombinationsId), progress));
        Assert.True(Achievements.EasterEggUnlocked(progress));
        Assert.Equal("8 / 8 rows", Achievements.Detail(Row(Achievements.AllCombinationsId), progress));
    }

    [Fact]
    public void EarnedCount_CoversTheWholeSheet()
    {
        UseRows(CombinationLedger.Rows.ToArray());
        var progress = new CampaignProgress();
        progress.CompletedNodes.Add("BS2");   // first battle + bare_hands
        progress.CompletedNodes.Add("AS3");   // alone
        progress.CompletedNodes.Add("AS5");   // wounded

        // first_battle, bare_hands, alone, wounded, every_combination (codex_ten depends on the
        // shared skill codex, so it is excluded from the count).
        Assert.Equal(5, Achievements.EarnedCount(progress));
    }

    [Fact]
    public void Chain_NeedsTwoEffectsFromOneChange()
    {
        UseRows();
        Assert.False(Achievements.Earned(Row("chain"), new CampaignProgress()));

        CombinationLedger.RecordChain(1);
        Assert.False(Achievements.Earned(Row("chain"), new CampaignProgress()));

        CombinationLedger.RecordChain(2);
        Assert.True(Achievements.Earned(Row("chain"), new CampaignProgress()));
        Assert.Equal("best 2", Achievements.Detail(Row("chain"), new CampaignProgress()));
    }

    [Fact]
    public void Store_RoundTripsRowsAndChain()
    {
        string path = Path.Combine(Path.GetTempPath(), "fida-achievements-test.json");
        try
        {
            AchievementStore.Save(["7", "9", "0&0"], 3, path);
            var loaded = AchievementStore.Load(path);
            Assert.Equal(new[] { "7", "9", "0&0" }, loaded.Rows.ToArray());
            Assert.Equal(3, loaded.Chain);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Store_MissingFileIsEmptyProgress()
    {
        string path = Path.Combine(Path.GetTempPath(), "fida-achievements-missing-" + System.Guid.NewGuid() + ".json");
        var loaded = AchievementStore.Load(path);
        Assert.Empty(loaded.Rows);
        Assert.Equal(0, loaded.Chain);
    }
}
