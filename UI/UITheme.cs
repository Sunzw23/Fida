using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>Cold, quiet surfaces; bone-white information and a single ice accent.</summary>
    public static class UITheme
    {
        public static readonly Color BgTop = new(12, 16, 21);
        public static readonly Color BgMiddle = BgTop;
        public static readonly Color BgBottom = BgTop;
        public static readonly Color Vignette = new(3, 6, 10);

        public static readonly Color Panel = new(17, 22, 28);
        public static readonly Color PanelLight = new(23, 29, 36);
        public static readonly Color PanelDark = new(9, 13, 18);
        public static readonly Color PanelHover = new(34, 44, 53);
        public static readonly Color PanelPressed = new(40, 57, 65);
        public static readonly Color Line = new(75, 86, 95);

        public static readonly Color Ink = new(237, 236, 229);
        public static readonly Color InkMuted = new(149, 160, 168);

        // Legacy names remain aliases so existing controls share one palette.
        public static readonly Color Gold = new(94, 217, 222);
        public static readonly Color GoldBright = new(112, 237, 240);
        public static readonly Color AccentBlue = Gold;
        public static readonly Color AccentViolet = new(183, 195, 205);
        public static readonly Color AccentGreen = Gold;
        public static readonly Color AccentYellow = Ink;

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
            sb.Draw(pixel, new Rectangle(0, 0, vp.Width, vp.Height), BgTop);
        }

        public static void DrawMenuPanel(SpriteBatch sb, Texture2D pixel, Viewport vp,
            GameTime gameTime)
        {
            Rectangle rect = PanelRect(vp);
            sb.Draw(pixel, rect, Panel);
            int inset = System.Math.Max(16, rect.Width / 25);
            sb.Draw(pixel, new Rectangle(rect.Left + inset, rect.Top,
                rect.Width - inset * 2, 1), Line);
            sb.Draw(pixel, new Rectangle(rect.Left + inset, rect.Bottom - 1,
                rect.Width - inset * 2, 1), Line);
        }
    }
}
