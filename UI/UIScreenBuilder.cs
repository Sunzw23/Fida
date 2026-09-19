// UIScreenBuilder.cs 文件
#nullable enable
using System;
using System.Collections.Generic;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Fida.UI.Campaign;
using CampaignBalance = Fida.Campaign.CampaignSkillBalance;

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
        private readonly UICampaignScreenBuilder _campaignScreens;

        public UIScreenBuilder(
            ResourceManager resourceManager,
            List<UIControl> uiControls,
            UIFocusManager focusManager)
        {
            _resourceManager = resourceManager;
            _uiControls = uiControls;
            _focusManager = focusManager;
            _campaignScreens = new UICampaignScreenBuilder(resourceManager, uiControls, focusManager);
        }

        /// <summary>构建主菜单</summary>
        public void BuildMainMenu(bool connected = false, bool isHost = false)
        {
            Clear();
            if (connected)
            {
                BuildFromScreen("ConnectedMainMenu", new Dictionary<string, string>
                {
                    { "role", isHost ? "HOST" : "GUEST" }
                });
            }
            else BuildFromScreen("MainMenu");
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
            BuildFromScreen("Settings", new Dictionary<string, string>
            {
                { "mode", DisplaySettingsManager.Current.Fullscreen ? "Fullscreen" : "Windowed" },
                { "resolution", DisplaySettingsManager.ResolutionLabel }
            });
        }

        /// <summary>构建规则界面</summary>
        public void BuildRulesScreen()
        {
            Clear();
        }

        /// <summary>构建在线菜单</summary>
        public void BuildOnlineMenu(bool connected = false)
        {
            Clear();
            BuildFromScreen(connected ? "ConnectedOnlineMenu" : "OnlineMenu");
        }

        /// <summary>构建房主等待界面</summary>
        public void BuildHostingScreen(string statusText, string roomCode = "")
        {
            Clear();
            var replacements = new Dictionary<string, string>
            {
                { "status", statusText },
                { "roomCode", string.IsNullOrEmpty(roomCode) ? "generating..." : roomCode }
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

        /// <summary>Builds the shared ban/pick board from the canonical skill catalog.</summary>
        public void BuildDuelDraft()
        {
            Clear();
            _uiControls.Add(new UIPanel(0, _resourceManager.Pixel) { Name = "panel" });
            _uiControls.Add(new UITitle(1, "Duel Draft", new Vector2(960, 105),
                _resourceManager.Pixel, _resourceManager.TitleFont) { Name = "title" });
            _uiControls.Add(new UILabel(2, "BAN - choose one skill", new Vector2(960, 190),
                _resourceManager.Font) { Name = "draftPhase", MaximumWidth = 1300 });
            _uiControls.Add(new UILabel(3, "Both bans are revealed together.", new Vector2(960, 238),
                _resourceManager.Font) { Name = "draftStatus", MaximumWidth = 1500 });

            int index = 0;
            foreach (SkillType skill in SkillFactory.SelectableSkills)
            {
                int column = index % 2;
                int row = index / 2;
                var bounds = new Rectangle(365 + column * 610, 278 + row * 66, 580, 54);
                var button = new UIButton(500 + index, bounds,
                    $"{GameText.SkillName(skill)}  [{CampaignBalance.Cost(skill)} SP]",
                    _resourceManager.Pixel, _resourceManager.Font)
                {
                    Name = $"draftSkill_{skill}",
                    FocusIndex = index
                };
                _uiControls.Add(button);
                index++;
            }

            _uiControls.Add(new UIButton(550, new Rectangle(620, 885, 320, 72),
                "Lock Loadout", _resourceManager.Pixel, _resourceManager.Font)
            { Name = "draftCommit", FocusIndex = index++ });
            _uiControls.Add(new UIButton(551, new Rectangle(980, 885, 320, 72),
                "Disconnect", _resourceManager.Pixel, _resourceManager.Font)
            { Name = "draftBack", FocusIndex = index });
            if (_focusManager.CurrentInputMode == InputMode.Controller) _focusManager.FocusFirst();
        }

        /// <summary>构建关卡选择界面</summary>
        public void BuildLevelSelect()
        {
            Clear();
            _campaignScreens.BuildMap();
        }

        /// <summary>构建技能选择界面</summary>
        public void BuildSkillSelect()
        {
            Clear();
            _campaignScreens.BuildLoadout(GameSetup.SelectedNode);
        }

        public void BuildCampaignResult(bool won, string message) =>
            _campaignScreens.BuildResult(won, message, GameSetup.SelectedNode);

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
                    "title" => new UITitle(desc.Id, desc.Text, desc.Position!.Value, _resourceManager.Pixel, _resourceManager.TitleFont),
                    "panel" => new UIPanel(desc.Id, _resourceManager.Pixel),
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
