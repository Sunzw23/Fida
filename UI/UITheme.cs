// UITheme.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// 全局赛博朋克配色与背景绘制。配色刻意避开红色，使用青、蓝、冷紫与荧光绿。
    ///
    /// 深蓝底 + 青色信号色：
    /// - 深色背景降低长时间游玩时的视觉疲劳；
    /// - 面板、按钮和卡片使用同一组层级色，保证识别一致；
    /// - 青色边框代表可交互，亮青色代表焦点/悬停/确认。
    ///
    /// 所有颜色集中于此，各控件不再各自硬编码 Black/White，
    /// 换主题只需改这一处。
    /// </summary>
    public static class UITheme
    {
        // ─── Background ─────────────────────────────────
        public static readonly Color BgTop = new Color(3, 7, 20);
        public static readonly Color BgMiddle = new Color(8, 19, 48);
        public static readonly Color BgBottom = new Color(4, 32, 47);
        public static readonly Color Vignette = new Color(1, 3, 12);

        // ─── Surfaces ────────────────────────────────────
        public static readonly Color Panel = new Color(9, 18, 39);
        public static readonly Color PanelLight = new Color(15, 39, 69);
        public static readonly Color PanelDark = new Color(5, 12, 29);
        public static readonly Color PanelHover = new Color(19, 64, 91);
        public static readonly Color PanelPressed = new Color(24, 61, 105);

        // ─── Text ────────────────────────────────────────
        public static readonly Color Ink = new Color(238, 252, 255);
        public static readonly Color InkMuted = new Color(155, 188, 210);

        // ─── Interaction signal ─────────────────────────
        // Names remain Gold/GoldBright for compatibility with existing controls.
        public static readonly Color Gold = new Color(34, 218, 255);
        public static readonly Color GoldBright = new Color(158, 250, 255);
        public static readonly Color AccentBlue = new Color(55, 116, 255);
        public static readonly Color AccentViolet = new Color(122, 96, 255);
        public static readonly Color AccentGreen = new Color(83, 255, 177);

        public static Color RouteAccent(int index) => (index % 4) switch
        {
            1 => AccentBlue,
            2 => AccentViolet,
            3 => AccentGreen,
            _ => Gold
        };

        // ─── 布局 ───────────────────────────────────────
        /// <summary>菜单面板在 1920×1200 下的居中几何（宽屏亦居中）。</summary>
        public static Rectangle PanelRect(Viewport vp)
        {
#if FIDA_ANDROID
            // Portrait screens need the available height for stacked controls.
            // Leave a small, consistent safe margin for rounded display corners.
            int width = System.Math.Max(1, vp.Width - 48);
            int height = System.Math.Max(1, vp.Height - 48);
            return new Rectangle((vp.Width - width) / 2, (vp.Height - height) / 2,
                width, height);
#else
            return new Rectangle((vp.Width - System.Math.Min(1000, vp.Width - 48)) / 2,
                (vp.Height - System.Math.Min(880, vp.Height - 48)) / 2,
                System.Math.Min(1000, vp.Width - 48),
                System.Math.Min(880, vp.Height - 48));
#endif
        }

        /// <summary>背景渐变 + 暗角（菜单与对局通用，铺满整个视口）。</summary>
        public static void DrawBackground(SpriteBatch sb, Texture2D pixel, Viewport vp,
            GameTime gameTime)
        {
            // More bands avoid visible color stepping on high-density displays.
            int bands = 48;
            int bandH = vp.Height / bands;
            for (int i = 0; i < bands; i++)
            {
                float t = (i + 0.5f) / bands;
                Color c = t < 0.52f
                    ? Color.Lerp(BgTop, BgMiddle, t / 0.52f)
                    : Color.Lerp(BgMiddle, BgBottom, (t - 0.52f) / 0.48f);
                // 顶部一条补齐被整数除法吃掉的高度
                int h = (i == bands - 1) ? (vp.Height - i * bandH) : bandH;
                sb.Draw(pixel, new Rectangle(0, i * bandH, vp.Width, h), c);
            }

            DrawCyberGrid(sb, pixel, vp, gameTime);
            DrawVignette(sb, pixel, vp);
        }

        private static void DrawCyberGrid(SpriteBatch sb, Texture2D pixel, Viewport vp,
            GameTime gameTime)
        {
            int spacing = vp.Width < 900 ? 72 : 96;
            int offset = (int)(gameTime.TotalGameTime.TotalSeconds * 10) % spacing;
            Color vertical = AccentBlue * 0.055f;
            Color horizontal = Gold * 0.045f;
            for (int x = -spacing + offset; x < vp.Width; x += spacing)
                sb.Draw(pixel, new Rectangle(x, 0, 1, vp.Height), vertical);
            for (int y = 0; y < vp.Height; y += spacing)
                sb.Draw(pixel, new Rectangle(0, y, vp.Width, 1), horizontal);

            int scanY = (int)(gameTime.TotalGameTime.TotalSeconds * 72) %
                System.Math.Max(1, vp.Height);
            sb.Draw(pixel, new Rectangle(0, scanY - 8, vp.Width, 18), Gold * 0.025f);
            sb.Draw(pixel, new Rectangle(0, scanY, vp.Width, 2), GoldBright * 0.12f);

            int rail = System.Math.Max(4, vp.Width / 180);
            sb.Draw(pixel, new Rectangle(0, 0, rail, vp.Height), AccentViolet * 0.32f);
            sb.Draw(pixel, new Rectangle(vp.Width - rail, 0, rail, vp.Height), AccentGreen * 0.28f);
        }

        private static void DrawVignette(SpriteBatch sb, Texture2D pixel, Viewport vp)
        {
            int w = 3;
            var c = Vignette;
            sb.Draw(pixel, new Rectangle(0, 0, vp.Width, w), c);
            sb.Draw(pixel, new Rectangle(0, vp.Height - w, vp.Width, w), c);
            sb.Draw(pixel, new Rectangle(0, 0, w, vp.Height), c);
            sb.Draw(pixel, new Rectangle(vp.Width - w, 0, w, vp.Height), c);
        }

        /// <summary>Modern menu panel with a restrained double signal frame.</summary>
        public static void DrawMenuPanel(SpriteBatch sb, Texture2D pixel, Viewport vp,
            GameTime gameTime)
        {
            var rect = PanelRect(vp);
            sb.Draw(pixel, rect, Panel);
            float pulse = 0.62f + 0.18f * (float)System.Math.Sin(
                gameTime.TotalGameTime.TotalSeconds * 2.2);
            UIUtils.DrawGlowFrame(sb, rect, pixel, Gold, 2, pulse);
            var inner = new Rectangle(rect.X + 8, rect.Y + 8,
                rect.Width - 16, rect.Height - 16);
            UIUtils.DrawRectangleOutlines(sb, inner, AccentBlue * 0.55f, 1, pixel);
            int signalWidth = System.Math.Max(1, System.Math.Min(320, rect.Width - 80));
            sb.Draw(pixel, new Rectangle(rect.Center.X - signalWidth / 2, rect.Top + 18,
                signalWidth, 3), GoldBright);
            int segment = System.Math.Max(12, signalWidth / 5);
            sb.Draw(pixel, new Rectangle(rect.Center.X - signalWidth / 2, rect.Top + 18,
                segment, 3), AccentViolet);
            sb.Draw(pixel, new Rectangle(rect.Center.X + signalWidth / 2 - segment,
                rect.Top + 18, segment, 3), AccentGreen);

            int sweep = (int)((gameTime.TotalGameTime.TotalSeconds * 90) %
                System.Math.Max(1, rect.Width - 60));
            sb.Draw(pixel, new Rectangle(rect.Left + 30 + sweep, rect.Top + 9, 2, 18),
                GoldBright * 0.65f);
        }
    }
}
