// UIScreenBuilder.cs 文件
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Fida.UI.Campaign;

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
#if FIDA_ANDROID
            BuildMobileSettingsScreen();
#else
            BuildFromScreen("Settings", new Dictionary<string, string>
            {
                { "mode", DisplaySettingsManager.Current.Fullscreen ? "Fullscreen" : "Windowed" },
                { "resolution", DisplaySettingsManager.ResolutionLabel }
            });
#endif
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
#if FIDA_ANDROID
            BuildMobileHostingScreen(roomCode, statusText);
#else
            var replacements = new Dictionary<string, string>
            {
                { "status", statusText },
                { "roomCode", string.IsNullOrEmpty(roomCode) ? "generating..." : roomCode }
            };
            BuildFromScreen("HostingScreen", replacements);
#endif
        }

        /// <summary>构建连接中界面</summary>
        public void BuildConnectingScreen(string statusText)
        {
            Clear();
#if FIDA_ANDROID
            BuildMobileConnectingScreen(statusText);
#else
            var replacements = new Dictionary<string, string> { { "status", statusText } };
            BuildFromScreen("ConnectingScreen", replacements);
#endif
        }

        /// <summary>构建在线大厅</summary>
        public void BuildOnlineLobby(bool isHost)
        {
            Clear();
            BuildFromScreen("OnlineLobby", new Dictionary<string, string>
            {
                { "role", isHost ? "HOST" : "GUEST" }
            });
            if (!isHost)
            {
                _uiControls.RemoveAll(control => control.Name is "startPvP" or "startCampaign");
                Rectangle panel = UITheme.PanelRect(_resourceManager.GraphicsDevice.Viewport);
                _uiControls.Add(new UILabel(6, "WAITING FOR HOST",
                    new Vector2(panel.Center.X, panel.Top + 365), _resourceManager.Font)
                {
                    Name = "lobbyStatus",
                    MaximumWidth = panel.Width - 96,
                    MinimumScale = 0.62f
                });
                if (_focusManager.CurrentInputMode == InputMode.Controller)
                    _focusManager.FocusFirst();
            }
        }

        /// <summary>Builds the shared ban/pick board from the canonical skill catalog.</summary>
        public void BuildDuelDraft()
        {
            Clear();
            var node = new Fida.Campaign.CampaignNode
            {
                Name = "DUEL DRAFT",
                SkillBudget = Fida.Network.OnlineModeRules.DuelSkillBudget
            };
            var progress = new Fida.Campaign.CampaignProgress
            {
                UnlockedSkills = SkillFactory.SelectableSkills.ToHashSet()
            };
#if FIDA_ANDROID
            var options = CampaignSkillWheelOptions.Mobile();
#else
            CampaignSkillWheelOptions options = null;
#endif
            _uiControls.Add(new UICampaignSkillWheel(0, _resourceManager.GraphicsDevice,
                _resourceManager.Pixel, _resourceManager.Font, _resourceManager.TitleFont,
                node, progress, new HashSet<SkillType>(), SkillFactory.SelectableSkills, options)
            {
                Name = "duel_draft_wheel",
                HeaderText = "BAN"
            });
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
#if FIDA_ANDROID
            BuildMobileFromDescriptors(descriptors);
#else
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
#endif
        }

