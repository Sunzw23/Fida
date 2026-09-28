using System.Collections.Generic;
using System.Text;
using Fida.Campaign;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign
{
    /// <summary>战役专用页面构建器。页面布局依赖战役只读数据，不处理点击行为。</summary>
    public sealed class UICampaignScreenBuilder
    {
        private readonly ResourceManager _resources;
        private readonly List<UIControl> _controls;
        private readonly UIFocusManager _focus;

        public UICampaignScreenBuilder(ResourceManager resources, List<UIControl> controls, UIFocusManager focus)
        {
            _resources = resources;
            _controls = controls;
            _focus = focus;
        }

        public void BuildMap(bool online = false)
        {
#if FIDA_ANDROID
            var options = CampaignMapOptions.Mobile();
#else
            float scale = UIDesktopLayout.Scale(_resources.GraphicsDevice.Viewport);
            CampaignMapOptions options = new()
            {
                NodeWidth = 270f * scale,
                NodeHeight = 64f * scale,
                ContentPadding = 70f * scale,
                MinimumZoom = 0.55f,
                MaximumZoom = 2.2f,
                ScreenMargin = System.Math.Max(1, (int)System.Math.Round(36 * scale)),
                HeaderHeight = System.Math.Max(1, (int)System.Math.Round(24 * scale)),
                FooterHeight = System.Math.Max(1, (int)System.Math.Round(94 * scale))
            };
#endif
            _controls.Add(new UICampaignMap(0, _resources.GraphicsDevice, _resources.Pixel,
                _resources.AntialiasedLine, _resources.MapFont, _resources.TitleFont,
                CampaignCatalog.Nodes, CampaignRuntime.GetProgress(online), options)
            {
                Name = "campaign_map"
            });
        }

        public void BuildLoadout(CampaignNode node, bool online = false)
        {
#if FIDA_ANDROID
            var options = CampaignSkillWheelOptions.Mobile();
#else
            CampaignSkillWheelOptions options = null;
#endif
            _controls.Add(new UICampaignSkillWheel(0, _resources.GraphicsDevice, _resources.Pixel,
                _resources.Font, _resources.TitleFont, node, CampaignRuntime.GetProgress(online),
                GameSetup.SelectedLocalSkills, SkillFactory.SelectableSkills, options)
            {
                Name = "skill_wheel"
            });
        }

        public void BuildResult(bool won, string message, CampaignNode node)
        {
            _controls.Add(new UIPanel(0, _resources.Pixel) { Name = "panel" });
#if FIDA_ANDROID
            Rectangle panel = UITheme.PanelRect(_resources.GraphicsDevice.Viewport);
            float centerX = panel.Center.X;
            _controls.Add(new UITitle(1, won ? "Node Cleared" : "Defeat",
                new Vector2(centerX, panel.Top + 120), _resources.Pixel, _resources.TitleFont)
            { Name = "title" });
            int textWidth = panel.Width - 96;
            string mobileMessage = WrapText(_resources.Font, message, textWidth);
            _controls.Add(new UILabel(2, mobileMessage, new Vector2(centerX, panel.Top + 330), _resources.Font)
            { Name = "result", MaximumWidth = textWidth, MaximumHeight = 180 });
            if (won)
            {
                string mobileReward = WrapText(_resources.Font, $"Reward: {node.RewardText}", textWidth);
                _controls.Add(new UILabel(3, mobileReward, new Vector2(centerX, panel.Top + 430), _resources.Font)
                { Name = "reward", MaximumWidth = textWidth, MaximumHeight = 120 });
            }
            int buttonWidth = panel.Width - 112;
            _controls.Add(new UIButton(4, new Rectangle(panel.Left + 56, panel.Top + 620, buttonWidth, 92),
                "Adjust Loadout", _resources.Pixel, _resources.Font)
            { Name = "retry", FocusIndex = 0 });
            _controls.Add(new UIButton(5, new Rectangle(panel.Left + 56, panel.Top + 748,
                buttonWidth, 92), "Main Menu", _resources.Pixel, _resources.Font)
            { Name = "mainMenu", FocusIndex = 1 });
            FocusIfNeeded();
            return;
#else
            Viewport viewport = _resources.GraphicsDevice.Viewport;
            float scale = UIDesktopLayout.Scale(viewport);
            Rectangle panel = UITheme.PanelRect(viewport);
            Vector2 center = UIDesktopLayout.MapPoint(new Vector2(960, 430), viewport);
            _controls.Add(new UITitle(1, won ? "Node Cleared" : "Defeat",
                UIDesktopLayout.MapPoint(new Vector2(960, 310), viewport),
                _resources.Pixel, _resources.TitleFont) { Name = "title" });
            // UILabel applies the desktop scale after measuring in font units. Keep
            // the measure width inside the panel's unscaled design width as well.
            int textWidth = System.Math.Max(1, (int)System.Math.Round(panel.Width / scale) - 96);
            string desktopMessage = WrapText(_resources.Font, message, textWidth);
            _controls.Add(new UILabel(2, desktopMessage, center, _resources.Font)
            {
                Name = "result",
                MaximumWidth = textWidth,
                MaximumHeight = 150
            });
            if (won)
            {
                string reward = WrapText(_resources.Font, $"Reward: {node.RewardText}", textWidth);
                _controls.Add(new UILabel(3, reward,
                    UIDesktopLayout.MapPoint(new Vector2(960, 500), viewport), _resources.Font)
                {
                    Name = "reward",
                    MaximumWidth = textWidth,
                    MaximumHeight = 120
                });
            }
            _controls.Add(new UIButton(4, UIDesktopLayout.MapRectangle(
                    new Rectangle(520, 650, 380, 75), viewport), "Adjust Loadout",
                _resources.Pixel, _resources.Font) { Name = "retry", FocusIndex = 0 });
            _controls.Add(new UIButton(5, UIDesktopLayout.MapRectangle(
                    new Rectangle(1020, 650, 380, 75), viewport), "Main Menu",
                _resources.Pixel, _resources.Font) { Name = "mainMenu", FocusIndex = 1 });
            FocusIfNeeded();
#endif
        }

        public void BuildDuelResult(bool won, string message)
        {
            _controls.Add(new UIPanel(0, _resources.Pixel) { Name = "panel" });
#if FIDA_ANDROID
            Rectangle panel = UITheme.PanelRect(_resources.GraphicsDevice.Viewport);
            float centerX = panel.Center.X;
            _controls.Add(new UITitle(1, won ? "VICTORY" : "DEFEAT",
                new Vector2(centerX, panel.Top + 150), _resources.Pixel, _resources.TitleFont)
            { Name = "title" });
            _controls.Add(new UILabel(2, message, new Vector2(centerX, panel.Top + 360), _resources.Font)
            { Name = "result", MaximumWidth = panel.Width - 96, MaximumHeight = 100 });
            _controls.Add(new UIButton(3, new Rectangle(panel.Left + 56, panel.Bottom - 152,
                panel.Width - 112, 92), "Main Menu", _resources.Pixel, _resources.Font)
            { Name = "mainMenu", FocusIndex = 0 });
#else
            Viewport viewport = _resources.GraphicsDevice.Viewport;
            float scale = UIDesktopLayout.Scale(viewport);
            int textWidth = (int)System.Math.Round(1100 * scale);
            _controls.Add(new UITitle(1, won ? "VICTORY" : "DEFEAT",
                UIDesktopLayout.MapPoint(new Vector2(960, 340), viewport),
                _resources.Pixel, _resources.TitleFont) { Name = "title" });
            _controls.Add(new UILabel(2, message,
                UIDesktopLayout.MapPoint(new Vector2(960, 520), viewport), _resources.Font)
            {
                Name = "result",
                MaximumWidth = textWidth,
                MaximumHeight = 100,
                MinimumScale = 0.45f
            });
            _controls.Add(new UIButton(3, UIDesktopLayout.MapRectangle(
                    new Rectangle(770, 650, 380, 75), viewport), "Main Menu",
                _resources.Pixel, _resources.Font) { Name = "mainMenu", FocusIndex = 0 });
#endif
            FocusIfNeeded();
        }

        private void FocusIfNeeded()
        {
            if (_focus.CurrentInputMode == InputMode.Controller) _focus.FocusFirst();
        }

        private static string WrapText(SpriteFont font, string text, float maximumWidth)
        {
            if (string.IsNullOrWhiteSpace(text) || maximumWidth <= 0) return text ?? string.Empty;

            string[] words = text.Split(new[] { ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            var wrapped = new StringBuilder();
            string line = string.Empty;
            foreach (string word in words)
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && font.MeasureString(candidate).X > maximumWidth)
                {
                    if (wrapped.Length > 0) wrapped.Append('\n');
                    wrapped.Append(line);
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }

            if (line.Length > 0)
            {
                if (wrapped.Length > 0) wrapped.Append('\n');
                wrapped.Append(line);
            }
            return wrapped.ToString();
        }

    }
}
