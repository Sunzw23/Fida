using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fida.UI
{
    /// <summary>
    /// UI 配置，从 Content/uiconfig.json 加载，提供 static 属性供全局访问。
    /// </summary>
    public class UIConfig
    {
        private static readonly Lazy<UIConfigData> _data = new(LoadConfig);

        // ─── JSON 数据模型 ────────────────────────────────

        private class UIConfigData
        {
            public string TextColor { get; set; } = "#FFFFFF";
            public string HighlightColor { get; set; } = "#FFFFFF";
            public string BackgroundColor { get; set; } = "#000000";
            public string ButtonTextColor { get; set; } = "#FFFFFF";
            public int Padding { get; set; } = 10;
            public int Spacing { get; set; } = 5;
            public int CardWidth { get; set; } = 300;
            public int CardHeight { get; set; } = 485;
            public int CardBorderThickness { get; set; } = 2;
            public int PortraitSize { get; set; } = 220;
            public int PortraitTopMargin { get; set; } = 43;
            public int NameTopMargin { get; set; } = 43;
            public int NameHeight { get; set; } = 30;
            public int BottomElementWidth { get; set; } = 70;
            public int BottomElementHeight { get; set; } = 70;
            public int InnerBorderMargin { get; set; } = 8;
            public int InnerBorderThickness { get; set; } = 1;
            public int CardBottomPadding { get; set; } = 15;
            public RectData GameMessageArea { get; set; } = new() { X = 10, Y = 10, Width = 400, Height = 30 };
            public Vec2Data GameMessagePosition { get; set; } = new() { X = 200, Y = 30 };
            public RectData DecisionInfoArea { get; set; } = new() { X = 10, Y = 100, Width = 400, Height = 30 };
            public RectData YesButtonArea { get; set; } = new() { X = 60, Y = 160, Width = 100, Height = 60 };
            public RectData NoButtonArea { get; set; } = new() { X = 220, Y = 160, Width = 100, Height = 60 };
            public RectData NumberSelectionArea { get; set; } = new() { X = 10, Y = 100, Width = 260, Height = 110 };
            public int NumberButtonWidth { get; set; } = 50;
            public int NumberButtonHeight { get; set; } = 50;
            public int NumberColumns { get; set; } = 5;
            public Vec2Data PlayerNameOffset { get; set; } = new() { X = 0, Y = 0 };
            public Vec2Data PlayerHpOffset { get; set; } = new() { X = 0, Y = 20 };
        }

        private class RectData { public int X { get; set; } public int Y { get; set; } public int Width { get; set; } public int Height { get; set; } }
        private class Vec2Data { public int X { get; set; } public int Y { get; set; } }

        // ─── 加载逻辑 ──────────────────────────────────────

        private static UIConfigData LoadConfig()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "uiconfig.json");
            if (!File.Exists(path))
                return new UIConfigData();

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<UIConfigData>(json, options) ?? new UIConfigData();
            }
            catch
            {
                return new UIConfigData();
            }
        }

        private static Color ParseHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.White;
            hex = hex.TrimStart('#');
            if (hex.Length != 6) return Color.White;
            try
            {
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return new Color(r, g, b);
            }
            catch
            {
                return Color.White;
            }
        }

        // ─── 公开属性（签名与原来完全一致）─────────────────

        public static Color TextColor => ParseHex(_data.Value.TextColor);
        public static Color HighlightColor => ParseHex(_data.Value.HighlightColor);
        public static Color BackgroundColor => ParseHex(_data.Value.BackgroundColor);
        public static Color ButtonTextColor => ParseHex(_data.Value.ButtonTextColor);

        public static int Padding => _data.Value.Padding;
        public static int Spacing => _data.Value.Spacing;

        public static int CardWidth => _data.Value.CardWidth;
        public static int CardHeight => _data.Value.CardHeight;
        public static int CardBorderThickness => _data.Value.CardBorderThickness;

        public static int PortraitSize => _data.Value.PortraitSize;
        public static int PortraitTopMargin => _data.Value.PortraitTopMargin;

        public static int NameTopMargin => _data.Value.NameTopMargin;
        public static int NameHeight => _data.Value.NameHeight;

        public static int BottomElementWidth => _data.Value.BottomElementWidth;
        public static int BottomElementHeight => _data.Value.BottomElementHeight;

        public static int InnerBorderMargin => _data.Value.InnerBorderMargin;
        public static int InnerBorderThickness => _data.Value.InnerBorderThickness;

        public static int CardBottomPadding => _data.Value.CardBottomPadding;

        public static Rectangle GameMessageArea => new(
            _data.Value.GameMessageArea.X,
            _data.Value.GameMessageArea.Y,
            _data.Value.GameMessageArea.Width,
            _data.Value.GameMessageArea.Height);

        public static Vector2 GameMessagePosition => new(
            _data.Value.GameMessagePosition.X,
            _data.Value.GameMessagePosition.Y);

        public static Rectangle DecisionInfoArea => new(
            _data.Value.DecisionInfoArea.X,
            _data.Value.DecisionInfoArea.Y,
            _data.Value.DecisionInfoArea.Width,
            _data.Value.DecisionInfoArea.Height);

        public static Rectangle YesButtonArea => new(
            _data.Value.YesButtonArea.X,
            _data.Value.YesButtonArea.Y,
            _data.Value.YesButtonArea.Width,
            _data.Value.YesButtonArea.Height);

        public static Rectangle NoButtonArea => new(
            _data.Value.NoButtonArea.X,
            _data.Value.NoButtonArea.Y,
            _data.Value.NoButtonArea.Width,
            _data.Value.NoButtonArea.Height);

        public static Rectangle NumberSelectionArea => new(
            _data.Value.NumberSelectionArea.X,
            _data.Value.NumberSelectionArea.Y,
            _data.Value.NumberSelectionArea.Width,
            _data.Value.NumberSelectionArea.Height);

        public static int NumberButtonWidth => _data.Value.NumberButtonWidth;
        public static int NumberButtonHeight => _data.Value.NumberButtonHeight;
        public static int NumberColumns => _data.Value.NumberColumns;

        public static Point PlayerNameOffset => new(
            _data.Value.PlayerNameOffset.X,
            _data.Value.PlayerNameOffset.Y);

        public static Point PlayerHpOffset => new(
            _data.Value.PlayerHpOffset.X,
            _data.Value.PlayerHpOffset.Y);
    }
}