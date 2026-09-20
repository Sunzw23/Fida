using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// Platform-neutral bridge for Android system-bar and display-cutout insets.
    /// The fallback keeps UI readable before the first native inset callback arrives.
    /// </summary>
    public static class UIMobileSafeArea
    {
        private static int _left;
        private static int _top;
        private static int _right;
        private static int _bottom;
        private static int _sourceWidth;
        private static int _sourceHeight;

        public static void SetInsets(int left, int top, int right, int bottom,
            int sourceWidth, int sourceHeight)
        {
            _left = System.Math.Max(0, left);
            _top = System.Math.Max(0, top);
            _right = System.Math.Max(0, right);
            _bottom = System.Math.Max(0, bottom);
            _sourceWidth = System.Math.Max(1, sourceWidth);
            _sourceHeight = System.Math.Max(1, sourceHeight);
        }

        public static Rectangle Get(Viewport viewport)
        {
#if FIDA_ANDROID
            float scaleX = _sourceWidth > 0 ? viewport.Width / (float)_sourceWidth : 1f;
            float scaleY = _sourceHeight > 0 ? viewport.Height / (float)_sourceHeight : 1f;
            int left = System.Math.Max(16, (int)(_left * scaleX));
            int right = System.Math.Max(16, (int)(_right * scaleX));
            // A conservative fallback protects centered punch-hole cameras even
            // before Android delivers WindowInsets to the game view.
            int fallbackTop = System.Math.Clamp(viewport.Height / 16, 76, 128);
            int fallbackBottom = System.Math.Clamp(viewport.Height / 32, 34, 72);
            int top = System.Math.Max(fallbackTop, (int)(_top * scaleY));
            int bottom = System.Math.Max(fallbackBottom, (int)(_bottom * scaleY));
            return new Rectangle(left, top,
                System.Math.Max(1, viewport.Width - left - right),
                System.Math.Max(1, viewport.Height - top - bottom));
#else
            return new Rectangle(0, 0, viewport.Width, viewport.Height);
#endif
        }
    }
}
