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
            int buttonY = panel.Bottom - 94;
            ApplyButton = new UIAnimatedButton(ApplyButtonId,
                new Rectangle(panel.X + 78, buttonY, 250, 66), "Apply", pixel, font)
            {
                FocusIndex = 410
            };
            PassButton = new UIAnimatedButton(PassButtonId,
                new Rectangle(panel.Right - 328, buttonY, 250, 66), "Pass", pixel, font)
            {
                FocusIndex = 411
            };
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

            DrawCentered(sb, "OPTIONAL EFFECT", panel.Center.X, panel.Y + 48,
                UITheme.Ink, 1f, panel.Width - 80);
            sb.Draw(_pixel, new Rectangle(panel.X + 80, panel.Y + 88,
                panel.Width - 160, 2), UITheme.Gold);
            DrawCentered(sb, _prompt, panel.Center.X, panel.Y + 132,
                UITheme.Ink, 0.82f, panel.Width - 100);
            DrawCentered(sb, "Apply this effect?", panel.Center.X, panel.Y + 184,
                UITheme.InkMuted, 0.72f, panel.Width - 100);
            DrawCentered(sb, "Enter / A: Apply        Esc: Pass", panel.Center.X,
                panel.Bottom - 15, UITheme.InkMuted, 0.48f, panel.Width - 80);
        }

        private Rectangle GetPanelBounds()
        {
            var viewport = _graphicsDevice.Viewport;
            int width = System.Math.Min(780, viewport.Width - 48);
            int height = 340;
            return new Rectangle((viewport.Width - width) / 2,
                (viewport.Height - height) / 2, width, height);
        }

        private void DrawCentered(SpriteBatch sb, string text, float centerX, float centerY,
            Color color, float preferredScale, float maxWidth)
        {
            string display = UIFontGuard.Sanitize(_font, text);
            Vector2 size = _font.MeasureString(display);
            float scale = size.X <= 0f
                ? preferredScale
                : System.Math.Min(preferredScale, maxWidth / size.X);
            sb.DrawString(_font, display,
                new Vector2(centerX - size.X * scale / 2f,
                    centerY - size.Y * scale / 2f),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
