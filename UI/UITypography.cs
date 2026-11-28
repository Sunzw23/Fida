using System;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public enum UITextRole { Small, Caption, Name, Body, Heading }

/// <summary>
/// Single source of truth for type size.
///
/// Phone sizes are stated in dp (Android's sp unit at default font scale) and
/// converted to the line-box heights the layout and drawing code consume. That is
/// deliberately one place: sizes authored as design pixels had to be multiplied by
/// a canvas ratio, which is not a physical unit, so the same design size rendered
/// at ~8dp on a 1080px-wide phone and ~16dp on a small one.
///
/// Desktop keeps its own design-pixel scale and per-role minimum so its approved
/// look is untouched.
/// </summary>
public static class UITypography
{
    /// <summary>A typical phone viewport width, used to turn dp into pixels.</summary>
    private const float ReferenceWidthDp = 412f;

    /// <summary>Drawn glyph ink measures about this fraction of a font's line box.</summary>
    private const float InkToLineBox = 0.7f;

    /// <summary>
    /// Dp on phones, plus the desktop design size and its design-pixel floor.
    /// </summary>
    private static (float Dp, float Design, float Minimum) Metrics(UITextRole role) => role switch
    {
        UITextRole.Small => (12f, 22f, 18f),
        UITextRole.Caption => (13f, 26f, 22f),
        UITextRole.Name => (17f, 30f, 24f),
        UITextRole.Body => (16f, 36f, 28f),
        _ => (24f, 52f, 34f)
    };

    /// <summary>Pixels per dp for this viewport.</summary>
    public static float PixelsPerDp(int viewportWidth) =>
        Math.Max(1f, viewportWidth / ReferenceWidthDp);

    /// <summary>Line-box height, in pixels, for a role on this viewport.</summary>
    public static float LineHeight(UITextRole role, int width, int height)
    {
        var (dp, design, minimum) = Metrics(role);
#if FIDA_ANDROID
        return dp * PixelsPerDp(width) / InkToLineBox;
#else
        _ = dp;
        return Math.Max(minimum, design * UIDesktopLayout.Scale(width, height));
#endif
    }

    /// <summary>Sprite scale that makes <paramref name="font"/> render this role.</summary>
    public static float Scale(SpriteFont font, UITextRole role, Viewport viewport) =>
        LineHeight(role, viewport.Width, viewport.Height) / Math.Max(1, font.LineSpacing);

    /// <summary>
    /// Role for a legacy pixel size, so call sites can migrate one at a time
    /// without changing what they express.
    /// </summary>
    public static UITextRole FromDesignPixels(float designPixels) => designPixels switch
    {
        <= 26f => UITextRole.Small,
        <= 34f => UITextRole.Caption,
        <= 42f => UITextRole.Name,
        <= 48f => UITextRole.Body,
        _ => UITextRole.Heading
    };

    /// <summary>
    /// Role matching a font's own atlas size, so labels authored without a role
    /// (the JSON screens) still draw at a physical size instead of scale 1.
    /// </summary>
    public static UITextRole RoleForFont(SpriteFont font) => font.LineSpacing switch
    {
        <= 26 => UITextRole.Small,
        <= 34 => UITextRole.Caption,
        <= 56 => UITextRole.Body,
        _ => UITextRole.Heading
    };
}
