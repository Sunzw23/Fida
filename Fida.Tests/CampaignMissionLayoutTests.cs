using Fida.UI;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// The phone mission sheet draws and hit-tests from one set of rectangles, so the card has
/// to stay inside the screen and its rows must never overlap on any phone size.
/// </summary>
public sealed class CampaignMissionLayoutTests
{
    [Fact]
    public void SheetStaysInsideTheViewportAndItsRowsDoNotOverlap()
    {
        foreach (var (width, height) in new[] { (1080, 2400), (720, 1280), (1440, 3200), (1080, 1920) })
        {
            var safe = UIMobileSafeArea.Get(new Viewport(0, 0, width, height));
            var layout = new CampaignMissionLayout(width, height);

            Assert.True(layout.Sheet.Width > 0 && layout.Sheet.Height > 0);
            Assert.True(new Rectangle(0, 0, width, height).Contains(layout.Sheet));
            Assert.True(layout.Sheet.Contains(layout.Action));
            Assert.True(layout.Sheet.Contains(layout.Close));

            Assert.False(layout.Eyebrow.Intersects(layout.Title));
            Assert.False(layout.Title.Intersects(layout.Art));
            Assert.False(layout.Art.Intersects(layout.Story));
            Assert.False(layout.Story.Intersects(layout.Metric));
            Assert.False(layout.Metric.Intersects(layout.RewardLabel));
            Assert.False(layout.RewardLabel.Intersects(layout.Action));
            // The close button owns the header band, so it never covers the title.
            Assert.False(layout.Close.Intersects(layout.Title));
            // The sheet is centred in the safe area rather than pinned to one edge.
            Assert.InRange(layout.Sheet.Top, safe.Top, safe.Bottom - layout.Sheet.Height);
        }
    }

    [Fact]
    public void TheActionTicketKeepsAUsableTouchHeight()
    {
        foreach (var (width, height) in new[] { (1080, 2400), (720, 1280), (1440, 3200) })
        {
            var layout = new CampaignMissionLayout(width, height);
            // 48dp is the smallest comfortable touch target; the ticket is taller than that.
            Assert.True(layout.Action.Height >= 44f * UITypography.PixelsPerDp(width));
            Assert.True(layout.Close.Width >= 44f * UITypography.PixelsPerDp(width));
        }
    }
}
