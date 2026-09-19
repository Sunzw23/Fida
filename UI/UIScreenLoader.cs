#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fida.UI
{
    /// <summary>
    /// 从 Content/screens/ 目录加载 JSON 屏幕布局文件，返回控件描述列表。
    /// UIManager 根据这些描述创建实际的 UIControl 对象。
    /// </summary>
    public static class UIScreenLoader
    {
        // ─── JSON 数据模型 ────────────────────────────────

        private class ScreenData
        {
            [JsonPropertyName("controls")]
            public List<ControlData> Controls { get; set; } = new();
        }

        private class ControlData
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "";

            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("text")]
            public string Text { get; set; } = "";

            [JsonPropertyName("bounds")]
            public RectData? Bounds { get; set; }

            [JsonPropertyName("position")]
            public Vec2Data? Position { get; set; }
        }

        private class RectData
        {
            [JsonPropertyName("x")]
            public int X { get; set; }

            [JsonPropertyName("y")]
            public int Y { get; set; }

            [JsonPropertyName("width")]
            public int Width { get; set; }

            [JsonPropertyName("height")]
            public int Height { get; set; }

            public Rectangle ToRectangle() => new(X, Y, Width, Height);
        }

        private class Vec2Data
        {
            [JsonPropertyName("x")]
            public int X { get; set; }

            [JsonPropertyName("y")]
            public int Y { get; set; }

            public Vector2 ToVector2() => new(X, Y);
        }

        // ─── 公开的控件描述（UIManager 使用）──────────────

        /// <summary>
        /// 控件描述，UIManager 根据此信息创建实际的 UIControl 对象。
        /// </summary>
        public class ControlDescriptor
        {
            /// <summary>控件类型："button" 或 "label"</summary>
            public string Type { get; set; } = "";

            /// <summary>控件 ID</summary>
            public int Id { get; set; }

            /// <summary>控件名称（用于事件绑定查找）</summary>
            public string Name { get; set; } = "";

            /// <summary>显示文字（已替换占位符）</summary>
            public string Text { get; set; } = "";

            /// <summary>按钮的矩形区域（仅 button 类型）</summary>
            public Rectangle? Bounds { get; set; }

            /// <summary>标签的位置（仅 label 类型）</summary>
            public Vector2? Position { get; set; }
        }

        // ─── 加载方法 ──────────────────────────────────────

        private static readonly string ScreensDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Content", "screens");

        /// <summary>
        /// 加载指定屏幕的布局文件。
        /// </summary>
        /// <param name="screenName">屏幕名称（对应文件名，不含 .json）</param>
        /// <param name="replacements">文字占位符替换表，例如 { "status": "正在创建房间..." }</param>
        /// <returns>控件描述列表，文件不存在或解析失败时返回空列表</returns>
        public static List<ControlDescriptor> Load(string screenName, Dictionary<string, string>? replacements = null)
        {
            string path = Path.Combine(ScreensDirectory, screenName + ".json");
            if (!File.Exists(path))
                return new List<ControlDescriptor>();

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var screenData = JsonSerializer.Deserialize<ScreenData>(json, options);
                if (screenData == null)
                    return new List<ControlDescriptor>();

                var result = new List<ControlDescriptor>();
                foreach (var ctrl in screenData.Controls)
                {
                    string text = ctrl.Text;
                    if (replacements != null)
                    {
                        foreach (var kvp in replacements)
                        {
                            text = text.Replace("{" + kvp.Key + "}", kvp.Value);
                        }
                    }

                    var descriptor = new ControlDescriptor
                    {
                        Type = ctrl.Type,
                        Id = ctrl.Id,
                        Name = ctrl.Name,
                        Text = text,
                    };

                    if (ctrl.Bounds != null)
                    {
                        Rectangle bounds = ctrl.Bounds.ToRectangle();
                        float scaleX = DisplaySettingsManager.Current.Width / 1920f;
                        float scaleY = DisplaySettingsManager.Current.Height / 1200f;
                        descriptor.Bounds = new Rectangle(
                            (int)(bounds.X * scaleX), (int)(bounds.Y * scaleY),
                            (int)(bounds.Width * scaleX), (int)(bounds.Height * scaleY));
                    }

                    if (ctrl.Position != null)
                    {
                        Vector2 position = ctrl.Position.ToVector2();
                        descriptor.Position = new Vector2(
                            position.X * DisplaySettingsManager.Current.Width / 1920f,
                            position.Y * DisplaySettingsManager.Current.Height / 1200f);
                    }

                    result.Add(descriptor);
                }

                return result;
            }
            catch
            {
                return new List<ControlDescriptor>();
            }
        }
    }
}
