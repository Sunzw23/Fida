using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>Swiss-inspired visual system: warm paper, ink, cobalt, and measured accents.</summary>
    public static class UITheme
    {
        public static readonly Color BgTop = new(242, 241, 235);
        public static readonly Color BgMiddle = new(242, 241, 235);
        public static readonly Color BgBottom = new(231, 234, 230);
        public static readonly Color Vignette = new(24, 31, 42);

        public static readonly Color Panel = new(250, 249, 244);
        public static readonly Color PanelLight = new(255, 255, 251);
        public static readonly Color PanelDark = new(226, 231, 231);
        public static readonly Color PanelHover = new(219, 231, 246);
        public static readonly Color PanelPressed = new(192, 215, 243);

        public static readonly Color Ink = new(21, 32, 47);
        public static readonly Color InkMuted = new(83, 99, 108);

        // Legacy names remain aliases so existing controls share one palette.
        public static readonly Color Gold = new(18, 82, 184);
        public static readonly Color GoldBright = new(8, 66, 160);
        public static readonly Color AccentBlue = new(25, 91, 197);
        public static readonly Color AccentViolet = new(94, 77, 160);
        public static readonly Color AccentGreen = new(17, 129, 102);
        public static readonly Color AccentYellow = new(220, 182, 48);

        public static Color RouteAccent(int index) => (index % 4) switch
        {
            1 => AccentBlue,
            2 => AccentViolet,
            3 => AccentGreen,
            _ => Gold
        };

        public static Rectangle PanelRect(Viewport vp)
        {
#if FIDA_ANDROID
            Rectangle safe = UIMobileSafeArea.Get(vp);
            return new Rectangle(safe.X + 8, safe.Y + 8,
                System.Math.Max(1, safe.Width - 16),
                System.Math.Max(1, safe.Height - 16));
#else
            int width = System.Math.Min(1000, vp.Width - 48);
            int height = System.Math.Min(880, vp.Height - 48);
            return new Rectangle((vp.Width - width) / 2, (vp.Height - height) / 2,
                width, height);
#endif
        }

        public static void DrawBackground(SpriteBatch sb, Texture2D pixel, Viewport vp,
            GameTime gameTime)
        {
            sb.Draw(pixel, new Rectangle(0, 0, vp.Width, vp.Height), BgTop);
            int margin = System.Math.Max(18, vp.Width / 35);
            int unit = System.Math.Max(44, vp.Width / 16);
            Color rule = Ink * 0.075f;
            for (int x = margin; x < vp.Width; x += unit)
                sb.Draw(pixel, new Rectangle(x, 0, 1, vp.Height), rule);
            for (int y = margin; y < vp.Height; y += unit)
                sb.Draw(pixel, new Rectangle(0, y, vp.Width, 1), rule);

            int block = System.Math.Clamp(vp.Width / 12, 34, 160);
            sb.Draw(pixel, new Rectangle(0, 0, block, 8), Gold);
            sb.Draw(pixel, new Rectangle(vp.Width - block, vp.Height - 8, block, 8), AccentGreen);
            sb.Draw(pixel, new Rectangle(margin, vp.Height - margin, 42, 4), AccentYellow);
        }

        public static void DrawMenuPanel(SpriteBatch sb, Texture2D pixel, Viewport vp,
            GameTime gameTime)
        {
            Rectangle rect = PanelRect(vp);
            sb.Draw(pixel, rect, Panel);
            UIUtils.DrawRectangleOutlines(sb, rect, Ink, 2, pixel);
            int strip = System.Math.Clamp(rect.Width / 7, 48, 124);
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Top, strip, 8), Gold);
            sb.Draw(pixel, new Rectangle(rect.Right - strip, rect.Bottom - 8, strip, 8), AccentGreen);
        }
    }
}
