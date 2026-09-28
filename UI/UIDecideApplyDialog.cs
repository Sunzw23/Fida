using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>A modal, input-agnostic prompt for optional skill effects.</summary>
    public sealed class UIDecideApplyDialog : UIControl
    {
        public const int ApplyButtonId = 410;
        public const int PassButtonId = 411;

        private readonly GraphicsDevice _graphicsDevice;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        private string _prompt = "";

        public UIAnimatedButton ApplyButton { get; }
        public UIAnimatedButton PassButton { get; }

        public override bool IsVisible
        {
            get => base.IsVisible;
            set
            {
                base.IsVisible = value;
                ApplyButton.IsVisible = value;
                PassButton.IsVisible = value;
            }
        }

        public UIDecideApplyDialog(int id, GraphicsDevice graphicsDevice,
            Texture2D pixel, SpriteFont font) : base(id)
        {
            _graphicsDevice = graphicsDevice;
            _pixel = pixel;
            _font = font;
            Rectangle panel = GetPanelBounds();
            int buttonY = panel.Bottom - UIConfig.ScaleDesktopMetric(94);
#if FIDA_ANDROID
            int buttonWidth = System.Math.Min(360, System.Math.Max(180, (panel.Width - 132) / 2));
            ApplyButton = new UIAnimatedButton(ApplyButtonId,
                new Rectangle(panel.Left + 44, buttonY, buttonWidth, 72), "Apply", pixel, font)
            {
                FocusIndex = 410
            };
            PassButton = new UIAnimatedButton(PassButtonId,
                new Rectangle(panel.Right - 44 - buttonWidth, buttonY, buttonWidth, 72), "Pass", pixel, font)
            {
                FocusIndex = 411
            };
#else
            ApplyButton = new UIAnimatedButton(ApplyButtonId,
                new Rectangle(panel.X + UIConfig.ScaleDesktopMetric(78), buttonY,
                    UIConfig.ScaleDesktopMetric(250), UIConfig.ScaleDesktopMetric(66)), "Apply", pixel, font)
            {
                FocusIndex = 410
            };
            PassButton = new UIAnimatedButton(PassButtonId,
                new Rectangle(panel.Right - UIConfig.ScaleDesktopMetric(328), buttonY,
                    UIConfig.ScaleDesktopMetric(250), UIConfig.ScaleDesktopMetric(66)), "Pass", pixel, font)
            {
                FocusIndex = 411
            };
#endif
            IsVisible = false;
        }

        public void Show(string prompt)
        {
            _prompt = prompt ?? "";
            IsVisible = true;
        }

        public void Hide() => IsVisible = false;

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            if (!IsVisible) return;
            var viewport = _graphicsDevice.Viewport;
            Rectangle panel = GetPanelBounds();
            UIUtils.DrawModalShell(sb, _pixel, panel, viewport);
            float uiScale = UIDesktopLayout.LayoutScale(viewport);

            DrawCentered(sb, "OPTIONAL SKILL", panel.Center.X,
                panel.Y + UIConfig.ScaleDesktopMetric(50), UITheme.Ink, 0.76f * uiScale,
                panel.Width - UIConfig.ScaleDesktopMetric(80));
            sb.Draw(_pixel, new Rectangle(panel.X + UIConfig.ScaleDesktopMetric(80),
                panel.Y + UIConfig.ScaleDesktopMetric(86),
                panel.Width - UIConfig.ScaleDesktopMetric(160), UIConfig.ScaleDesktopMetric(2)), UITheme.Gold);

            const string separator = " triggered skill: ";
            int split = _prompt.IndexOf(separator, System.StringComparison.OrdinalIgnoreCase);
            if (split >= 0)
            {
                string actor = _prompt.Substring(0, split);
                string skill = _prompt.Substring(split + separator.Length).TrimEnd('.');
                DrawCentered(sb, actor, panel.Center.X,
                    panel.Y + UIConfig.ScaleDesktopMetric(126), UITheme.InkMuted,
                    0.56f * uiScale, panel.Width - UIConfig.ScaleDesktopMetric(100));
                DrawCentered(sb, skill, panel.Center.X,
                    panel.Y + UIConfig.ScaleDesktopMetric(178), UITheme.Ink,
                    0.88f * uiScale, panel.Width - UIConfig.ScaleDesktopMetric(100));
            }
            else
            {
                DrawCentered(sb, _prompt, panel.Center.X,
                    panel.Y + UIConfig.ScaleDesktopMetric(152), UITheme.Ink,
                    0.64f * uiScale, panel.Width - UIConfig.ScaleDesktopMetric(100));
            }
        }

        private Rectangle GetPanelBounds()
        {
            var viewport = _graphicsDevice.Viewport;
#if FIDA_ANDROID
            int width = System.Math.Max(1, viewport.Width - 48);
            int height = System.Math.Min(460, viewport.Height - 48);
            return new Rectangle((viewport.Width - width) / 2,
                (viewport.Height - height) / 2, width, height);
#else
            int width = System.Math.Min(UIConfig.ScaleDesktopMetric(780), viewport.Width - 48);
            int height = System.Math.Min(UIConfig.ScaleDesktopMetric(340), viewport.Height - 48);
            return new Rectangle((viewport.Width - width) / 2,
                (viewport.Height - height) / 2, width, height);
#endif
        }

        private void DrawCentered(SpriteBatch sb, string text, float centerX, float centerY,
            Color color, float preferredScale, float maxWidth)
        {
            UIUtils.DrawCenteredFit(sb, _font, text, new Vector2(centerX, centerY),
                maxWidth, preferredScale, color);
        }
    }
}
