using Microsoft.Xna.Framework;
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Fida.Logic;

namespace Fida.UI
{
    public static class UIConfig
    {
        private static Dictionary<string, JsonElement> _settings = new();
        private static readonly string PathConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "uiconfig.json");

        static UIConfig() => Reload();

        public static void Reload()
        {
            try
            {
                string? json = ContentSource.ReadText(PathConfig);
                if (json != null)
                {
                    _settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
                }
            }
            catch (Exception ex) { Console.WriteLine($"UIConfig Load Error: {ex.Message}"); }
        }

        // --- 通用获取方法 ---

        public static int Int(string key, int def = 0)
        {
#if FIDA_ANDROID
            // Card metrics are derived from the actual portrait viewport rather
            // than the desktop JSON canvas. This keeps text and touch targets
            // readable on both phones and tablets without changing the rules UI.
            int width = Math.Max(1, DisplaySettingsManager.LayoutWidth);
            int height = Math.Max(1, DisplaySettingsManager.LayoutHeight);
            if (key == "cardWidth") return Math.Max(1, width - 48);
            // Reserve roughly one fifth of the viewport per card so a 2v2
            // portrait battle remains visible without scrolling.
            if (key == "cardHeight") return Math.Clamp(height / 5, 320, 480);
            if (key == "portraitSize") return Math.Clamp(Math.Min(width - 96, height / 8), 160, 320);
            if (key == "nameHeight") return 48;
            if (key == "bottomElementWidth") return Math.Clamp(width / 6, 74, 150);
            if (key == "bottomElementHeight") return Math.Clamp(height / 24, 68, 104);
#endif
            return _settings.TryGetValue(key, out var e) ? e.GetInt32() : def;
        }

        public static float Float(string key, float def = 0f) => _settings.TryGetValue(key, out var e) ? e.GetSingle() : def;

        public static string String(string key, string def = "") => _settings.TryGetValue(key, out var e) ? e.GetString() : def;

        public static Color GetColor(string key)
        {
            string hex = String(key, "#FFFFFF").TrimStart('#');
            if (hex.Length != 6) return Color.White;
            return new Color(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16)
            );
        }

        public static Rectangle Rect(string key)
        {
            if (_settings.TryGetValue(key, out var e) && e.ValueKind == JsonValueKind.Object)
            {
                return new Rectangle(
                    e.GetProperty("x").GetInt32(),
                    e.GetProperty("y").GetInt32(),
                    e.GetProperty("width").GetInt32(),
                    e.GetProperty("height").GetInt32()
                );
            }
            return Rectangle.Empty;
        }

        public static Vector2 Vec2(string key)
        {
            if (_settings.TryGetValue(key, out var e) && e.ValueKind == JsonValueKind.Object)
            {
                return new Vector2(
                    e.GetProperty("x").GetSingle(),
                    e.GetProperty("y").GetSingle()
                );
            }
            return Vector2.Zero;
        }
    }
}
