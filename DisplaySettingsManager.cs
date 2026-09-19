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
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1200;
    }

    public static class DisplaySettingsManager
    {
        private static readonly (int Width, int Height)[] Resolutions =
            [(1920, 1200), (2240, 1400), (2560, 1600)];
        private static GraphicsDeviceManager? _graphics;
        public static DisplaySettingsData Current { get; private set; } = new();
        public static string ResolutionLabel => $"{Current.Width} x {Current.Height}";

        public static void Initialize(GraphicsDeviceManager graphics)
        {
            _graphics = graphics;
            Current = Load();
            Normalize(Current);
            Apply();
        }

        public static void SetFullscreen(bool fullscreen)
        {
            Current.Fullscreen = fullscreen;
            ApplyAndSave();
        }

        public static void CycleResolution()
        {
            int index = Array.FindIndex(Resolutions,
                value => value.Width == Current.Width && value.Height == Current.Height);
            var next = Resolutions[(index + 1 + Resolutions.Length) % Resolutions.Length];
            Current.Width = next.Width;
            Current.Height = next.Height;
            ApplyAndSave();
        }

        public static void Normalize(DisplaySettingsData settings)
        {
            if (settings.Width < 1024 || settings.Height < 576 ||
                settings.Width > 7680 || settings.Height > 4320)
            {
                settings.Width = 1920;
                settings.Height = 1200;
            }
        }

        private static void ApplyAndSave()
        {
            Apply();
            Save();
        }

        private static void Apply()
        {
            if (_graphics == null) return;
            _graphics.PreferredBackBufferWidth = Current.Width;
            _graphics.PreferredBackBufferHeight = Current.Height;
            _graphics.IsFullScreen = Current.Fullscreen;
            _graphics.HardwareModeSwitch = false;
            _graphics.ApplyChanges();
        }

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
