// UITheme.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// 全局配色与桌面（tabletop）背景绘制。
    ///
    /// 暖墨 + 羊皮纸（ink-and-paper）主题：
    /// - 桌面背景为暖墨棕的纵向渐变（上深下浅），营造"灯下牌桌"的氛围；
    /// - 菜单控件铺在一块居中的"羊皮纸"面板上，带描金边框；
    /// - 按钮/卡片为羊皮纸色（浅色），文字用深墨色，悬停描金；
    /// - 按下/选中反色为深羊皮纸 + 描金，形成"盖章"质感。
    ///
    /// 所有颜色集中于此，各控件不再各自硬编码 Black/White，
    /// 换主题只需改这一处。
    /// </summary>
    public static class UITheme
    {
        // ─── 背景（桌面） ────────────────────────────────
        /// <summary>背景渐变顶部色：暖墨深棕。</summary>
        public static readonly Color BgTop = new Color(38, 31, 25);
        /// <summary>背景渐变底部色：稍亮的暖棕，模拟灯光由中心泛开。</summary>
        public static readonly Color BgBottom = new Color(66, 55, 44);
        /// <summary>面板四周暗角（晕影）色，向边缘加深以聚焦中央。</summary>
        public static readonly Color Vignette = new Color(18, 14, 11);

        // ─── 纸张 / 卡片 ────────────────────────────────
        /// <summary>羊皮纸面板（菜单底衬）：米色。</summary>
        public static readonly Color Panel = new Color(232, 221, 199);
        /// <summary>浅色羊皮纸：按钮/卡片底色，也作"按压态"文字色。</summary>
        public static readonly Color PanelLight = new Color(243, 236, 221);
        /// <summary>深羊皮纸：卡片底衬、按压/选中态底色，兼作输入框底。</summary>
        public static readonly Color PanelDark = new Color(214, 203, 179);

        // ─── 墨 / 文字 ──────────────────────────────────
        /// <summary>深墨色：浅底上的正文、按压态文字。</summary>
        public static readonly Color Ink = new Color(46, 38, 30);
        /// <summary>占位 / 次要文字（输入框空值提示）。</summary>
        public static readonly Color InkMuted = new Color(112, 96, 74);

        // ─── 描金 ───────────────────────────────────────
        /// <summary>描金：按钮/面板/标题装饰线、焦点边框。</summary>
        public static readonly Color Gold = new Color(186, 152, 84);
        /// <summary>亮金：悬停边框，比 Gold 更亮以示强调。</summary>
        public static readonly Color GoldBright = new Color(214, 183, 113);

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

        /// <summary>菜单"羊皮纸"面板：米色底 + 描金双线框（菜单专属，对局不画）。</summary>
        public static void DrawMenuPanel(SpriteBatch sb, Texture2D pixel, Viewport vp)
        {
            var rect = PanelRect(vp);
            sb.Draw(pixel, rect, Panel);
            UIUtils.DrawEngravedFrame(sb, rect, pixel, Gold, Ink, 3, 8, 24);
        }
    }
}
