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

    [Fact]
    public void Fullscreen_UsesDesktopResolutionInsteadOfWindowSize()
    {
        var settings = new DisplaySettingsData
        {
            Fullscreen = true,
            Width = 1920,
            Height = 1200
        };

        var result = DisplaySettingsManager.ResolveBackBuffer(settings, 1920, 1080);

        Assert.Equal((1920, 1080), result);
    }

    [Fact]
    public void WindowedMode_ClampsWindowToUsableDesktopArea()
    {
        var settings = new DisplaySettingsData
        {
            Fullscreen = false,
            Width = 2240,
            Height = 1400
        };

        var result = DisplaySettingsManager.ResolveBackBuffer(settings, 1920, 1080);

        Assert.Equal((1888, 984), result);
    }

    [Fact]
    public void WindowResolutionChoices_FitInside1080pDesktop()
    {
        var resolutions = DisplaySettingsManager.GetWindowResolutions(1920, 1080);

        Assert.All(resolutions, resolution =>
        {
            Assert.True(resolution.Width <= 1888);
            Assert.True(resolution.Height <= 984);
        });
        Assert.Contains((1600, 900), resolutions);
    }
}
