#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Fida.Logic;
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;

namespace Fida.UI;

/// <summary>Gum owns modal geometry; the existing paper renderer and input controls share its rectangles.</summary>
public static class UIGumLayout
{
    private static GraphicsDevice? _device;
    private static bool _initialized;
    private static readonly Dictionary<string, GraphicalUiElement> Screens = new();

    public static void Initialize(GraphicsDevice device)
    {
        // GumService is a process-wide singleton and refuses a second Initialize.
        // Android rebuilds the graphics device when the surface is recreated (a
        // display-size change, or the process resuming into a new surface), so the
        // guard tracks that we already loaded the project instead of comparing the
        // device: re-initialising threw "Initialize has already been called once".
        // Only layout rectangles are read back from Gum, never rendering, so the
        // loaded screens stay valid against a replacement device.
        if (_initialized)
        {
            _device = device;
            return;
        }
        // Both platforms hand Gum an absolute file path. Gum's MonoGame loader
        // prefixes "Content/" when it is given a relative path and then resolves
        // each screen the same way again, which asks Android for
        // "./Content/Content/Gum/Screens/*.gusx" and throws while the app starts.
        string path = ContentSource.GumProjectPath
            ?? Path.Combine(AppContext.BaseDirectory, "Content", "Gum", "Fida.gumx");
        GumService.Default.Initialize(device, path);
        Screens.Clear();
        var project = ObjectFinder.Self.GumProjectSave
            ?? throw new InvalidDataException("Gum layout project could not be loaded.");
        foreach (var screen in project.Screens)
        {
            var runtime = screen.ToGraphicalUiElement();
            runtime.WidthUnits = DimensionUnitType.Absolute;
            runtime.HeightUnits = DimensionUnitType.Absolute;
            // A Gum Screen normally uses the service's global canvas. Give this
            // layout-only tree its own root so multiple aspect ratios stay isolated.
            foreach (var element in runtime.ContainedElements)
                if (element.Parent == null) element.Parent = runtime;
            Screens.Add(screen.Name, runtime);
        }
        _device = device;
        _initialized = true;
    }

    public static Rectangle Bounds(string screenName, string elementName, int width, int height)
    {
        var layout = new UIEditorialLayout(width, height);
        return Bounds(screenName, elementName, width, height, layout.Scale);
    }

    /// <summary>Allows a reusable screen to define its own design canvas without changing other pages.</summary>
    public static Rectangle Bounds(string screenName, string elementName, int width, int height, float scale)
    {
        bool portrait = height > width;
        string key = screenName + (portrait ? "Portrait" : "Landscape");
        if (!Screens.TryGetValue(key, out var screen))
            throw new InvalidOperationException($"Gum screen '{key}' was not loaded.");
        float canvasWidth = width / scale, canvasHeight = height / scale;
        if (screen.Width != canvasWidth || screen.Height != canvasHeight)
        {
            screen.Width = canvasWidth;
            screen.Height = canvasHeight;
            screen.UpdateLayout();
        }
        var element = screen.GetGraphicalUiElementByName(elementName)
            ?? throw new InvalidDataException($"Gum screen '{key}' is missing '{elementName}'.");
        return new Rectangle(
            (int)Math.Round(element.GetAbsoluteX() * scale),
            (int)Math.Round(element.GetAbsoluteY() * scale),
            Math.Max(0, (int)Math.Round(element.GetAbsoluteWidth() * scale)),
            Math.Max(0, (int)Math.Round(element.GetAbsoluteHeight() * scale)));
    }

    public static Rectangle Bounds(string screen, string element, Viewport viewport) =>
        Bounds(screen, element, viewport.Width, viewport.Height);
}
