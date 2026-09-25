using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>Maps the desktop UI's 1920x1200 design canvas into any viewport without distortion.</summary>
    public static class UIDesktopLayout
    {
        public const int DesignWidth = 1920;
        public const int DesignHeight = 1200;

        public static float Scale(Viewport viewport) => Scale(viewport.Width, viewport.Height);

        public static float Scale(int width, int height) => System.Math.Max(0.01f,
            System.Math.Min(width / (float)DesignWidth, height / (float)DesignHeight));

        public static Vector2 MapPoint(Vector2 point, Viewport viewport)
            => MapPoint(point, viewport.Width, viewport.Height);

        public static Vector2 MapPoint(Vector2 point, int width, int height)
        {
            float scale = Scale(width, height);
            float offsetX = (width - DesignWidth * scale) / 2f;
            float offsetY = (height - DesignHeight * scale) / 2f;
            return new Vector2(offsetX + point.X * scale, offsetY + point.Y * scale);
        }

        public static Rectangle MapRectangle(Rectangle rectangle, Viewport viewport)
            => MapRectangle(rectangle, viewport.Width, viewport.Height);

        public static Rectangle MapRectangle(Rectangle rectangle, int width, int height)
        {
            Vector2 topLeft = MapPoint(new Vector2(rectangle.Left, rectangle.Top), width, height);
            float scale = Scale(width, height);
            return new Rectangle((int)System.Math.Round(topLeft.X),
                (int)System.Math.Round(topLeft.Y),
                System.Math.Max(1, (int)System.Math.Round(rectangle.Width * scale)),
                System.Math.Max(1, (int)System.Math.Round(rectangle.Height * scale)));
        }

        public static int ScaleMetric(int value) =>
            System.Math.Max(value == 0 ? 0 : 1,
                (int)System.Math.Round(value * Scale(DisplaySettingsManager.LayoutWidth,
                    DisplaySettingsManager.LayoutHeight)));
    }
}
