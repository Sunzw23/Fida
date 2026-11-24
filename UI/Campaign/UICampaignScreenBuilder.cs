#nullable enable annotations
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
                // These are world units. The camera already scales them into the viewport.
                NodeWidth = 380f,
                NodeHeight = 180f,
                ContentPadding = 0f,
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
            CampaignSkillWheelOptions? options = null;
#endif
            _controls.Add(new UICampaignSkillWheel(0, _resources.GraphicsDevice, _resources.Pixel,
                _resources.Font, _resources.TitleFont, node, CampaignRuntime.GetProgress(online),
                GameSetup.SelectedLocalSkills, SkillFactory.SelectableSkills, options, _resources.CaptionFont, _resources.LabelFont)
            {
                Name = "skill_wheel"
            });
            if (CampaignLoadout.HasCompanionChoices(node, CampaignRuntime.GetProgress(online)))
                _controls.Add(new UICampaignPartyStrip(1, _resources.GraphicsDevice, _resources,
                    node, _resources.Font, _resources.CaptionFont,
                    CampaignCompanions.PartyLimitFor(node, CampaignRuntime.GetProgress(online)))
                {
                    Name = "party_strip"
                });
        }

        /// <summary>Full roster screen with art; the player builds the battle party here.</summary>
        public void BuildCompanionSelect(CampaignNode node, bool online = false)
        {
            var roster = CampaignCompanions.Available(node, CampaignRuntime.GetProgress(online));
            if (roster.Count == 0) return;
            _controls.Add(new UICampaignCompanionSelect(0, _resources.GraphicsDevice, _resources,
                node, roster, _resources.Font, _resources.TitleFont, _resources.CaptionFont)
            {
                Name = "companion_select"
            });
        }

        public void BuildResult(bool won, string message, CampaignNode node)
        {
            BuildPaperOutcome(won, message, node, false);
            AddEndingButtons(won, node);
            FocusIfNeeded();
        }

        private void BuildPaperOutcome(bool won, string message, CampaignNode? node, bool duel)
        {
            _controls.Add(new UIPaperOutcome(_resources, node, won, message, duel) { Name = "paper_outcome" });
            var layout = new UIEditorialLayout(_resources.GraphicsDevice.Viewport);
            if (!duel)
                _controls.Add(new UIEditorialButton(4, UIPaperOutcome.Action(layout, false, false), "Adjust loadout", _resources.Pixel, _resources.LabelFont)
                { Name = "retry", FocusIndex = 0, Primary = true, TextLineHeight = 52 });
            _controls.Add(new UIEditorialButton(5, UIPaperOutcome.Action(layout, true, duel),
                won && !duel ? "Continue journey" : "Main menu", _resources.Pixel, _resources.LabelFont)
                  { Name = "mainMenu", FocusIndex = 1, Primary = duel, Active = won && !duel, TrailingChevron = won && !duel, TextLineHeight = 52 });
        }

        private void AddEndingButtons(bool won, CampaignNode node)
        {
            if (!won || node.Endings.Count == 0) return;
            var positions = new List<Rectangle>();
            foreach (var control in _controls)
                if (control is UIButton && (control.Name is "retry" or "mainMenu") &&
                    control.GetScreenBounds(_resources.GraphicsDevice.Viewport) is Rectangle bounds)
                    positions.Add(bounds);
            for (int i = 0; i < node.Endings.Count; i++)
            {
                var viewport = _resources.GraphicsDevice.Viewport;
                Rectangle bounds = positions[i % positions.Count];
                if (viewport.Width < 700 || viewport.Height > viewport.Width)
                {
                    Rectangle panel = UITheme.PanelRect(viewport);
                    int height = System.Math.Clamp(panel.Height / 8, 44, 76);
                    int top = panel.Bottom - node.Endings.Count * (height + 12) - 12;
                    bounds = new Rectangle(panel.Left + 16, top + i * (height + 12), panel.Width - 32, height);
                }
                _controls.Add(new UIButton(10 + i, bounds, node.Endings[i].Label,
                    _resources.Pixel, _resources.Font)
                { Name = "ending_" + node.Endings[i].Id, FocusIndex = i, IsVisible = false, MinimumTextScale = 0.62f });
            }
        }

        public void BuildDuelResult(bool won, string message)
        {
            BuildPaperOutcome(won, message, null, true);
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
