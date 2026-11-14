// UIScreenBuilder.cs 文件
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;
using Fida.Campaign;
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
    public partial class UIScreenBuilder
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
            BuildEditorialMainMenu(connected);
        }

        /// <summary>构建暂停菜单</summary>
        public void BuildPauseMenu()
        {
            Clear();
            _uiControls.Add(new UIPaperPause(_resourceManager) { Name = "pause_art" });
            var layout = EditorialLayout;
            string[] names = { "resume", "settings", "rules", "backToMenu" };
            string[] labels = { "Resume", "Settings", "Rules", "Main menu" };
            for(int i=0;i<names.Length;i++)
                EditorialButton(names[i],labels[i],UIPaperPause.Button(layout,i),primary:i==0).TextLineHeight = 58;
            EditorialFocus();
        }

        /// <summary>构建设置界面</summary>
        public void BuildSettingsScreen()
        {
            Clear();
            BuildEditorialSettings();
        }

        /// <summary>Rules book: three reference pages, the skill index, then one page per discovered skill.</summary>
        public void BuildRulesScreen(int page = 0, int indexPage = 0)
        {
            Clear();
            BuildRulesPage(page, indexPage);
        }

        /// <summary>构建在线菜单</summary>
        public void BuildOnlineMenu(bool connected = false)
        {
            Clear();
            BuildReferenceOnline(connected);
        }

        public void BuildHostingScreen(string statusText, string roomCode = "")
        {
            Clear();
            BuildReferenceWaiting(true, statusText, roomCode);
        }

        public void BuildConnectingScreen(string statusText)
        {
            Clear();
            BuildReferenceWaiting(false, statusText);
        }

        /// <summary>构建在线大厅</summary>
        public void BuildOnlineLobby(bool isHost)
        {
            Clear();
            BuildEditorialLobby(isHost);
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
            CampaignSkillWheelOptions? options = null;
#endif
            _uiControls.Add(new UICampaignSkillWheel(0, _resourceManager.GraphicsDevice,
                _resourceManager.Pixel, _resourceManager.Font, _resourceManager.TitleFont,
                node, progress, new HashSet<SkillType>(), SkillFactory.SelectableSkills, options, _resourceManager.CaptionFont, _resourceManager.LabelFont)
            {
                Name = "duel_draft_wheel",
                HeaderText = "BAN"
            });
        }

        /// <summary>构建关卡选择界面</summary>
        public void BuildLevelSelect(bool online = false)
        {
            Clear();
            _campaignScreens.BuildMap(online);
        }

        /// <summary>构建技能选择界面</summary>
        public void BuildSkillSelect(bool online = false)
        {
            Clear();
            _campaignScreens.BuildLoadout(GameSetup.SelectedNode, online);
            var wheel = _uiControls.OfType<UICampaignSkillWheel>().First();
            bool canChoose = CampaignLoadout.HasSelectableSkills(CampaignRuntime.GetProgress(online), GameSetup.SelectedNode);
            wheel.IsVisible = canChoose;
            wheel.InputBlocked = !canChoose;
            if (!canChoose && online)
                _uiControls.Add(new UILabel(-1001, "Waiting for ally...",
                    new Vector2(_resourceManager.GraphicsDevice.Viewport.Width / 2f,
                        _resourceManager.GraphicsDevice.Viewport.Height / 2f), _resourceManager.Font));
            if (GameSetup.SelectedNode.BeforeStory.Count > 0)
            {
                var dialogue = AddTutorialDialogue(GameSetup.SelectedNode);
                dialogue.PresentPages(GameSetup.SelectedNode.BeforeStory, CampaignRuntime.GetProgress(online));
                wheel.InputBlocked = !canChoose || dialogue.IsVisible;
                dialogue.Finished += () => wheel.InputBlocked = !canChoose;
            }
        }

        public UITutorialDialogue AddTutorialDialogue(CampaignNode node)
        {
            var dialogue = new UITutorialDialogue(-1000, _resourceManager.GraphicsDevice,
                _resourceManager.Pixel, _resourceManager.Font, node)
                { PortraitAtlas = _resourceManager.GetTexture("cold_roster"), MaraPortrait = _resourceManager.GetTexture("dialogue_mara"), SpeakerFont = _resourceManager.LabelFont };
            _uiControls.Add(dialogue);
            if (_focusManager.CurrentInputMode == InputMode.Controller)
                _focusManager.FocusFirst();
            return dialogue;
        }

        public void BuildCampaignResult(bool won, string message) =>
            _campaignScreens.BuildResult(won, message, GameSetup.SelectedNode);

        public void BuildDuelResult(bool won, string message) =>
            _campaignScreens.BuildDuelResult(won, message);

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
                UIControl control = desc.Type == "button" && IsNavigationBack(desc.Name)
                    ? new UIIconButton(desc.Id, DesktopBackIconBounds(), UIIconButton.IconKind.Back,
                        _resourceManager.Pixel, _resourceManager.Font)
                    : desc.Type switch
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

        private static bool IsNavigationBack(string name) =>
            name is "back" or "backToMain";

        private Rectangle DesktopBackIconBounds() => UIIconButton.StandardBounds(
            _resourceManager.GraphicsDevice.Viewport);

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
            new(panel.Left, panel.Top - 4, 64, 64);

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
            _uiControls.Add(new UIButton(5,
                new Rectangle(panel.Left + 56, panel.Top + 370, width, 68),
                UIMotion.Reduced ? "Motion: Reduced" : "Motion: Full",
                _resourceManager.Pixel, _resourceManager.Font)
            { Name = "motion", FocusIndex = 1 });
            _uiControls.Add(new UIIconButton(4,
                MobileWaitingBackBounds(panel),
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
                d.Type == "button" && IsNavigationBack(d.Name));
            if (back != null)
            {
                Rectangle safe = UIMobileSafeArea.Get(_resourceManager.GraphicsDevice.Viewport);
                _uiControls.Add(new UIIconButton(back.Id,
                    new Rectangle(safe.Left + 8, safe.Top + 4, 64, 64),
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
