#nullable enable
using System;
using Fida.Campaign;
using Fida.UI.Rules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// The achievement sheet, drawn in the field guide's own language: header bar, paper sheet,
    /// paged rows, folio bottom right. Paging is what makes the list extensible — adding an
    /// achievement never changes the layout.
    /// </summary>
    public sealed class UIAchievementsPage : UIControl
    {
        public const int DesktopPerPage = 4;
        public const int PortraitPerPage = 5;

        private readonly ResourceManager _resources;
        private readonly CampaignProgress _progress;
        private readonly int _page;

        public UIAchievementsPage(ResourceManager resources, CampaignProgress progress, int page = 0)
            : base(-55)
        {
            _resources = resources;
            _progress = progress;
            _page = page;
        }

        public static int PerPage(UIRulesLayout layout) => layout.Portrait ? PortraitPerPage : DesktopPerPage;

        public static int PageCount(UIRulesLayout layout) =>
            Math.Max(1, (Achievements.All.Count + PerPage(layout) - 1) / PerPage(layout));

        public override void Draw(SpriteBatch batch, GameTime time)
        {
            if (!IsVisible) return;

            var layout = new UIRulesLayout(batch.GraphicsDevice.Viewport);
            var canvas = layout.Canvas;
            float scale = layout.Scale;

            UIPaper.Background(batch, batch.GraphicsDevice.Viewport);
            UIPaper.Backdrop(batch, batch.GraphicsDevice.Viewport, time);
            UIPaper.Sheet(batch,
                canvas.Portrait ? canvas.Rect(30, 30, 840, 135) : canvas.Rect(45, 30, 1830, 135),
                UITheme.Panel, 3, 9);
            UIRulesText.Line(batch, _resources.LabelFont, "Achievements", layout.Header, 65, scale,
                UITheme.Ink);

            UIPaper.Sheet(batch, layout.Sheet, UIPaper.Face, 4, 10);

            Rectangle inner = layout.Inner;
            UIRulesText.Line(batch, _resources.CaptionFont,
                $"{Achievements.EarnedCount(_progress)} / {Achievements.All.Count} earned",
                new Rectangle(inner.Left, inner.Top, inner.Width, layout.D(26)), 24, scale,
                UIPaper.MutedInk, centered: true);

            int perPage = PerPage(layout);
            int start = _page * perPage;
            int rowHeight = layout.Portrait ? 168 : 104;
            int rowTop = inner.Top + layout.D(42);
            float rowLeft = inner.Left;
            float rowRight = inner.Right;

            for (int slot = 0; slot < perPage && start + slot < Achievements.All.Count; slot++)
            {
                var achievement = Achievements.All[start + slot];
                bool done = Achievements.Earned(achievement, _progress);
                Rectangle row = new((int)rowLeft, rowTop + slot * layout.D(rowHeight),
                    (int)(rowRight - rowLeft), layout.D(rowHeight));

                // Sheet colours are UIPaper's (dark ink on cream); UITheme.Ink is the cream
                // used on dark panels and is invisible here.
                batch.Draw(_resources.Pixel,
                    new Rectangle(row.Left, row.Top + layout.D(4), layout.D(8), layout.D(58)),
                    done ? UIPaper.Ink : UIPaper.MutedInk * .45f);
                batch.Draw(_resources.Pixel,
                    new Rectangle(row.Left, row.Bottom - layout.D(16), row.Width, 1),
                    UIPaper.MutedInk * .35f);

                UIRulesText.Line(batch, _resources.LabelFont, achievement.Name,
                    new Rectangle(row.Left + layout.D(26), row.Top, row.Width - layout.D(320),
                        layout.D(46)), 40, scale, done ? UIPaper.Ink : UIPaper.MutedInk);
                UIRulesText.Line(batch, _resources.Font, achievement.Description,
                    new Rectangle(row.Left + layout.D(26), row.Top + layout.D(46),
                        row.Width - layout.D(320), layout.D(40)), 30, scale, UIPaper.MutedInk);

                // The detail column is right-aligned, so it needs its own measurement.
                string detail = Achievements.Detail(achievement, _progress);
                float detailScale = 24f * scale / Math.Max(1f, _resources.CaptionFont.LineSpacing);
                float detailWidth = _resources.CaptionFont.MeasureString(detail).X * detailScale;
                batch.DrawString(_resources.CaptionFont, detail,
                    new Vector2(row.Right - detailWidth, row.Top + layout.D(8)),
                    done ? UIPaper.Ink : UIPaper.MutedInk,
                    0f, Vector2.Zero, detailScale, SpriteEffects.None, 0f);
            }

            if (PageCount(layout) > 1)
                UIRulesText.Line(batch, _resources.CaptionFont, $"{_page + 1} / {PageCount(layout)}",
                    layout.PageLabel, 26, scale, UITheme.InkMuted, centered: true);
        }
    }
}
