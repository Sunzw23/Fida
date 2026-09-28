using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Fida.UI
{
    /// <summary>
    /// 布局重叠调试器：读取所有控件经 <see cref="UIControl.GetScreenBounds"/> 报告的
    /// 真实屏幕几何，标出真正互相侵入的控件对。
    /// 检测与渲染共用同一份几何来源，因此检测器看到的永远是屏幕上实际画出的范围。
    /// F8 开关（仅桌面构建）；在任意屏幕（菜单、大厅、战斗）上按 F8 即可查看全部控件轮廓与冲突。
    /// </summary>
    public static class UIOverlapDebugger
    {
        /// <summary>相交面积占较小矩形面积低于此比例时视为"贴边"，不算重叠。</summary>
        public const float ContainmentThreshold = 0.02f;

        /// <summary>调试器当前是否开启。</summary>
        public static bool IsEnabled { get; private set; }

        public static void Toggle() => IsEnabled = !IsEnabled;

        public struct Collision
        {
            public string A;
            public string B;
            public Rectangle Intersect;
        }

        /// <summary>
        /// 两矩形是否构成包含关系（内矩形贴边位于外矩形内部也算包含）。
        /// 完全相同的矩形不算包含——那表示完全重叠，应被冲突检测报出。
        /// </summary>
        public static bool IsContained(Rectangle outer, Rectangle inner)
        {
            if (outer == inner) return false;
            return outer.Left <= inner.Left && outer.Top <= inner.Top &&
                   outer.Right >= inner.Right && outer.Bottom >= inner.Bottom;
        }

        /// <summary>
        /// 两矩形是否构成布局冲突：相交、互不包含，
        /// 且相交面积占较小矩形面积的比例超过阈值（仅共享一条边的"贴边"不算）。
        /// </summary>
        public static bool IsOverlapping(Rectangle a, Rectangle b)
        {
            if (IsContained(a, b) || IsContained(b, a)) return false;
            var intersect = Rectangle.Intersect(a, b);
            if (intersect.Width <= 0 || intersect.Height <= 0) return false;
            float area = intersect.Width * (float)intersect.Height;
            float smaller = System.Math.Min(
                a.Width * (float)a.Height, b.Width * (float)b.Height);
            return smaller > 0 && area / smaller > ContainmentThreshold;
        }

        /// <summary>
        /// 纯几何冲突检测（不依赖任何 XNA 绘制状态，可直接单测）：
        /// 返回所有互相侵入的控件对及其相交矩形。
        /// </summary>
        public static List<Collision> FindCollisions(IReadOnlyList<(string Name, Rectangle Bounds)> items)
        {
            var result = new List<Collision>();
            for (int i = 0; i < items.Count; i++)
            {
                var (nameA, boundsA) = items[i];
                for (int j = i + 1; j < items.Count; j++)
                {
                    var (nameB, boundsB) = items[j];
                    if (!IsOverlapping(boundsA, boundsB)) continue;
                    result.Add(new Collision
                    {
                        A = nameA,
                        B = nameB,
                        Intersect = Rectangle.Intersect(boundsA, boundsB)
                    });
                }
            }
            return result;
        }

        private static readonly Color[] Palette =
        {
            Color.Cyan, Color.Magenta, Color.Yellow, Color.Lime,
            Color.Orange, Color.White, Color.HotPink, Color.Khaki
        };

        private static string DisplayName(UIControl control) =>
            string.IsNullOrEmpty(control.Name)
                ? control.GetType().Name + "#" + control.GetId()
                : control.Name;

        /// <summary>
        /// 在当前帧绘制调试叠加层：每个可见控件一圈彩色轮廓，
        /// 每个冲突对的相交区域涂红，左上角图例列出冲突控件名。
        /// </summary>
        public static void Draw(SpriteBatch sb, IReadOnlyList<UIControl> controls,
            Viewport viewport, Texture2D pixel, SpriteFont font)
        {
            if (!IsEnabled || pixel == null || font == null || controls.Count == 0) return;

            var items = new List<(string, Rectangle)>(controls.Count);
            foreach (var control in controls)
            {
                var bounds = control.GetScreenBounds(viewport);
                if (bounds == null) continue;
                items.Add((DisplayName(control), bounds.Value));
            }

            // 每个控件一圈彩色轮廓（颜色按顺序轮换，便于追踪相邻控件）
            int colorIndex = 0;
            foreach (var (_, bounds) in items)
            {
                UIUtils.DrawRectangleOutlines(sb, bounds, Palette[colorIndex % Palette.Length], 1, pixel);
                colorIndex++;
            }

            var collisions = FindCollisions(items);

            // 冲突相交区域涂红
            foreach (var collision in collisions)
            {
                sb.Draw(pixel, collision.Intersect, Color.Red);
            }

            // 左上角图例
            float scale = System.Math.Clamp((float)viewport.Width / 1920f, 0.6f, 1f);
            int lineH = (int)(16 * scale) + 2;
            var lines = new List<string>
            {
                "OVERLAP DEBUG (F8 off)  controls: " + items.Count + "  overlaps: " + collisions.Count
            };
            foreach (var collision in collisions)
            {
                if (lines.Count > 8) { lines.Add("..."); break; }
                lines.Add(collision.A + "  <->  " + collision.B);
            }
            int legendW = (int)font.MeasureString(lines[0]).X + 16;
            int legendH = lineH * lines.Count + 6;
            sb.Draw(pixel, new Rectangle(8, 8, legendW, legendH), new Color(0, 0, 0, 170));
            var pos = new Vector2(16, 12);
            for (int i = 0; i < lines.Count; i++)
            {
                var color = i == 0 ? Color.White : (i <= collisions.Count ? Color.Red : Color.Gray);
                sb.DrawString(font, UIFontGuard.Sanitize(font, lines[i]), pos, color * scale);
                pos.Y += lineH;
            }
        }
    }
}
