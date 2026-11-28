#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Rules;

/// <summary>Text fitting shared by the rules pages: one scale rule, one wrap rule.</summary>
public static class UIRulesText
{
    /// <summary>
    /// Converts a design line height into the font's draw scale. Android resolves
    /// the design size to a <see cref="UITextRole"/> and takes the size from the dp
    /// ramp, because the canvas ratio is not a physical unit; desktop keeps the
    /// proportional scale it was designed against.
    /// </summary>
    public static float ScaleFor(SpriteFont font, float lineHeight, float layoutScale)
    {
        if (font.LineSpacing <= 0) return 0;
        float target;
#if FIDA_ANDROID
        // The portrait rules canvas is 900 design px wide, so the current page
        // width is 900 x layout scale; the ramp only needs it to derive px-per-dp.
        int viewportWidth = (int)MathF.Round(900f * layoutScale);
        target = UITypography.LineHeight(UITypography.FromDesignPixels(lineHeight), viewportWidth, 0);
#else
        target = lineHeight * layoutScale;
#endif
        return Math.Max(0f, target / font.LineSpacing);
    }

    public static float Width(SpriteFont font, string text, float layoutScale, float lineHeight)
    {
        float scale = ScaleFor(font, lineHeight, layoutScale);
        return font.MeasureString(UIFontGuard.Sanitize(font, text)).X * scale;
    }

    public static void Line(SpriteBatch batch, SpriteFont font, string text, Rectangle area,
        float lineHeight, float layoutScale, Color color, bool centered = false, bool rightAligned = false)
    {
        if (area.Width <= 0 || area.Height <= 0 || string.IsNullOrEmpty(text)) return;
        float scale = ScaleFor(font, lineHeight, layoutScale);
#if FIDA_ANDROID
        // A single line owns its box: grow it to the role's height instead of
        // letting FitScale shrink the type back to the authored design height.
        float lineBox = font.LineSpacing * scale;
        if (lineBox > area.Height)
        {
            int grown = (int)MathF.Ceiling(lineBox);
            area = new Rectangle(area.X, area.Y - (grown - area.Height) / 2, area.Width, grown);
        }
#endif
        string safe = UIFontGuard.Sanitize(font, text);
        Vector2 size = font.MeasureString(safe);
        float fit = UITextRenderer.FitScale(size, area.Width, area.Height, scale);
        if (fit <= 0) return;
        float x = centered ? area.Center.X - size.X * fit / 2f
            : rightAligned ? area.Right - size.X * fit : area.Left;
        float y = area.Top + (area.Height - size.Y * fit) / 2f;
        UITextRenderer.DrawString(batch, font, safe, new Vector2(x, y), color, scale: fit);
    }

    /// <summary>Left-aligned paragraph that wraps first and only shrinks when it cannot fit.</summary>
    public static void Paragraph(SpriteBatch batch, SpriteFont font, string text, Rectangle area,
        float lineHeight, float layoutScale, Color color, float lineSpacing = 1.15f)
    {
        if (area.Width <= 0 || area.Height <= 0) return;
        float scale = ScaleFor(font, lineHeight, layoutScale);
        if (scale <= 0) return;
        string safe = UIFontGuard.Sanitize(font, text);
        var lines = UITextRenderer.WrapLines(safe, area.Width, value => font.MeasureString(value).X * scale);
        float row = font.LineSpacing * scale * lineSpacing;
        if (lines.Count > 0 && lines.Count * row > area.Height)
        {
            scale *= area.Height / (lines.Count * row);
            lines = UITextRenderer.WrapLines(safe, area.Width, value => font.MeasureString(value).X * scale);
            row = font.LineSpacing * scale * lineSpacing;
        }
        float y = area.Top;
        foreach (string line in lines)
        {
            UITextRenderer.DrawString(batch, font, line, new Vector2(area.Left, y), color, scale: scale);
            y += row;
        }
    }

    /// <summary>Word-wrapped lines without drawing, for hit regions and diagnostics.</summary>
    public static IReadOnlyList<string> Wrap(SpriteFont font, string text, float width, float lineHeight, float layoutScale)
    {
        float scale = ScaleFor(font, lineHeight, layoutScale);
        return UITextRenderer.WrapLines(UIFontGuard.Sanitize(font, text), width,
            value => font.MeasureString(value).X * scale);
    }
}
