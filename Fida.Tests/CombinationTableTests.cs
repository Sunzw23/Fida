using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class CombinationTableTests
{
    /// <summary>
    /// The hard-coded table as it reads today in <c>ApplyingEffectState</c>: single 7/8 first,
    /// then pairs, then single 9, then 0&amp;0. Used as the oracle for the port.
    /// </summary>
    private static (CombinationKind Kind, int Amount, int First, int Second) Legacy(int x, int y)
    {
        if (x is 7 or 8) return (CombinationKind.Attack, 1, 0, -1);
        if (y is 7 or 8) return (CombinationKind.Attack, 1, 1, -1);
        if ((x == 1 && y == 6) || (x == 6 && y == 1)) return (CombinationKind.Attack, 2, 0, 1);
        if (x == 5 && y == 5) return (CombinationKind.Attack, 3, 0, 1);
        if (x == 6 && y == 6) return (CombinationKind.Attack, 5, 0, 1);
        if (x == 9) return (CombinationKind.Heal, 1, 0, -1);
        if (y == 9) return (CombinationKind.Heal, 1, 1, -1);
        if (x == 0 && y == 0) return (CombinationKind.Heal, 10, 0, 1);
        return (CombinationKind.None, 0, -1, -1);
    }

    [Fact]
    public void Baseline_ReproducesTheHardCodedTableForEveryPair()
    {
        for (int x = 0; x <= 9; x++)
        {
            for (int y = 0; y <= 9; y++)
            {
                var expected = Legacy(x, y);
                var actual = CombinationTable.Match([x, y]);

                Assert.Equal(expected.Kind, actual.Kind);
                Assert.Equal(expected.Amount, actual.Amount);
                Assert.Equal(expected.First, actual.FirstIndex);
                Assert.Equal(expected.Second, actual.SecondIndex);
                if (!actual.IsNone) Assert.Equal(CombinationTable.BaseSource, actual.SourceId);
            }
        }
    }

    [Fact]
    public void PairPriority_DoesNotChangeTheEightBaselineRows()
    {
        // Pairs only ever use 1/5/6/0, so hoisting them above the 7/8/9 singles is a no-op for
        // the shipped table. This pins that down for the rows that could ever collide.
        var collisions = new[] { (1, 6), (6, 1), (5, 5), (6, 6), (0, 0) };
        foreach (var (x, y) in collisions)
        {
            var legacy = Legacy(x, y);
            var actual = CombinationTable.Match([x, y]);
            Assert.Equal(legacy.Kind, actual.Kind);
            Assert.Equal(legacy.Amount, actual.Amount);
        }
    }

    [Fact]
    public void Card_CoversItsDigit_SoALoneDigitGoesInert()
    {
        var card = new CombinationCard("7:1&7:-2", 7, [new CombinationRule(1, 7, CombinationKind.Attack, 2)]);

        Assert.True(CombinationTable.Match([7, 2], [card]).IsNone);

        // A lone 8 is a different row and stays untouched.
        var eight = CombinationTable.Match([8, 2], [card]);
        Assert.Equal(CombinationKind.Attack, eight.Kind);
        Assert.Equal(1, eight.Amount);
    }

    [Fact]
    public void CardPair_WinsOverEverything()
    {
        var card = new CombinationCard("7:1&7:-2", 7, [new CombinationRule(1, 7, CombinationKind.Attack, 2)]);

        var hit = CombinationTable.Match([1, 7], [card]);

        Assert.Equal(CombinationKind.Attack, hit.Kind);
        Assert.Equal(2, hit.Amount);
        Assert.Equal(0, hit.FirstIndex);
        Assert.Equal(1, hit.SecondIndex);
        Assert.Equal("7:1&7:-2", hit.SourceId);
    }

    [Fact]
    public void Card_CanMakeNineStorable_AndTheNinePairIsHuge()
    {
        var card = new CombinationCard("9:9&9:-7", 9, [new CombinationRule(9, 9, CombinationKind.Attack, 7)]);

        // A lone 9 is inert, so it survives the turn — that is what makes 9&9 reachable.
        Assert.True(CombinationTable.Match([9, 2], [card]).IsNone);
        Assert.True(CombinationTable.Match([2, 9], [card]).IsNone);

        var hit = CombinationTable.Match([9, 9], [card]);
        Assert.Equal(CombinationKind.Attack, hit.Kind);
        Assert.Equal(7, hit.Amount);
    }

    [Fact]
    public void Card_DoesNotBreakOtherRows()
    {
        var card = new CombinationCard("9:1&9:-2", 9, [new CombinationRule(1, 9, CombinationKind.Attack, 2)]);

        // 1&6 and 6&6 still work, and 7 still attacks.
        Assert.Equal(2, CombinationTable.Match([1, 6], [card]).Amount);
        Assert.Equal(5, CombinationTable.Match([6, 6], [card]).Amount);
        Assert.Equal(CombinationKind.Attack, CombinationTable.Match([7, 2], [card]).Kind);
    }

    [Fact]
    public void EquippedIds_ResolveThroughTheAuthoredLibrary()
    {
        // No cards are authored yet, so any id resolves to nothing and the table stays baseline.
        Assert.Empty(CombinationTable.Cards);
        Assert.Empty(CombinationTable.Resolve(["7:1&7:-2"]));
        Assert.Equal(2, CombinationTable.Match([1, 6], ["7:1&7:-2"]).Amount);
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(5, 0, 1)]
    [InlineData(0, 0, 0)]
    [InlineData(5, 5, -1)]
    [InlineData(9, 9, -1)]
    public void ShieldIndex_MatchesThePlayerRule(int x, int y, int expected)
    {
        Assert.Equal(expected, CombinationTable.ShieldIndex([x, y]));
    }
}
