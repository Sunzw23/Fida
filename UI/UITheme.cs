// UITheme.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// 全局配色与现代科技感背景绘制。
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
        public static readonly Color BgTop = new Color(4, 12, 22);
        public static readonly Color BgBottom = new Color(10, 31, 52);
        public static readonly Color Vignette = new Color(1, 5, 11);

        // ─── Surfaces ────────────────────────────────────
        public static readonly Color Panel = new Color(13, 26, 42);
        public static readonly Color PanelLight = new Color(21, 42, 65);
        public static readonly Color PanelDark = new Color(8, 18, 31);
        public static readonly Color PanelHover = new Color(27, 58, 83);
        public static readonly Color PanelPressed = new Color(18, 74, 94);

        // ─── Text ────────────────────────────────────────
        public static readonly Color Ink = new Color(231, 247, 255);
        public static readonly Color InkMuted = new Color(137, 165, 186);

        // ─── Interaction signal ─────────────────────────
        // Names remain Gold/GoldBright for compatibility with existing controls.
        public static readonly Color Gold = new Color(59, 210, 255);
        public static readonly Color GoldBright = new Color(138, 244, 255);

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
        public static void DrawBackground(SpriteBatch sb, Texture2D pixel, Viewport vp)
        {
            // More bands avoid visible color stepping on high-density displays.
            int bands = 48;
            int bandH = vp.Height / bands;
            for (int i = 0; i < bands; i++)
            {
                float t = (i + 0.5f) / bands;
                Color c = Color.Lerp(BgTop, BgBottom, t);
                // 顶部一条补齐被整数除法吃掉的高度
                int h = (i == bands - 1) ? (vp.Height - i * bandH) : bandH;
                sb.Draw(pixel, new Rectangle(0, i * bandH, vp.Width, h), c);
            }

            // 暗角：四缘细描，向中心收拢视线（很轻，不抢戏）
            DrawVignette(sb, pixel, vp);
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
        public static void DrawMenuPanel(SpriteBatch sb, Texture2D pixel, Viewport vp)
        {
            var rect = PanelRect(vp);
            sb.Draw(pixel, rect, Panel);
            UIUtils.DrawRectangleOutlines(sb, rect, Gold, 2, pixel);
            var inner = new Rectangle(rect.X + 8, rect.Y + 8,
                rect.Width - 16, rect.Height - 16);
            UIUtils.DrawRectangleOutlines(sb, inner, Gold * 0.28f, 1, pixel);
            // A short top signal bar gives the panel a modern visual anchor.
            int signalWidth = System.Math.Max(1, System.Math.Min(320, rect.Width - 80));
            sb.Draw(pixel, new Rectangle(rect.Center.X - signalWidth / 2, rect.Top + 18,
                signalWidth, 3), GoldBright);
        }
    }
}
