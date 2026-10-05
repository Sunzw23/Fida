using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Fida.Campaign;

namespace Fida.UI.Campaign
{
    /// <summary>Darkens the live battle board and presents an advancing, bottom-anchored dialogue card.</summary>
    public sealed class UITutorialDialogue : UIButton
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        private string[] _lines;
        private int _lineIndex;

        public event Action? Finished;

        public UITutorialDialogue(int id, GraphicsDevice graphicsDevice, Texture2D pixel,
            SpriteFont font, CampaignNode node)
            : base(id, Rectangle.Empty, string.Empty, pixel, font)
        {
            _graphicsDevice = graphicsDevice;
            _pixel = pixel;
            _font = font;
            _lines = BuildLines(node);
            _lineIndex = 0;
            Name = "tutorial_dialogue";
            FocusIndex = -1000;
            Clicked += _ => Advance();
        }

        public override bool IsVisible { get; set; } = true;
        public override bool CanFocus => IsVisible;

        public void Advance()
        {
            if (!IsVisible) return;
            _lineIndex++;
            if (_lineIndex >= _lines.Length)
            {
                IsVisible = false;
                Finished?.Invoke();
            }
        }

        public void PresentInstruction(string instruction)
        {
            if (string.IsNullOrWhiteSpace(instruction)) return;
            _lines = [instruction];
            _lineIndex = 0;
            IsVisible = true;
        }

        public override void Update(GameTime gameTime)
        {
            Viewport viewport = _graphicsDevice.Viewport;
            Rectangle safe = UIMobileSafeArea.Get(viewport);
            int margin = Math.Clamp(viewport.Width / 24, 18, 44);
            int height = Math.Clamp(viewport.Height / 5, 142, 218);
            int bottomMargin = Math.Max(18, viewport.Height - safe.Bottom + 18);
            Bounds = new Rectangle(margin, viewport.Height - bottomMargin - height,
                Math.Max(1, viewport.Width - margin * 2), height);
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsVisible) return;
            Viewport viewport = _graphicsDevice.Viewport;
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height),
                new Color(8, 14, 26, 166));

            Color paper = UITheme.Panel;
            spriteBatch.Draw(_pixel, Bounds, paper);
            UIUtils.DrawRectangleOutlines(spriteBatch, Bounds, UITheme.Ink, 2, _pixel);
            int accent = Math.Clamp(Bounds.Width / 7, 54, 148);
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.Left, Bounds.Top, accent, 6), UITheme.Gold);
            spriteBatch.Draw(_pixel, new Rectangle(Bounds.Right - accent, Bounds.Bottom - 6, accent, 6),
                UITheme.AccentGreen);

            int pad = Math.Clamp(Bounds.Width / 32, 18, 38);
            float scale = Math.Clamp(Bounds.Width / 720f, 0.52f, 0.86f);
            Vector2 speakerAt = new(Bounds.Left + pad, Bounds.Top + pad * 0.55f);
            spriteBatch.DrawString(_font, "MARA VENN  /  FIELD UNIT", speakerAt,
                UITheme.AccentGreen, 0f, Vector2.Zero, scale * 0.82f,
                SpriteEffects.None, 0f);

            Rectangle textArea = new(Bounds.Left + pad, Bounds.Top + (int)(pad * 1.7f),
                Bounds.Width - pad * 2, Bounds.Height - (int)(pad * 2.45f));
            DrawWrapped(spriteBatch, _lines[_lineIndex], textArea, scale, UITheme.Ink);

            string progress = $"{_lineIndex + 1:00} / {_lines.Length:00}";
            Vector2 progressSize = _font.MeasureString(progress) * scale * 0.62f;
            spriteBatch.DrawString(_font, progress,
                new Vector2(Bounds.Right - pad - progressSize.X, Bounds.Bottom - pad - progressSize.Y),
                UITheme.InkMuted, 0f, Vector2.Zero, scale * 0.62f, SpriteEffects.None, 0f);
            DrawChevron(spriteBatch, new Vector2(Bounds.Right - pad * 0.55f, Bounds.Center.Y),
                Math.Max(6, (int)(10 * scale)), UITheme.Gold);
        }

        private void DrawWrapped(SpriteBatch sb, string text, Rectangle area, float scale, Color color)
        {
            string line = string.Empty;
            float y = area.Top;
            float lineHeight = _font.LineSpacing * scale * 1.12f;
            foreach (string word in (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && _font.MeasureString(candidate).X * scale > area.Width)
                {
                    sb.DrawString(_font, line, new Vector2(area.Left, y), color,
                        0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    y += lineHeight;
                    line = word;
                    if (y + lineHeight > area.Bottom) break;
                }
                else line = candidate;
            }
            if (line.Length > 0 && y + lineHeight <= area.Bottom)
                sb.DrawString(_font, line, new Vector2(area.Left, y), color,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private static string[] BuildLines(CampaignNode node) =>
            [string.IsNullOrWhiteSpace(node.Story) ? node.Name : node.Story];

        private void DrawChevron(SpriteBatch sb, Vector2 center, int size, Color color)
        {
            // Two clean strokes make a compact "next" affordance without adding a button label.
            int thickness = Math.Max(2, size / 4);
            DrawLine(sb, new Vector2(center.X - size, center.Y - size), center, color, thickness);
            DrawLine(sb, center, new Vector2(center.X - size, center.Y + size), color, thickness);
        }

        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 delta = end - start;
            sb.Draw(_pixel, start, null, color, (float)Math.Atan2(delta.Y, delta.X),
                Vector2.Zero, new Vector2(delta.Length(), thickness), SpriteEffects.None, 0f);
        }
    }
}
