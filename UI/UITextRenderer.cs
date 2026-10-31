using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>统一文字缩放与像素对齐，避免在已缩放的控件内二次缩小文字。</summary>
public static class UITextRenderer
{
    /// <summary>Wrap at words, preserving paragraphs and splitting unusually long identifiers.</summary>
    public static IReadOnlyList<string> WrapLines(string text, float width, Func<string, float> measure)
    {
        var lines = new List<string>();
        if (width <= 0) return lines;
        foreach (string paragraph in (text ?? "").Replace("\r", "").Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (measure(candidate) <= width) { line = candidate; continue; }
                if (line.Length > 0) { lines.Add(line); line = ""; }
                foreach (char letter in word)
                {
                    if (line.Length > 0 && measure(line + letter) > width)
                    { lines.Add(line); line = ""; }
                    line += letter;
                }
            }
            lines.Add(line);
        }
        return lines;
    }

    public static void DrawParagraph(SpriteBatch batch, SpriteFont font, string text, Rectangle area, Color color, bool centered = false)
    {
        if (area.Width <= 0 || area.Height <= 0) return;
        float scale = UITypography.Scale(font, UITextRole.Body, batch.GraphicsDevice.Viewport);
        IReadOnlyList<string> lines;
        float lineHeight;
        for (int attempt = 0; ; attempt++)
        {
            lines = WrapLines(UIFontGuard.Sanitize(font, text), area.Width, value => font.MeasureString(value).X * scale);
            lineHeight = font.LineSpacing * scale * 1.2f;
            if (lines.Count * lineHeight <= area.Height || attempt >= 40) break;
            scale *= 0.95f;
        }
        float y = area.Top;
        foreach (string line in lines)
        {
            DrawString(batch, font, line, new Vector2(centered ? area.Center.X-font.MeasureString(line).X*scale/2 : area.Left, y), color, scale: scale);
            y += lineHeight;
        }
    }

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
