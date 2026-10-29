using Fida.UI;
using Microsoft.Xna.Framework;
using Xunit;

namespace Fida.Tests;

public class ColdBattleLayoutTests
{
    [Theory]
    [InlineData(1920, 1200, 1)]
    [InlineData(1920, 1200, 4)]
    [InlineData(1280, 720, 4)]
    [InlineData(1024, 768, 4)]
    [InlineData(960, 600, 3)]
    public void SupportedRostersKeepAllNumbersVisibleSeparatedAndClickable(int width, int height, int count)
    {
        var viewport = new Rectangle(0, 0, width, height);
        var targets = new List<Rectangle>();
        foreach (bool ally in new[] { false, true })
        for (int i = 0; i < count; i++)
        {
            Rectangle unit = UIBattleLayout.Unit(i, count, ally, width, height);
            Assert.True(viewport.Contains(unit));
            for (int n = 0; n < 2; n++)
            {
                Rectangle target = UIBattleLayout.Number(unit, n);
                Assert.True(unit.Contains(target));
                Assert.True(target.Width >= 64 && target.Height >= 64);
                Assert.DoesNotContain(targets, other => other.Intersects(target));
                targets.Add(target);
            }
        }
    }

    [Fact]
    public void UnevenAndOverflowTeamsKeepEveryMemberOnScreen()
    {
        foreach (int count in new[] { 2, 3, 5, 6, 8 })
        {
            var slots = Enumerable.Range(0, count)
                .Select(i => UIBattleLayout.Unit(i, count, true, 1920, 1200)).ToArray();
            foreach (Rectangle slot in slots)
            {
                Assert.True(new Rectangle(0, 680, 1920, 410).Contains(slot));
                Assert.DoesNotContain(slots, other => other != slot && other.Intersects(slot));
            }
        }
    }
}
