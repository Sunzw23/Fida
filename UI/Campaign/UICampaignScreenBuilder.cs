using System.Collections.Generic;
using Fida.Campaign;
using Fida.Logic;
using Microsoft.Xna.Framework;

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

        public void BuildMap()
        {
#if FIDA_ANDROID
            var options = CampaignMapOptions.Mobile();
#else
            CampaignMapOptions options = null;
#endif
            _controls.Add(new UICampaignMap(0, _resources.GraphicsDevice, _resources.Pixel,
                _resources.AntialiasedLine, _resources.MapFont, _resources.TitleFont,
                CampaignCatalog.Nodes, CampaignRuntime.Progress, options)
            {
                Name = "campaign_map"
            });
        }

        public void BuildLoadout(CampaignNode node)
        {
#if FIDA_ANDROID
            var options = CampaignSkillWheelOptions.Mobile();
#else
            CampaignSkillWheelOptions options = null;
#endif
            _controls.Add(new UICampaignSkillWheel(0, _resources.GraphicsDevice, _resources.Pixel,
                _resources.Font, _resources.TitleFont, node, CampaignRuntime.Progress,
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
            _controls.Add(new UILabel(2, message, new Vector2(centerX, panel.Top + 330), _resources.Font)
            { Name = "result", MaximumWidth = panel.Width - 96, MinimumScale = 0.58f });
            string mobileReward = won ? $"Reward: {node.RewardText}" : "Nothing was lost. Adjust your loadout and try again.";
            _controls.Add(new UILabel(3, mobileReward, new Vector2(centerX, panel.Top + 430), _resources.Font)
            { Name = "reward", MaximumWidth = panel.Width - 96, MinimumScale = 0.54f });
            int buttonWidth = panel.Width - 112;
            _controls.Add(new UIButton(4, new Rectangle(panel.Left + 56, panel.Top + 620, buttonWidth, 92),
                "Adjust Loadout", _resources.Pixel, _resources.Font)
            { Name = "retry", FocusIndex = 0 });
            _controls.Add(new UIButton(5, new Rectangle(panel.Left + 56, panel.Top + 748, buttonWidth, 92),
                "Campaign Map", _resources.Pixel, _resources.Font)
            { Name = "map", FocusIndex = 1 });
            FocusIfNeeded();
            return;
#else
            _controls.Add(new UITitle(1, won ? "Node Cleared" : "Defeat", new Vector2(960, 310),
                _resources.Pixel, _resources.TitleFont) { Name = "title" });
            _controls.Add(new UILabel(2, message, new Vector2(960, 430), _resources.Font) { Name = "result" });
            string reward = won ? $"Reward: {node.RewardText}" : "Nothing was lost. Adjust your loadout and try again.";
            _controls.Add(new UILabel(3, reward, new Vector2(960, 500), _resources.Font) { Name = "reward" });
            _controls.Add(new UIButton(4, new Rectangle(520, 650, 380, 75), "Adjust Loadout",
                _resources.Pixel, _resources.Font) { Name = "retry", FocusIndex = 0 });
            _controls.Add(new UIButton(5, new Rectangle(1020, 650, 380, 75), "Campaign Map",
                _resources.Pixel, _resources.Font) { Name = "map", FocusIndex = 1 });
            FocusIfNeeded();
#endif
        }

        private void FocusIfNeeded()
        {
            if (_focus.CurrentInputMode == InputMode.Controller) _focus.FocusFirst();
        }
    }
}
