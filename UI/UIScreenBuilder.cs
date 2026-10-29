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
                BuildFromScreen("ConnectedMainMenu");
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
                { "resolution", DisplaySettingsManager.ResolutionLabel },
                { "motion", UIMotion.Reduced ? "Reduced" : "Full" }
            });
#endif
        }

        /// <summary>Build a compact, page-based field guide that scales to phone and desktop.</summary>
        public void BuildRulesScreen(int page = 0)
        {
            Clear();
            page = (page % UIManager.RulesPageCount + UIManager.RulesPageCount) %
                UIManager.RulesPageCount;
            Rectangle panel = UITheme.PanelRect(_resourceManager.GraphicsDevice.Viewport);
            float layoutScale = UIDesktopLayout.LayoutScale(_resourceManager.GraphicsDevice.Viewport);
            _uiControls.Add(new UIPanel(0, _resourceManager.Pixel) { Name = "panel" });

            _uiControls.Add(new UIIconButton(1, UIIconButton.StandardBounds(
                _resourceManager.GraphicsDevice.Viewport), UIIconButton.IconKind.Back,
                _resourceManager.Pixel, _resourceManager.Font)
            {
                Name = "back",
                FocusIndex = 0
            });

            AddRulesLabel(2, "FIELD GUIDE", new Vector2(panel.Center.X, panel.Top + 78 * layoutScale),
                _resourceManager.TitleFont, panel.Width - 150, 48, UITheme.Ink);

            string[] pageTitles = ["MAKE A MOVE", "NUMBER EFFECTS", "SKILL TIMING",
                "SKILL SET  /  01", "SKILL SET  /  02"];
            string[] pageKickers = ["YOUR TURN", "COMBAT", "TRIGGERS", "LOADOUT", "LOADOUT"];
            AddRulesLabel(3, pageTitles[page], new Vector2(panel.Center.X, panel.Top + 142 * layoutScale),
                _resourceManager.TitleFont, panel.Width - 100, 54, UITheme.AccentBlue);
            AddRulesLabel(4, pageKickers[page], new Vector2(panel.Center.X, panel.Top + 188 * layoutScale),
                _resourceManager.Font, panel.Width - 120, 32, UITheme.InkMuted);

            int contentTop = panel.Top + (int)(226 * layoutScale);
            int footerTop = panel.Bottom - (int)(142 * layoutScale);
            int contentHeight = Math.Max(120, footerTop - contentTop - (int)(10 * layoutScale));
            if (page == 0)
                BuildTurnRules(panel, contentTop, contentHeight);
            else if (page == 1)
                BuildNumberRules(panel, contentTop, contentHeight);
            else if (page == 2)
                BuildSkillTimingRules(panel, contentTop, contentHeight);
            else
                BuildSkillSet(panel, contentTop, contentHeight, page - 3);

            int buttonSize = Math.Clamp(panel.Width / 10, 64, 78);
            int buttonY = panel.Bottom - buttonSize - (int)(48 * layoutScale);
            _uiControls.Add(new UIIconButton(5,
                new Rectangle(panel.Center.X - buttonSize - 12, buttonY, buttonSize, buttonSize),
                UIIconButton.IconKind.PreviousPage, _resourceManager.Pixel, _resourceManager.Font)
            {
                Name = "prevPage",
                FocusIndex = 1
            });
            _uiControls.Add(new UIIconButton(6,
                new Rectangle(panel.Center.X + 12, buttonY, buttonSize, buttonSize),
                UIIconButton.IconKind.NextPage, _resourceManager.Pixel, _resourceManager.Font)
            {
                Name = "nextPage",
                FocusIndex = 2
            });
            AddRulesLabel(7, $"{page + 1:00} / {UIManager.RulesPageCount:00}", new Vector2(panel.Center.X,
                    panel.Bottom - 28 * layoutScale),
                _resourceManager.Font, panel.Width - 160, 28, UITheme.InkMuted);
            _uiControls.Add(new UIBoxLabel(8,
                new Rectangle(panel.Right - (int)(112 * layoutScale),
                    panel.Top + (int)(24 * layoutScale), (int)(82 * layoutScale),
                    (int)(42 * layoutScale)),
                "TAB", _resourceManager.Pixel, _resourceManager.Font)
            {
                Name = "tabHint",
                BackColor = UITheme.PanelDark,
                ForeColor = UITheme.InkMuted,
                BorderColor = UITheme.AccentBlue,
                BorderThickness = 1,
                AutoFitText = true,
                MinimumTextScale = 0.7f,
                MaximumTextScale = 0.9f,
                TextPadding = 5
            });

            if (_focusManager.CurrentInputMode == InputMode.Controller)
                _focusManager.FocusFirst();
        }

        private void BuildTurnRules(Rectangle panel, int top, int availableHeight)
        {
            float scale = UIDesktopLayout.LayoutScale(_resourceManager.GraphicsDevice.Viewport);
            int side = Math.Max(28, (int)(44 * scale));
            int width = panel.Width - side * 2;
            int gap = Math.Max(10, (int)(14 * scale));
            int equationHeight = Math.Max(34, (int)(72 * scale));
            int equationGap = Math.Max(5, (int)(16 * scale));
            int cardHeight = Math.Min((int)(122 * scale),
                (availableHeight - equationHeight - equationGap - gap * 2) / 3);
            cardHeight = Math.Max(Math.Max(38, (int)(65 * scale)), cardHeight);
            var rows = new (string tag, string title, string detail)[]
            {
                ("01", "Choose", "Pick one of your two numbers."),
                ("02", "Add", "Choose another number — yours or any player’s."),
                ("03", "Resolve", "Add the values; keep the last digit. Then resolve effects.")
            };
            Color[] accents = [UITheme.AccentBlue, UITheme.AccentViolet, UITheme.AccentGreen];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                AddRuleCard(10 + i,
                    new Rectangle(panel.Left + side, top + i * (cardHeight + gap), width, cardHeight),
                    row.tag, row.title, row.detail, accents[i]);
            }

            int equationY = top + rows.Length * (cardHeight + gap) - gap + equationGap;
            var equation = new UIBoxLabel(20,
                new Rectangle(panel.Left + side, equationY, width, equationHeight),
                "4  +  7  =  11  ->  1", _resourceManager.Pixel, _resourceManager.TitleFont)
            {
                Name = "example",
                BackColor = UITheme.PanelDark,
                ForeColor = UITheme.AccentBlue,
                BorderColor = UITheme.AccentBlue,
                BorderThickness = 1,
                AccentBar = true,
                AutoFitText = true,
                MaximumTextScale = 0.9f,
                TextPadding = 14
            };
            _uiControls.Add(equation);
        }

        private void BuildNumberRules(Rectangle panel, int top, int availableHeight)
        {
            float scale = UIDesktopLayout.LayoutScale(_resourceManager.GraphicsDevice.Viewport);
            int side = Math.Max(24, (int)(38 * scale));
            int gapX = Math.Max(10, (int)(16 * scale));
            int gapY = Math.Max(10, (int)(14 * scale));
            int width = (panel.Width - side * 2 - gapX) / 2;
            int height = Math.Min((int)(104 * scale), (availableHeight - gapY * 3) / 4);
            height = Math.Max(Math.Max(34, (int)(58 * scale)), height);
            var rows = new (string title, string detail)[]
            {
                ("7  /  8", "Attack 1"),
                ("1  +  6", "Attack 2"),
                ("5  +  5", "Attack 3"),
                ("6  +  6", "Attack 5"),
                ("9", "Heal 1"),
                ("0", "Block one hit"),
                ("0  +  0", "Heal 10")
            };
            for (int i = 0; i < rows.Length; i++)
            {
                int column = i % 2;
                int row = i / 2;
                var item = rows[i];
                AddRuleCard(30 + i,
                    new Rectangle(panel.Left + side + column * (width + gapX),
                        top + row * (height + gapY), width, height),
                    "", item.title, item.detail,
                    i % 2 == 0 ? UITheme.AccentBlue : UITheme.AccentGreen);
            }
        }

        private void BuildSkillTimingRules(Rectangle panel, int top, int availableHeight)
        {
            float scale = UIDesktopLayout.LayoutScale(_resourceManager.GraphicsDevice.Viewport);
            int side = Math.Max(28, (int)(44 * scale));
            int gap = Math.Max(8, (int)(10 * scale));
            int height = Math.Min((int)(98 * scale), (availableHeight - gap * 4) / 5);
            height = Math.Max(Math.Max(34, (int)(58 * scale)), height);
            var rows = new (string tag, string title, string detail)[]
            {
                ("01", "Order matters", "Skills sharing a trigger resolve top to bottom in your loadout."),
                ("02", "Once per turn", "Limited skills refresh when a new turn begins."),
                ("03", "Before damage", "Defensive effects resolve before health is reduced."),
                ("04", "After damage", "Damage reactions resolve before post-attack effects."),
                ("05", "Defeat", "A player is out when health reaches zero.")
            };
            Color[] accents = [UITheme.AccentBlue, UITheme.AccentViolet,
                UITheme.AccentGreen, UITheme.AccentBlue, UITheme.InkMuted];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                AddRuleCard(50 + i,
                    new Rectangle(panel.Left + side, top + i * (height + gap),
                        panel.Width - side * 2, height),
                    row.tag, row.title, row.detail, accents[i]);
            }
        }

        private void BuildSkillSet(Rectangle panel, int top, int availableHeight, int setIndex)
        {
            float scale = UIDesktopLayout.LayoutScale(_resourceManager.GraphicsDevice.Viewport);
            int side = Math.Max(24, (int)(38 * scale));
            int gapX = Math.Max(8, (int)(12 * scale));
            int gapY = Math.Max(6, (int)(9 * scale));
            int width = (panel.Width - side * 2 - gapX) / 2;
            int height = Math.Min((int)(96 * scale), (availableHeight - gapY * 4) / 5);
            height = Math.Max(Math.Max(34, (int)(56 * scale)), height);

            (string name, string detail)[][] sets =
            [
                [
                    ("Attack Up", "+1 attack damage"),
                    ("Double Attack", "Double attack damage"),
                    ("Defense Up", "Reduce incoming damage by 1"),
                    ("Double Defense", "Halve incoming damage"),
                    ("Double Health", "Start with double health"),
                    ("Triple Health", "Start with triple health"),
                    ("Blood", "Heal damage dealt"),
                    ("Stone", "Block later hits this turn"),
                    ("Demon", "Return half the damage"),
                    ("Same", "Apply one result to both numbers")
                ],
                [
                    ("God", "After damage: set one number"),
                    ("Immortal", "After attack: set one number"),
                    ("Seng", "After damage: perform one add"),
                    ("Dao", "After attack: perform one add"),
                    ("Emperor", "Turn start: set one number"),
                    ("Super God", "After damage: set both numbers"),
                    ("Super Immortal", "After attack: set both numbers"),
                    ("Super Emperor", "Turn start: set both numbers"),
                    ("Transform", "Choose attack damage 1-5"),
                    ("Dragon", "Attack damage equals number sum")
                ]
            ];

            var items = sets[setIndex];
            for (int i = 0; i < items.Length; i++)
            {
                int column = i / 5;
                int row = i % 5;
                var item = items[i];
                AddRuleCard(70 + setIndex * 10 + i,
                    new Rectangle(panel.Left + side + column * (width + gapX),
                        top + row * (height + gapY), width, height),
                    $"{i + 1:00}", item.name, item.detail,
                    (i + setIndex) % 2 == 0 ? UITheme.AccentBlue : UITheme.AccentGreen);
            }
        }

        private void AddRuleCard(int id, Rectangle bounds, string tag, string heading,
            string detail, Color accent) =>
            _uiControls.Add(new UIRuleCard(id, bounds, tag, heading, detail,
                _resourceManager.Pixel, _resourceManager.Font, _resourceManager.TitleFont, accent));

        private void AddRulesLabel(int id, string text, Vector2 position, SpriteFont font,
            float maxWidth, float maxHeight, Color color)
        {
            var label = new UILabel(id, text, position, font)
            {
                Name = $"ruleLabel{id}",
                MaximumWidth = maxWidth,
                MaximumHeight = maxHeight,
                MinimumScale = 0.65f
            };
            label.SetColor(color);
            _uiControls.Add(label);
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
            string[] statusLines = (statusText ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var replacements = new Dictionary<string, string>
            {
                { "status", statusLines.Length > 0 ? statusLines[0].ToUpperInvariant() : "WAITING FOR OPPONENT" },
                { "address", statusLines.Length > 1 ? statusLines[1].ToUpperInvariant() : string.Empty },
                { "roomCode", string.IsNullOrEmpty(roomCode) ? "····" : roomCode }
            };
            BuildFromScreen("HostingScreen", replacements);
            Rectangle panel = UITheme.PanelRect(_resourceManager.GraphicsDevice.Viewport);
            foreach (var (name, height, color) in new[]
            {
                ("roomCodeCaption", 34f, UITheme.InkMuted),
                ("roomCodeLabel", 78f, UITheme.Ink),
                ("statusLabel", 54f, UITheme.Ink),
                ("addressLabel", 38f, UITheme.InkMuted)
            })
            {
                var label = _uiControls.OfType<UILabel>().FirstOrDefault(control => control.Name == name);
                if (label == null) continue;
                label.MaximumWidth = panel.Width - 120;
                label.MaximumHeight = height;
                label.SetColor(color);
            }
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
            BuildFromScreen("OnlineLobby");
            Rectangle panel = UITheme.PanelRect(_resourceManager.GraphicsDevice.Viewport);
            _uiControls.Add(new UILabel(8, "BOTH PLAYERS CONFIRM DUEL",
                new Vector2(960, 330), _resourceManager.Font)
            {
                Name = "lobbyStatus",
                MaximumWidth = panel.Width - 96,
                MinimumScale = 0.72f
            });
            if (_focusManager.CurrentInputMode == InputMode.Controller)
                _focusManager.FocusFirst();
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
                node, progress, new HashSet<SkillType>(), SkillFactory.SelectableSkills, options)
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
                _resourceManager.Pixel, _resourceManager.Font, node);
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
