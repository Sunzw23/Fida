// UIScreenBuilder.cs 文件
#nullable enable
using System;
using System.Collections.Generic;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// 屏幕构建：从 JSON 布局文件加载并实例化控件。
    /// 各 BuildXxx 方法对应一个屏幕（主菜单 / 暂停 / 设置 / 规则 / 在线菜单 / 房主 / 连接 / 大厅）。
    /// 从 UIManager 中提取，便于独立维护。
    /// </summary>
    public class UIScreenBuilder
    {
        private readonly ResourceManager _resourceManager;
        private readonly List<UIControl> _uiControls;
        private readonly UIFocusManager _focusManager;

        public UIScreenBuilder(
            ResourceManager resourceManager,
            List<UIControl> uiControls,
            UIFocusManager focusManager)
        {
            _resourceManager = resourceManager;
            _uiControls = uiControls;
            _focusManager = focusManager;
        }

        /// <summary>构建主菜单</summary>
        public void BuildMainMenu()
        {
            Clear();
            BuildFromScreen("MainMenu");
        }

        /// <summary>构建暂停菜单</summary>
        public void BuildPauseMenu()
        {
            Clear();
            BuildFromScreen("PauseMenu");
        }

        /// <summary>构建设置界面</summary>
        public void BuildSettingsScreen()
        {
            Clear();
        }

        /// <summary>构建规则界面</summary>
        public void BuildRulesScreen()
        {
            Clear();
        }

        /// <summary>构建在线菜单</summary>
        public void BuildOnlineMenu()
        {
            Clear();
            BuildFromScreen("OnlineMenu");
        }

        /// <summary>构建房主等待界面</summary>
        public void BuildHostingScreen(string statusText, string roomCode = "")
        {
            Clear();
            var replacements = new Dictionary<string, string>
            {
                { "status", statusText },
                { "roomCode", string.IsNullOrEmpty(roomCode) ? "生成中..." : roomCode }
            };
            BuildFromScreen("HostingScreen", replacements);
        }

        /// <summary>构建连接中界面</summary>
        public void BuildConnectingScreen(string statusText)
        {
            Clear();
            var replacements = new Dictionary<string, string> { { "status", statusText } };
            BuildFromScreen("ConnectingScreen", replacements);
        }

        /// <summary>构建在线大厅</summary>
        public void BuildOnlineLobby(bool isHost)
        {
            Clear();
            BuildFromScreen("OnlineLobby");
        }

        /// <summary>清空控件列表（由构建方法调用，不直接对外）</summary>
        public void Clear()
        {
            _uiControls.Clear();
        }

        /// <summary>
        /// 从 JSON 屏幕布局文件构建控件。
        /// 自动为按钮设置 FocusIndex（按添加顺序），并聚焦第一个按钮。
        /// </summary>
        private void BuildFromScreen(string screenName, Dictionary<string, string>? replacements = null)
        {
            var descriptors = UIScreenLoader.Load(screenName, replacements);
            int focusIndex = 0;
            foreach (var desc in descriptors)
            {
                UIControl control = desc.Type switch
                {
                    "button" => new UIButton(desc.Id, desc.Bounds!.Value, desc.Text, _resourceManager.Pixel, _resourceManager.Font),
                    "label" => new UILabel(desc.Id, desc.Text, desc.Position!.Value, _resourceManager.Font),
                    "textbox" => new UIRoomCodeInput(desc.Id, desc.Bounds!.Value, desc.Text, _resourceManager.Pixel, _resourceManager.Font),
                    _ => throw new System.NotSupportedException($"Unknown control type: {desc.Type}")
                };
                // 设置控件名称（用于事件绑定查找）
                control.Name = desc.Name;
                // 为按钮设置 FocusIndex，使其可被手柄聚焦
                if (control is UIButton)
                {
                    control.FocusIndex = focusIndex++;
                }
                _uiControls.Add(control);
            }
            // 仅在控制器模式下自动聚焦第一个按钮，避免鼠标模式下显示焦点
            if (_focusManager.CurrentInputMode == InputMode.Controller)
            {
                _focusManager.FocusFirst();
            }
        }
    }
}
