using System;
using Microsoft.Xna.Framework;

namespace Fida.UI;

/// <summary>Fixed input geometry on the shared design canvas, independent of animation.</summary>
public static class UIBattleLayout
{
    public static Rectangle Unit(int index, int count, bool ally, int width, int height)
    {
        if (count < 1 || index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
        int columns = Math.Min(count, 4);
        int rows = (count + columns - 1) / columns;
        int row = index / columns;
        int inRow = Math.Min(columns, count - row * columns);
        int slotWidth = 1600 / columns;
        int unitWidth = Math.Min(380, slotWidth - 32);
        int unitHeight = Math.Min(390, 410 / rows);
        int x = 160 + (1600 - inRow * slotWidth) / 2 + (index % columns) * slotWidth + (slotWidth - unitWidth) / 2;
        int y = (ally ? 680 : 160) + row * (410 / rows);
        return UIDesktopLayout.MapRectangle(new Rectangle(x, y, unitWidth, unitHeight), width, height);
    }

    public static Rectangle Number(Rectangle unit, int index)
    {
        int gap = Math.Max(6, unit.Width / 24);
        int size = Math.Min((unit.Width - gap) / 2, unit.Height * 43 / 100);
        int left = unit.Center.X - (size * 2 + gap) / 2;
        return new Rectangle(left + index * (size + gap), unit.Bottom - size, size, size);
    }
}
