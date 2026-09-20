using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
                14;
#endif
            int buttonWidth = System.Math.Min(
#if FIDA_ANDROID
                144,
#else
                112,
#endif
                (panel.Width - 128 - gap * 4) / columns);
            int buttonHeight =
#if FIDA_ANDROID
                82;
#else
                68;
#endif
            int gridWidth = columns * buttonWidth + gap * (columns - 1);
            int startX = panel.Center.X - gridWidth / 2;
            int startY = panel.Y + 174;

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
            sb.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height),
                UITheme.Vignette * 0.78f);

            Rectangle panel = GetPanelBounds();
            sb.Draw(_pixel, panel, UITheme.PanelLight);
            UIUtils.DrawEngravedFrame(sb, panel, _pixel, UITheme.GoldBright,
                UITheme.Ink, 4, 10, 28);

            DrawCentered(sb, "CHOOSE A NUMBER", panel.Center.X, panel.Y + 48,
                UITheme.Ink, 1f, panel.Width - 80);
            sb.Draw(_pixel, new Rectangle(panel.X + 80, panel.Y + 88,
                panel.Width - 160, 2), UITheme.Gold);
            DrawCentered(sb, _prompt, panel.Center.X, panel.Y + 126,
                UITheme.Ink, 0.72f, panel.Width - 100);
            DrawCentered(sb, "Select the value this number becomes.", panel.Center.X,
                panel.Y + 154, UITheme.InkMuted, 0.55f, panel.Width - 100);
            DrawCentered(sb, "Click or use the directional controls, then confirm.",
                panel.Center.X, panel.Bottom - 22, UITheme.InkMuted, 0.46f,
                panel.Width - 80);
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
            int width = System.Math.Min(760, viewport.Width - 48);
            int height = System.Math.Min(430, viewport.Height - 48);
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
