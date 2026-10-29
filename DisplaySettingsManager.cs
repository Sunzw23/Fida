#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida
{
    public sealed class DisplaySettingsData
    {
        public bool Fullscreen { get; set; }
        public bool ReduceMotion { get; set; }
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1200;
    }

    public static class DisplaySettingsManager
    {
        private static readonly (int Width, int Height)[] ResolutionPresets =
            [(1280, 720), (1366, 768), (1600, 900), (1680, 1050),
             (1920, 1080), (1920, 1200), (2240, 1400), (2560, 1440), (2560, 1600)];
        private static GraphicsDeviceManager? _graphics;
        public static DisplaySettingsData Current { get; private set; } = new();
        public static string ResolutionLabel => $"{Current.Width} x {Current.Height}";
        public static int LayoutWidth => _graphics?.GraphicsDevice.Viewport.Width ?? Current.Width;
        public static int LayoutHeight => _graphics?.GraphicsDevice.Viewport.Height ?? Current.Height;

        public static void Initialize(GraphicsDeviceManager graphics)
        {
            _graphics = graphics;
            Current = Load();
            Normalize(Current, GetWindowResolutions(
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height));
            Apply();
        }

        /// <summary>Registers the native full-screen surface without applying desktop window modes.</summary>
        public static void InitializeMobile(GraphicsDeviceManager graphics)
        {
            _graphics = graphics;
            Current = Load();
            Current.Fullscreen = true;
        }

        public static void SetFullscreen(bool fullscreen)
        {
#if FIDA_ANDROID
            return;
#else
            if (Current.Fullscreen == fullscreen) return;
            Current.Fullscreen = fullscreen;
            ApplyAndSave();
#endif
        }

        public static void ToggleReducedMotion()
        {
            Current.ReduceMotion = !Current.ReduceMotion;
            Save();
        }

        public static void CycleResolution()
        {
#if FIDA_ANDROID
            return;
#else
            var resolutions = GetCurrentWindowResolutions();
            int index = Array.FindIndex(resolutions,
                value => value.Width == Current.Width && value.Height == Current.Height);
            var next = resolutions[(index + 1 + resolutions.Length) % resolutions.Length];
            Current.Width = next.Width;
            Current.Height = next.Height;
            ApplyAndSave();
#endif
        }

        public static void Normalize(DisplaySettingsData settings)
        {
            if (Array.FindIndex(ResolutionPresets,
                value => value.Width == settings.Width && value.Height == settings.Height) < 0)
            {
                settings.Width = 1920;
                settings.Height = 1200;
            }
        }

        private static void Normalize(DisplaySettingsData settings,
            (int Width, int Height)[] resolutions)
        {
            if (Array.FindIndex(resolutions,
                value => value.Width == settings.Width && value.Height == settings.Height) < 0)
            {
                (int Width, int Height) fallback = resolutions.Length > 0
                    ? resolutions[^1] : (1280, 720);
                settings.Width = fallback.Width;
                settings.Height = fallback.Height;
            }
        }

        /// <summary>Window presets are limited to the current display's usable client area.</summary>
        public static (int Width, int Height)[] GetWindowResolutions(int desktopWidth, int desktopHeight)
        {
            int maxWidth = Math.Max(1, desktopWidth - 32);
            int maxHeight = Math.Max(1, desktopHeight - 96);
            var available = Array.FindAll(ResolutionPresets,
                mode => mode.Width <= maxWidth && mode.Height <= maxHeight);
            if (available.Length > 0) return available;

            return [(maxWidth, maxHeight)];
        }

        private static (int Width, int Height)[] GetCurrentWindowResolutions()
        {
            DisplayMode desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            return GetWindowResolutions(desktop.Width, desktop.Height);
        }

        private static void ApplyAndSave()
        {
            Apply();
            Save();
        }

        private static void Apply()
        {
            if (_graphics == null) return;
            DisplayMode desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            var backBuffer = ResolveBackBuffer(Current, desktop.Width, desktop.Height);

            // Borderless fullscreen must use the desktop mode. Reusing the selected
            // window size here can create an oversized surface or fail on 16:9 displays.
            _graphics.HardwareModeSwitch = false;
            _graphics.PreferredBackBufferWidth = backBuffer.Width;
            _graphics.PreferredBackBufferHeight = backBuffer.Height;
            _graphics.IsFullScreen = Current.Fullscreen;
            _graphics.ApplyChanges();
        }

        public static (int Width, int Height) ResolveBackBuffer(
            DisplaySettingsData settings, int desktopWidth, int desktopHeight) =>
            settings.Fullscreen
                ? (Math.Max(1, desktopWidth), Math.Max(1, desktopHeight))
                : (Math.Min(settings.Width, Math.Max(1, desktopWidth - 32)),
                    Math.Min(settings.Height, Math.Max(1, desktopHeight - 96)));

        private static DisplaySettingsData Load()
        {
            try
            {
                string path = SettingsPath();
                return File.Exists(path)
                    ? JsonSerializer.Deserialize<DisplaySettingsData>(File.ReadAllText(path)) ?? new()
                    : new();
            }
            catch { return new(); }
        }

        private static void Save()
        {
            try
            {
                string path = SettingsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private static string SettingsPath() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Fida", "settings.json");
    }
}
