using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class CombinationLoadoutTests
{
    [Fact]
    public void InstallingACard_CoversItsDigit_AndOnlyOneCardPerDigit()
    {
        CombinationLoadout.SetEquipped([]);

        CombinationLoadout.Toggle("7:1&7:-2");
        Assert.True(CombinationLoadout.IsEquipped("7:1&7:-2"));

        // The second card on the same row evicts the first.
        string? evicted = CombinationLoadout.Toggle("7:7&7:-4");
        Assert.Equal("7:1&7:-2", evicted);
        Assert.False(CombinationLoadout.IsEquipped("7:1&7:-2"));
        Assert.True(CombinationLoadout.IsEquipped("7:7&7:-4"));
        Assert.Single(CombinationLoadout.Equipped);
    }

    [Fact]
    public void TogglingTwice_RemovesTheCard()
    {
        CombinationLoadout.SetEquipped([]);
        CombinationLoadout.Toggle("9:9&9:-7");
        CombinationLoadout.Toggle("9:9&9:-7");
        Assert.Empty(CombinationLoadout.Equipped);
    }

    [Fact]
    public void UnknownIdsAreIgnored()
    {
        CombinationLoadout.SetEquipped(["nope", "9:9&9:-7"]);
        Assert.Single(CombinationLoadout.Equipped);
        Assert.Null(CombinationLoadout.Toggle("nope"));
    }

    [Fact]
    public void Store_RoundTripsTheInstalledSet()
    {
        string path = Path.Combine(Path.GetTempPath(), "fida-combinations-test.json");
        try
        {
            CombinationLoadout.SetEquipped(["7:1&7:-2", "9:1&9:-2"]);
            CombinationStore.Save(path);
            CombinationLoadout.SetEquipped([]);
            var loaded = CombinationStore.Load(path);
            Assert.Equal(2, loaded.Count);
            Assert.Contains("7:1&7:-2", loaded);
        }
        finally
        {
            CombinationLoadout.SetEquipped([]);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void CyclingARowWalksThroughItsVariantsThenBackToThePrintedRule()
    {
        CombinationLoadout.SetEquipped([]);
        var variants = CombinationLoadout.Variants(7);
        Assert.Equal(2, variants.Count);

        Assert.Equal(variants[0].Id, CombinationLoadout.CycleRow(7));
        Assert.Equal(variants[1].Id, CombinationLoadout.CycleRow(7));
        Assert.Null(CombinationLoadout.CycleRow(7));
        Assert.Empty(CombinationLoadout.Equipped);

        // Pair rows have nothing to switch.
        Assert.Null(CombinationLoadout.CycleRow(-1));
    }

    [Fact]
    public void EveryAuthoredCardCoversASingleDigitRow()
    {
        foreach (var card in CombinationTable.Cards)
        {
            Assert.Contains(card.ReplacesDigit, new[] { 0, 7, 8, 9 });
            Assert.NotEmpty(card.Rules);
        }
    }

    [Fact]
    public void AnInstalledCardChangesWhatTheTableResolves()
    {
        CombinationLoadout.SetEquipped([]);
        Assert.Equal(1, CombinationTable.Match([1, 7]).Amount);

        CombinationLoadout.SetEquipped(["7:1&7:-2"]);
        var outcome = CombinationTable.Match([1, 7], CombinationLoadout.Active);
        Assert.Equal(2, outcome.Amount);
        Assert.Equal("7:1&7:-2", outcome.SourceId);

        // The covered row is inert on its own.
        Assert.True(CombinationTable.Match([7, 2], CombinationLoadout.Active).IsNone);
        CombinationLoadout.SetEquipped([]);
    }
}
