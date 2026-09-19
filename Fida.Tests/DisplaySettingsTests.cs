using Xunit;

namespace Fida.Tests;

public class DisplaySettingsTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(800, 600)]
    [InlineData(9000, 5000)]
    public void Normalize_ReplacesUnsafeWindowSizes(int width, int height)
    {
        var settings = new DisplaySettingsData { Width = width, Height = height };

        DisplaySettingsManager.Normalize(settings);

        Assert.Equal(1920, settings.Width);
        Assert.Equal(1200, settings.Height);
    }

    [Fact]
    public void Normalize_PreservesSupportedDesktopSize()
    {
        var settings = new DisplaySettingsData { Width = 1920, Height = 1200 };

        DisplaySettingsManager.Normalize(settings);

        Assert.Equal(1920, settings.Width);
        Assert.Equal(1200, settings.Height);
    }
}
