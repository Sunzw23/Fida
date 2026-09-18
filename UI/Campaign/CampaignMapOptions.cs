using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign
{
    /// <summary>
    /// Campaign-map presentation settings. Keeping these values outside the control makes
    /// alternate layouts and themes possible without changing navigation or campaign data.
    /// </summary>
    public sealed class CampaignMapOptions
    {
        public float NodeWidth { get; init; } = 270f;
        public float NodeHeight { get; init; } = 64f;
        public float ContentPadding { get; init; } = 70f;
        public float MinimumZoom { get; init; } = 0.55f;
        public float MaximumZoom { get; init; } = 2.2f;
        public int ScreenMargin { get; init; } = 36;
        public int HeaderHeight { get; init; } = 120;
        public int FooterHeight { get; init; } = 94;

        public Rectangle GetMapViewport(Viewport viewport)
        {
            int width = System.Math.Max(1, viewport.Width - ScreenMargin * 2);
            int height = System.Math.Max(1, viewport.Height - HeaderHeight - FooterHeight);
            return new Rectangle(ScreenMargin, HeaderHeight, width, height);
        }
    }
}
