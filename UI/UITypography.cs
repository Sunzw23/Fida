using System;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public enum UITextRole { Small, Caption, Name, Body, Heading }

/// <summary>Line-box sizes with a physical-pixel floor, independent of atlas resolution.</summary>
public static class UITypography
{
    public static float LineHeight(UITextRole role, int width, int height)
    {
        var (design, minimum) = role switch
        {
            UITextRole.Small => (22f, 18f),
            UITextRole.Caption => (26f, 22f),
            UITextRole.Name => (30f, 24f),
            UITextRole.Body => (36f, 28f),
            _ => (52f, 34f)
        };
#if FIDA_ANDROID
        return design * 1.25f;
#else
        return Math.Max(minimum, design * UIDesktopLayout.Scale(width, height));
#endif
    }

    public static float Scale(SpriteFont font, UITextRole role, Viewport viewport) =>
        LineHeight(role, viewport.Width, viewport.Height) / Math.Max(1, font.LineSpacing);
}
