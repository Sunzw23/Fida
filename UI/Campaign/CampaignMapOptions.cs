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
        public float NodeHeight { get; init; } = 120f;
        public float ContentPadding { get; init; } = 70f;
        public float MinimumZoom { get; init; } = 0.55f;
        public float MaximumZoom { get; init; } = 2.2f;
        public int ScreenMargin { get; init; } = 36;
        public int HeaderHeight { get; init; } = 24;
        public int FooterHeight { get; init; } = 94;

        public static CampaignMapOptions Mobile() => new()
        {
            NodeWidth = 420f,
            NodeHeight = 104f,
            ContentPadding = 110f,
            MinimumZoom = 0.42f,
            MaximumZoom = 2.4f,
            ScreenMargin = 24,
            HeaderHeight = 142,
            FooterHeight = 196
        };

        public Rectangle GetMapViewport(Viewport viewport)
        {
#if !FIDA_ANDROID
            return new CampaignMapLayout(viewport).Map;
#else
            int width = System.Math.Max(1, viewport.Width - ScreenMargin * 2);
            int height = System.Math.Max(1, viewport.Height - HeaderHeight - FooterHeight);
            return new Rectangle(ScreenMargin, HeaderHeight, width, height);
#endif
        }
    }
}