#if FIDA_ANDROID
        private void BuildMobileHostingScreen(string roomCode, string statusText)
        {
            Rectangle panel = UITheme.PanelRect(_resourceManager.GraphicsDevice.Viewport);
            _uiControls.Add(new UIPanel(0, _resourceManager.Pixel) { Name = "panel" });
            AddMobileWaitingTitle(panel, "HOST ROOM");

            int codeTop = panel.Top + Math.Max(200, panel.Height * 30 / 100);
            var codeBox = new Rectangle(panel.Left + 38, codeTop,
                Math.Max(1, panel.Width - 76), 146);
            _uiControls.Add(new UIBoxLabel(2, codeBox, "", _resourceManager.Pixel,
                _resourceManager.Font)
            {
                Name = "codePanel",
                BackColor = UITheme.PanelDark,
                BorderThickness = 0,
                AccentBar = true
            });
            _uiControls.Add(new UILabel(3,
                roomCode.Length == 4 ? $"Room: {roomCode}" : "Room: generating...",
                new Vector2(codeBox.Center.X, codeBox.Center.Y), _resourceManager.Font)
            {
                Name = "roomCodeLabel",
                MaximumWidth = codeBox.Width - 36,
                MaximumHeight = 72
            });
            _uiControls.Add(new UILabel(4,
                string.IsNullOrWhiteSpace(statusText) ? "WAITING FOR PLAYER" : statusText.ToUpperInvariant(),
                new Vector2(panel.Center.X, codeBox.Bottom + 64), _resourceManager.Font)
            {
                Name = "statusLabel",
                MaximumWidth = panel.Width - 76,
                MaximumHeight = 42
            });
            _uiControls.Add(new UIIconButton(5, MobileWaitingBackBounds(panel),
                UIIconButton.IconKind.Back, _resourceManager.Pixel, _resourceManager.Font)
            { Name = "cancelHost", FocusIndex = 0 });
        }

        private void BuildMobileConnectingScreen(string statusText)
        {
            Rectangle panel = UITheme.PanelRect(_resourceManager.GraphicsDevice.Viewport);
            _uiControls.Add(new UIPanel(0, _resourceManager.Pixel) { Name = "panel" });
            AddMobileWaitingTitle(panel, "JOIN ROOM");
            _uiControls.Add(new UILabel(2,
                string.IsNullOrWhiteSpace(statusText) ? "CONNECTING..." : statusText.ToUpperInvariant(),
                new Vector2(panel.Center.X, panel.Top + panel.Height * 42 / 100),
                _resourceManager.Font)
            {
                Name = "statusLabel",
                MaximumWidth = panel.Width - 76,
                MaximumHeight = 64
            });
            _uiControls.Add(new UIIconButton(3, MobileWaitingBackBounds(panel),
                UIIconButton.IconKind.Back, _resourceManager.Pixel, _resourceManager.Font)
            { Name = "cancelConnect", FocusIndex = 0 });
        }

        private void AddMobileWaitingTitle(Rectangle panel, string title)
        {
            _uiControls.Add(new UILabel(1, title,
                new Vector2(panel.Center.X, panel.Top + 88), _resourceManager.TitleFont)
            {
                Name = "title",
                MaximumWidth = panel.Width - 130,
                MaximumHeight = 58
            });
        }

        private static Rectangle MobileWaitingBackBounds(Rectangle panel) =>
            new(panel.Left + 20, panel.Top + 16, 76, 76);

        /// <summary>
        /// Android settings intentionally exposes only options that are meaningful on a
        /// phone. Window mode and desktop resolution controls are omitted instead of
        /// rendering no-op buttons that make the page look broken.
        /// </summary>
        private void BuildMobileSettingsScreen()
        {
            Rectangle panel = UITheme.PanelRect(_resourceManager.GraphicsDevice.Viewport);
            _uiControls.Add(new UIPanel(0, _resourceManager.Pixel) { Name = "panel" });
            _uiControls.Add(new UITitle(1, "Settings",
                new Vector2(panel.Center.X, panel.Top + 92),
                _resourceManager.Pixel, _resourceManager.TitleFont)
            { Name = "title" });

            int width = Math.Max(1, panel.Width - 112);
            _uiControls.Add(new UILabel(2, "Display: Portrait / Fullscreen",
                new Vector2(panel.Center.X, panel.Top + 232), _resourceManager.Font)
            {
                Name = "displayInfo",
                MaximumWidth = width,
                MinimumScale = 0.62f
            });
            _uiControls.Add(new UILabel(3, "Touch controls: Enabled",
                new Vector2(panel.Center.X, panel.Top + 300), _resourceManager.Font)
            {
                Name = "touchInfo",
                MaximumWidth = width,
                MinimumScale = 0.62f
            });
            _uiControls.Add(new UIIconButton(4,
                new Rectangle(panel.Left + 20, panel.Top + 16, 76, 76),
                UIIconButton.IconKind.Back, _resourceManager.Pixel, _resourceManager.Font)
            {
                Name = "back",
                FocusIndex = 0
            });

            if (_focusManager.CurrentInputMode == InputMode.Controller)
                _focusManager.FocusFirst();
        }

        /// <summary>
        /// Builds a JSON screen using a portrait-first flow. The JSON files remain
        /// the source of labels and control names, while geometry is calculated from
        /// the current viewport so adding a new screen does not require desktop
        /// coordinates or a second Android-specific JSON file.
        /// </summary>
        private void BuildMobileFromDescriptors(IReadOnlyList<UIScreenLoader.ControlDescriptor> descriptors)
        {
            Rectangle panel = UITheme.PanelRect(_resourceManager.GraphicsDevice.Viewport);
            _uiControls.Add(new UIPanel(0, _resourceManager.Pixel) { Name = "panel" });

            var title = descriptors.FirstOrDefault(d => d.Type == "title");
            if (title != null)
            {
                _uiControls.Add(new UITitle(title.Id, title.Text,
                    new Vector2(panel.Center.X, panel.Top + 96),
                    _resourceManager.Pixel, _resourceManager.TitleFont)
                { Name = title.Name });
            }

            var back = descriptors.FirstOrDefault(d =>
                d.Type == "button" && d.Name == "backToMain");
            if (back != null)
            {
                _uiControls.Add(new UIIconButton(back.Id,
                    new Rectangle(panel.Left + 20, panel.Top + 16, 76, 76),
                    UIIconButton.IconKind.Back, _resourceManager.Pixel, _resourceManager.Font)
                {
                    Name = back.Name
                });
            }

            var content = descriptors.Where(d => d.Type != "panel" && d.Type != "title" &&
                d != back).ToList();
            int interactiveCount = content.Count(d => d.Type == "button" || d.Type == "textbox");
            int labelCount = content.Count(d => d.Type == "label");
            int top = panel.Top + (title == null ? 100 : 220);
            int bottom = panel.Bottom - 56;
            int available = Math.Max(240, bottom - top);
            int rowGap = interactiveCount > 5 ? 18 : 28;
            int rowHeight = interactiveCount == 0
                ? 76
                : Math.Clamp((available - Math.Max(0, interactiveCount - 1) * rowGap - labelCount * 70) /
                    Math.Max(1, interactiveCount), 72, 108);
            int focusIndex = 0;

            if (back != null)
            {
                UIButton iconButton = _uiControls.OfType<UIButton>()
                    .First(button => button.Name == back.Name);
                iconButton.FocusIndex = focusIndex++;
            }

            foreach (var desc in content)
            {
                if (desc.Type == "label")
                {
                    var label = new UILabel(desc.Id, desc.Text,
                        new Vector2(panel.Center.X, top + 28), _resourceManager.Font)
                    {
                        Name = desc.Name,
                        MaximumWidth = panel.Width - 96,
                        MinimumScale = 0.56f
                    };
                    _uiControls.Add(label);
                    top += 70;
                    continue;
                }

                if (desc.Type == "button")
                {
                    int width = Math.Max(1, panel.Width - 112);
                    var button = new UIButton(desc.Id,
                        new Rectangle(panel.Left + 56, top, width, rowHeight),
                        desc.Text, _resourceManager.Pixel, _resourceManager.Font)
                    {
                        Name = desc.Name,
                        FocusIndex = focusIndex++
                    };
                    _uiControls.Add(button);
                    top += rowHeight + rowGap;
                    continue;
                }

                if (desc.Type == "textbox")
                {
                    int width = Math.Max(1, panel.Width - 112);
                    var input = new UIRoomCodeInput(desc.Id,
                        new Rectangle(panel.Left + 56, top, width, Math.Max(76, rowHeight - 8)),
                        desc.Text, _resourceManager.Pixel, _resourceManager.Font)
                    { Name = desc.Name };
                    _uiControls.Add(input);
                    top += rowHeight + rowGap;
                }
            }

            if (_focusManager.CurrentInputMode == InputMode.Controller) _focusManager.FocusFirst();
        }

#endif
    }
}
