using Fida.UI;
using Xunit;

namespace Fida.Tests;

public class UITypographyTests
{
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1024, 768)]
    [InlineData(960, 600)]
    public void SmallWindowsRetainReadablePhysicalLineBoxes(int width, int height)
    {
        Assert.True(UITypography.LineHeight(UITextRole.Caption, width, height) >= 22);
        Assert.True(UITypography.LineHeight(UITextRole.Name, width, height) >= 24);
        Assert.True(UITypography.LineHeight(UITextRole.Body, width, height) >= 28);
        Assert.True(UITypography.LineHeight(UITextRole.Name, width, height) >
            UITypography.LineHeight(UITextRole.Small, width, height));
    }

    [Fact]
    public void ParagraphWrappingPreservesWordsBlankLinesAndLongIdentifiers()
    {
        var lines = UITextRenderer.WrapLines("Alpha beta\n\nABCDEFGHIJK gamma", 5, text => text.Length);
        Assert.Equal(new[] { "Alpha", "beta", "", "ABCDE", "FGHIJ", "K", "gamma" }, lines);
        Assert.All(lines, line => Assert.True(line.Length <= 5));
    }
}
