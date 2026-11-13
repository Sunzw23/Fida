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
        public Texture2D? MaraPortrait { get; set; }
        public SpriteFont? SpeakerFont { get; set; }

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
            Bounds = UIGumLayout.Bounds("Dialogue", "Panel", viewport);
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsVisible) return;
            Viewport viewport = _graphicsDevice.Viewport;
            // A state change can create this dialogue after Update in the same frame.
            // Resolve geometry here too so its first draw never uses Rectangle.Empty.
            Bounds = UIGumLayout.Bounds("Dialogue", "Panel", viewport);
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * .64f);

            UIPaper.StackedSquare(spriteBatch, Bounds, UIPaper.Face);

            var layout = new UIEditorialLayout(viewport);
            float scale = UITypography.Scale(_font, UITextRole.Body, viewport);
            var speakerFont = SpeakerFont ?? _font;
            float speakerScale = UITypography.Scale(speakerFont, UITextRole.Name, viewport);
            float captionScale = UITypography.Scale(_font, UITextRole.Small, viewport);
            if (!layout.Portrait)
            {
                scale = layout.Scale * 46 / _font.LineSpacing;
                speakerScale = layout.Scale * 58 / speakerFont.LineSpacing;
                captionScale = layout.Scale * 32 / _font.LineSpacing;
            }
            Rectangle portrait = layout.Element("Dialogue", "Portrait");
            if(portrait.Width > 0 && portrait.Height > 0 && PortraitAtlas != null)
            {
                int cellWidth=PortraitAtlas!.Width/3, cellHeight=PortraitAtlas.Height/2;
                string speaker=_lines[_lineIndex].Speaker?.ToLowerInvariant() ?? "";
                if (speaker.Contains("mara") && MaraPortrait != null)
                {
                    float maraFit = Math.Min(portrait.Width / (float)MaraPortrait.Width,
                        portrait.Height / (float)MaraPortrait.Height);
                    spriteBatch.Draw(MaraPortrait, new Vector2(portrait.Center.X, portrait.Bottom),
                        null, Color.White, 0, new Vector2(MaraPortrait.Width / 2f, MaraPortrait.Height),
                        maraFit, SpriteEffects.None, 0);
                }
                else
                {
                int index=speaker.Contains("mara") ? 1 : speaker.Contains("king") ? 3 : speaker.Contains("echo") ? 5 : 0;
                var source=new Rectangle(index%3*cellWidth,index/3*cellHeight,cellWidth,cellHeight);
                float fit=Math.Min(portrait.Width/(float)cellWidth,portrait.Height/(float)cellHeight);
                spriteBatch.Draw(PortraitAtlas,new Vector2(portrait.Center.X,portrait.Bottom),source,Color.White,
                    0,new Vector2(cellWidth/2f,cellHeight),fit,SpriteEffects.None,0);
                }
            }
            Rectangle speakerArea = layout.Element("Dialogue", "Speaker");
            Vector2 speakerAt = speakerArea.Location.ToVector2();
            speakerScale = Math.Min(speakerScale, speakerArea.Height / (float)speakerFont.LineSpacing);
            UITextRenderer.DrawString(spriteBatch, speakerFont, _lines[_lineIndex].Speaker, speakerAt,
                UIPaper.Ink, 0f, Vector2.Zero, speakerScale,
                SpriteEffects.None, 0f);

            spriteBatch.Draw(_pixel, layout.Element("Dialogue", "Divider"), UIPaper.MutedInk * .6f);
            Rectangle textArea = layout.Element("Dialogue", "Body");
            DrawWrapped(spriteBatch, _lines[_lineIndex].Text, textArea, scale, UIPaper.Ink);

            Rectangle next = layout.Element("Dialogue", "Next");
            UIPaper.Button(spriteBatch, next, UITheme.AccentBlue, HoverAmount, PressAmount, IsEnabled, cut: 6);
            UIUtils.DrawCenteredFit(spriteBatch,speakerFont,"Next",next.Center.ToVector2() - new Vector2(next.Width * .06f, 0),Math.Max(1,next.Width-30),
                layout.Scale * 44 / speakerFont.LineSpacing,UIPaper.Ink);
            DrawChevron(spriteBatch, new Vector2(next.Right - next.Height * .25f, next.Center.Y), Math.Max(4, next.Height / 9), UIPaper.Ink);
            string progress = $"{_lineIndex + 1} / {_lines.Length}";
            Vector2 progressSize = _font.MeasureString(progress) * captionScale;
            Rectangle progressArea = layout.Element("Dialogue", "Progress");
            UITextRenderer.DrawString(spriteBatch, _font, progress,
                progressArea.Center.ToVector2() - progressSize / 2,
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

