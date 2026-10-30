using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>Cold, quiet surfaces; bone-white information and a single ice accent.</summary>
    public static class UITheme
    {
        public static readonly Color BgTop = new(11, 19, 28);
        public static readonly Color BgMiddle = BgTop;
        public static readonly Color BgBottom = BgTop;
        public static readonly Color Vignette = new(3, 6, 10);

        public static readonly Color Panel = new(24, 39, 54);
        public static readonly Color PanelLight = new(36, 55, 73);
        public static readonly Color PanelDark = new(15, 27, 39);
        public static readonly Color PanelHover = new(48, 76, 95);
        public static readonly Color PanelPressed = new(52, 83, 101);
        public static readonly Color Line = new(85, 111, 129);

        public static readonly Color Ink = new(237, 232, 219);
        public static readonly Color InkMuted = new(174, 188, 198);

        // Legacy names remain aliases so existing controls share one palette.
        public static readonly Color Gold = new(94, 217, 222);
        public static readonly Color GoldBright = new(112, 237, 240);
        public static readonly Color AccentBlue = Gold;
        public static readonly Color AccentViolet = new(172, 162, 198);
        public static readonly Color AccentGreen = Gold;
        public static readonly Color AccentYellow = new(226, 190, 127);

        public static Color RouteAccent(int index) => index % 3 == 0 ? Gold : InkMuted;

        public static Rectangle PanelRect(Viewport vp)
        {
#if FIDA_ANDROID
            Rectangle safe = UIMobileSafeArea.Get(vp);
            return new Rectangle(safe.X + 8, safe.Y + 8,
                System.Math.Max(1, safe.Width - 16),
                System.Math.Max(1, safe.Height - 16));
#else
            float scale = UIDesktopLayout.Scale(vp);
            int margin = System.Math.Max(8, (int)System.Math.Round(24 * scale));
            int width = System.Math.Min(System.Math.Max(1, (int)System.Math.Round(1000 * scale)),
                System.Math.Max(1, vp.Width - margin * 2));
            int height = System.Math.Min(System.Math.Max(1, (int)System.Math.Round(880 * scale)),
                System.Math.Max(1, vp.Height - margin * 2));
            return new Rectangle((vp.Width - width) / 2, (vp.Height - height) / 2,
                width, height);
#endif
        }

        public static void DrawBackground(SpriteBatch sb, Texture2D pixel, Viewport vp,
            GameTime gameTime)
        {
            UIPaper.Background(sb, vp);
        }

        public static void DrawMenuPanel(SpriteBatch sb, Texture2D pixel, Viewport vp,
            GameTime gameTime)
        {
            Rectangle rect = PanelRect(vp);
            Rectangle backing = rect; backing.Offset(-6, 7);
            UIPaper.Sheet(sb, backing, PanelLight, 3, 12);
            UIPaper.Sheet(sb, rect, Panel, 4, 10);
        }
    }
}
