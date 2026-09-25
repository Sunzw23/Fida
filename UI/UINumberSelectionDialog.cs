using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Fida.UI
{
    /// <summary>Modal presentation for choosing a resulting value from 0 through 9.</summary>
    public sealed class UINumberSelectionDialog : UIControl
    {
        public const int FirstButtonId = 200;

        private readonly GraphicsDevice _graphicsDevice;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        private string _prompt = "";

        public UIAnimatedButton[] NumberButtons { get; }

        public override bool IsVisible
        {
            get => base.IsVisible;
            set
            {
                base.IsVisible = value;
                foreach (var button in NumberButtons)
                    button.IsVisible = value;
            }
        }

        public UINumberSelectionDialog(int id, GraphicsDevice graphicsDevice,
            Texture2D pixel, SpriteFont font) : base(id)
        {
            _graphicsDevice = graphicsDevice;
            _pixel = pixel;
            _font = font;
            NumberButtons = new UIAnimatedButton[10];

            Rectangle panel = GetPanelBounds();
            const int columns = 5;
            int gap =
#if FIDA_ANDROID
                16;
#else
                UIConfig.ScaleDesktopMetric(14);
#endif
            int buttonWidth = System.Math.Min(
#if FIDA_ANDROID
                144,
#else
                UIConfig.ScaleDesktopMetric(112),
#endif
                (panel.Width - 128 - gap * 4) / columns);
            int buttonHeight =
#if FIDA_ANDROID
                82;
#else
                UIConfig.ScaleDesktopMetric(68);
#endif
            int gridWidth = columns * buttonWidth + gap * (columns - 1);
            int startX = panel.Center.X - gridWidth / 2;
            int startY = panel.Y + UIConfig.ScaleDesktopMetric(174);

            for (int number = 0; number < NumberButtons.Length; number++)
            {
                int column = number % columns;
                int row = number / columns;
                NumberButtons[number] = new UIAnimatedButton(FirstButtonId + number,
                    new Rectangle(startX + column * (buttonWidth + gap),
                        startY + row * (buttonHeight + gap), buttonWidth, buttonHeight),
                    number.ToString(), pixel, font)
                {
                    FocusIndex = 300 + number
                };
            }
            IsVisible = false;
        }

        public void Show(string prompt, IReadOnlyCollection<int> validNumbers)
        {
            _prompt = prompt ?? "";
            const string skillSeparator = " activated skill: ";
            int skillIndex = _prompt.IndexOf(skillSeparator, System.StringComparison.OrdinalIgnoreCase);
            if (skillIndex >= 0)
                _prompt = _prompt.Substring(skillIndex + skillSeparator.Length).TrimEnd('.');
            IsVisible = true;
            int[] choices = validNumbers == null || validNumbers.Count == 0
                ? Enumerable.Range(0, 10).ToArray()
                : validNumbers.Where(n => n >= 0 && n <= 9).Distinct().OrderBy(n => n).ToArray();
            int columns = System.Math.Min(5, System.Math.Max(1, choices.Length));
            int gap = UIConfig.ScaleDesktopMetric(14);
            int buttonWidth = NumberButtons[0].LayoutBounds.Width;
            int buttonHeight = NumberButtons[0].LayoutBounds.Height;
            Rectangle panel = GetPanelBounds();
            int rows = (choices.Length + columns - 1) / columns;
            int startY = panel.Center.Y - (rows * buttonHeight + (rows - 1) * gap) / 2 +
                UIConfig.ScaleDesktopMetric(25);
            foreach (var button in NumberButtons) button.IsVisible = false;
            for (int i = 0; i < choices.Length; i++)
            {
                int row = i / columns;
                int inRow = System.Math.Min(columns, choices.Length - row * columns);
                int rowWidth = inRow * buttonWidth + (inRow - 1) * gap;
                var button = NumberButtons[choices[i]];
                button.LayoutBounds = new Rectangle(panel.Center.X - rowWidth / 2 +
                    (i % columns) * (buttonWidth + gap), startY + row * (buttonHeight + gap),
                    buttonWidth, buttonHeight);
                button.IsVisible = true;
            }
        }

        public void Hide() => IsVisible = false;

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            if (!IsVisible) return;
            var viewport = _graphicsDevice.Viewport;
            sb.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height),
                UITheme.Vignette * 0.78f);

            Rectangle panel = GetPanelBounds();
 #if FIDA_ANDROID
            const float uiScale = 1f;
#else
            float uiScale = UIDesktopLayout.Scale(viewport);
#endif
            sb.Draw(_pixel, panel, UITheme.PanelLight);
            UIUtils.DrawEngravedFrame(sb, panel, _pixel, UITheme.GoldBright,
                UITheme.Ink, UIConfig.ScaleDesktopMetric(4), UIConfig.ScaleDesktopMetric(10),
                UIConfig.ScaleDesktopMetric(28));

            DrawCentered(sb, "CHOOSE A NUMBER", panel.Center.X,
                panel.Y + UIConfig.ScaleDesktopMetric(50), UITheme.Ink, 0.76f * uiScale,
                panel.Width - UIConfig.ScaleDesktopMetric(80));
            sb.Draw(_pixel, new Rectangle(panel.X + UIConfig.ScaleDesktopMetric(80),
                panel.Y + UIConfig.ScaleDesktopMetric(86),
                panel.Width - UIConfig.ScaleDesktopMetric(160), UIConfig.ScaleDesktopMetric(2)), UITheme.Gold);
            DrawCentered(sb, _prompt, panel.Center.X,
                panel.Y + UIConfig.ScaleDesktopMetric(124), UITheme.Ink, 0.60f * uiScale,
                panel.Width - UIConfig.ScaleDesktopMetric(100));
        }

        private Rectangle GetPanelBounds()
        {
            var viewport = _graphicsDevice.Viewport;
#if FIDA_ANDROID
            int width = System.Math.Max(1, viewport.Width - 48);
            int height = System.Math.Min(560, viewport.Height - 48);
            return new Rectangle((viewport.Width - width) / 2,
                (viewport.Height - height) / 2, width, height);
#else
            int width = System.Math.Min(UIConfig.ScaleDesktopMetric(760), viewport.Width - 48);
            int height = System.Math.Min(UIConfig.ScaleDesktopMetric(430), viewport.Height - 48);
            return new Rectangle((viewport.Width - width) / 2,
                (viewport.Height - height) / 2, width, height);
#endif
        }

        private void DrawCentered(SpriteBatch sb, string text, float centerX, float centerY,
            Color color, float preferredScale, float maxWidth)
        {
            string display = UIFontGuard.Sanitize(_font, text);
            Vector2 size = _font.MeasureString(display);
            float scale = size.X <= 0f ? preferredScale
                : System.Math.Min(preferredScale, maxWidth / size.X);
            sb.DrawString(_font, display,
                new Vector2(centerX - size.X * scale / 2f,
                    centerY - size.Y * scale / 2f),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
