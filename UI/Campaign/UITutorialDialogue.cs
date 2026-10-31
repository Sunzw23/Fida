#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Linq;
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
        private CampaignStoryPage[] _lines;
        private int _lineIndex;
        public Texture2D? PortraitAtlas { get; set; }

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
            Confirmed += _ => Advance();
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
            _lines = [new CampaignStoryPage { Speaker = "Mara Venn", Text = instruction }];
            _lineIndex = 0;
            IsVisible = true;
        }

        public void PresentPages(IEnumerable<CampaignStoryPage> pages, CampaignProgress? progress = null)
        {
            _lines = pages.Where(page => !string.IsNullOrWhiteSpace(page.Text) &&
                (string.IsNullOrEmpty(page.RequiredCompletedNode) ||
                 progress?.IsCompleted(page.RequiredCompletedNode) == true)).ToArray();
            _lineIndex = 0;
            IsVisible = _lines.Length > 0;
        }

        public void Finish()
        {
            if (!IsVisible) return;
            IsVisible = false;
            Finished?.Invoke();
        }

        public override void Update(GameTime gameTime)
        {
            Viewport viewport = _graphicsDevice.Viewport;
            Rectangle safe = UIMobileSafeArea.Get(viewport);
            int margin = Math.Clamp(viewport.Width / 24, 18, 44);
            int height = Math.Min(safe.Height - 36, Math.Clamp(viewport.Height / 3, 210, 340));
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

            var backing = Bounds; backing.Offset(-6, 6);
            UIPaper.Sheet(spriteBatch, backing, UITheme.PanelLight, 3, 10);
            UIPaper.Sheet(spriteBatch, Bounds, UIPaper.Face, 4, 9);

            int pad = Math.Clamp(Bounds.Width / 32, 18, 38);
            float scale = UITypography.Scale(_font, UITextRole.Body, viewport);
            float speakerScale = UITypography.Scale(_font, UITextRole.Name, viewport);
            float captionScale = UITypography.Scale(_font, UITextRole.Small, viewport);
            int portraitWidth = Bounds.Width >= 700 && PortraitAtlas != null ? Math.Min(Bounds.Width/4,Bounds.Height) : 0;
            if(portraitWidth>0)
            {
                int cellWidth=PortraitAtlas!.Width/3, cellHeight=PortraitAtlas.Height/2;
                string speaker=_lines[_lineIndex].Speaker?.ToLowerInvariant() ?? "";
                int index=speaker.Contains("mara") ? 1 : speaker.Contains("king") ? 3 : speaker.Contains("echo") ? 5 : 0;
                var source=new Rectangle(index%3*cellWidth,index/3*cellHeight,cellWidth,cellHeight);
                float fit=Math.Min(portraitWidth/(float)cellWidth,(Bounds.Height-pad)/(float)cellHeight);
                spriteBatch.Draw(PortraitAtlas,new Vector2(Bounds.Left+pad+portraitWidth/2,Bounds.Bottom-pad/2),source,Color.White,
                    0,new Vector2(cellWidth/2f,cellHeight),fit,SpriteEffects.None,0);
            }
            Vector2 speakerAt = new(Bounds.Left + pad + portraitWidth, Bounds.Top + pad * 0.55f);
            UITextRenderer.DrawString(spriteBatch, _font, _lines[_lineIndex].Speaker, speakerAt,
                UIPaper.Ink, 0f, Vector2.Zero, speakerScale,
                SpriteEffects.None, 0f);

            int textTop = (int)(speakerAt.Y + _font.LineSpacing * speakerScale + 12);
            int textBottom = Bounds.Bottom - pad - (int)(_font.LineSpacing * captionScale) - 10;
            Rectangle textArea = new(Bounds.Left + pad + portraitWidth, textTop,
                Bounds.Width - pad * 2 - 12 - portraitWidth, System.Math.Max(1, textBottom - textTop));
            DrawWrapped(spriteBatch, _lines[_lineIndex].Text, textArea, scale, UIPaper.Ink);

            int nextWidth=Math.Clamp(Bounds.Width/7,100,180), nextHeight=Math.Clamp(Bounds.Height/5,42,64);
            var next=new Rectangle(Bounds.Right-pad-nextWidth,Bounds.Bottom-pad-nextHeight,nextWidth,nextHeight);
            UIPaper.Sheet(spriteBatch,next,UITheme.AccentBlue,2,6);
            UIUtils.DrawCenteredFit(spriteBatch,_font,"Next  >",next.Center.ToVector2(),next.Width-16,captionScale*1.25f,UIPaper.Ink);
            string progress = $"{_lineIndex + 1} / {_lines.Length}";
            Vector2 progressSize = _font.MeasureString(progress) * captionScale;
            UITextRenderer.DrawString(spriteBatch, _font, progress,
                new Vector2(next.Left - pad - progressSize.X, next.Center.Y-progressSize.Y/2),
                UIPaper.MutedInk, 0f, Vector2.Zero, captionScale, SpriteEffects.None, 0f);
        }

        private void DrawWrapped(SpriteBatch sb, string text, Rectangle area, float scale, Color color)
        {
            List<string> Wrap(float s)
            {
                var result = new List<string>();
                foreach (string paragraph in (text ?? string.Empty).Split('\n'))
                {
                    string line = string.Empty;
                    foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string candidate = line.Length == 0 ? word : line + " " + word;
                        if (line.Length > 0 && _font.MeasureString(candidate).X * s > area.Width)
                        {
                            result.Add(line);
                            line = word;
                        }
                        else line = candidate;
                    }
                    result.Add(line);
                }
                return result;
            }
            var lines = Wrap(scale);
            while (lines.Count * _font.LineSpacing * scale * 1.12f > area.Height && scale > 0.3f)
            {
                scale *= 0.94f;
                lines = Wrap(scale);
            }
            float y = area.Top;
            foreach (string line in lines)
            {
                UITextRenderer.DrawString(sb, _font, line, new Vector2(area.Left, y), color,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                y += _font.LineSpacing * scale * 1.12f;
            }
        }

        private static CampaignStoryPage[] BuildLines(CampaignNode node) => node.BeforeStory.Count > 0
            ? node.BeforeStory.ToArray()
            : [new CampaignStoryPage { Text = string.IsNullOrWhiteSpace(node.Story) ? node.Name : node.Story }];

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
