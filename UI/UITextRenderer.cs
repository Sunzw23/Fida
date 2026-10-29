using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>统一文字缩放与像素对齐，避免在已缩放的控件内二次缩小文字。</summary>
public static class UITextRenderer
{
    public static float FitScale(Vector2 measured, float width, float height, float preferredScale = 1f)
    {
        if (width <= 0 || height <= 0 || preferredScale <= 0) return 0;
        return Math.Max(0, Math.Min(preferredScale,
            Math.Min(width / Math.Max(1, measured.X), height / Math.Max(1, measured.Y))));
    }

    public static Vector2 Snap(Vector2 position) => new(MathF.Round(position.X), MathF.Round(position.Y));

    public static void DrawString(SpriteBatch batch, SpriteFont font, string text, Vector2 position,
        Color color, float rotation = 0f, Vector2 origin = default, float scale = 1f,
        SpriteEffects effects = SpriteEffects.None, float layerDepth = 0f)
    {
        if (font == null || string.IsNullOrEmpty(text) || scale <= 0) return;
        // 动态内容也通过同一个字符检查入口；正常缩放下将最终起点对齐到像素。
        batch.DrawString(font, UIFontGuard.Sanitize(font, text), rotation == 0f ? Snap(position) : position,
            color, rotation, origin, scale, effects, layerDepth);
    }
}
