#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

/// <summary>
/// Geometry for the phone mission sheet: one printed card carrying the mission's artwork,
/// its difficulty/vow stamp, the story, the encounter cost, the reward and one action
/// ticket. Drawing and hit testing both read these rectangles, so the paper card and its
/// touch targets can never drift apart.
///
/// The type ramp comes from <see cref="UITypography"/>; the card only decides where the
/// line boxes sit. Only the artwork flexes when the viewport is unusually short.
/// </summary>
public sealed class CampaignMissionLayout
{
    private readonly int _width, _height;

    public Rectangle Sheet { get; }
    public Rectangle Eyebrow { get; }
    public Rectangle Close { get; }
    public Rectangle Title { get; }
    public Rectangle Art { get; }
    public Rectangle Story { get; }
    public Rectangle Metric { get; }
    public Rectangle RewardLabel { get; }
    public Rectangle RewardIcon { get; }
    public Rectangle RewardText { get; }
    public Rectangle Action { get; }

    /// <summary>Backing-sheet offsets and stamp sizes follow the same phone scale as the list.</summary>
    public float Scale { get; }

    /// <summary>Pixels per dp on this viewport; the sheet's geometry is authored in dp.</summary>
    public float PixelsPerDp { get; }

    public float LineHeight(UITextRole role) => UITypography.LineHeight(role, _width, _height);

    public CampaignMissionLayout(int width, int height) : this(width, height, null) { }

    /// <summary>
    /// <paramref name="safeArea"/> lets a caller supply the system-bar inset rectangle; the
    /// control reads it from <see cref="UIMobileSafeArea"/> when the platform has not reported
    /// insets yet, which is also what the preview harness exercises.
    /// </summary>
    public CampaignMissionLayout(int width, int height, Rectangle? safeArea)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);
        Rectangle safe = safeArea ?? UIMobileSafeArea.Get(new Viewport(0, 0, _width, _height));
        PixelsPerDp = UITypography.PixelsPerDp(_width);
        Scale = Math.Clamp(Math.Min(_width / 1080f, _height / 1920f), .5f, 2f);
        int Dp(float value) => Math.Max(1, (int)Math.Round(value * PixelsPerDp));

        int margin = Dp(18);
        int pad = Dp(22);
        int gap = Dp(12);
        int closeSide = Dp(48);
        int sheetWidth = Math.Min(Math.Max(1, _width - margin * 2), Dp(720));

        int eyebrowLine = (int)Math.Ceiling(LineHeight(UITextRole.Small));
        int titleLine = (int)Math.Ceiling(LineHeight(UITextRole.Heading));
        int captionLine = (int)Math.Ceiling(LineHeight(UITextRole.Caption));
        int bodyLine = (int)Math.Ceiling(LineHeight(UITextRole.Body));
        int storyLines = 3;
        int storyHeight = (int)Math.Round(bodyLine * 1.15f * storyLines);
        int rewardHeight = Math.Max(captionLine, Dp(30));
        int actionHeight = Dp(54);
        int minArt = Dp(64);
        int wantedArt = Math.Clamp((int)Math.Round(sheetWidth * .32f), Dp(96), Dp(150));

        // Every row except the artwork; the artwork absorbs the difference so the action
        // ticket stays anchored to the sheet's foot on any phone height. The close button
        // owns the header band, so the title always prints across the full content width.
        int headerHeight = Math.Max(eyebrowLine, closeSide);
        int fixedRows = pad + headerHeight + gap / 2 + titleLine + gap + gap + storyHeight + gap +
            captionLine + gap + rewardHeight + gap + actionHeight + pad;
        int maximum = Math.Max(fixedRows + minArt, safe.Height - margin * 2);
        int sheetHeight = Math.Clamp(fixedRows + wantedArt, fixedRows + minArt, maximum);
        int artHeight = Math.Clamp(sheetHeight - fixedRows, minArt, wantedArt);

        int left = safe.Left + (safe.Width - sheetWidth) / 2;
        int top = safe.Top + (safe.Height - sheetHeight) / 2;
        Sheet = new Rectangle(left, top, sheetWidth, sheetHeight);

        int contentLeft = Sheet.Left + pad;
        int contentWidth = Sheet.Width - pad * 2;
        Close = new Rectangle(Sheet.Right - pad - closeSide, Sheet.Top + pad, closeSide, closeSide);
        Eyebrow = new Rectangle(contentLeft, Sheet.Top + pad + (headerHeight - eyebrowLine) / 2,
            Math.Max(1, Close.Left - gap - contentLeft), eyebrowLine);
        Title = new Rectangle(contentLeft, Sheet.Top + pad + headerHeight + gap / 2, contentWidth, titleLine);
        Art = new Rectangle(contentLeft, Title.Bottom + gap, contentWidth, artHeight);
        Story = new Rectangle(contentLeft, Art.Bottom + gap, contentWidth, storyHeight);
        Metric = new Rectangle(contentLeft, Story.Bottom + gap, contentWidth, captionLine);
        int rewardTop = Metric.Bottom + gap;
        int labelWidth = Math.Min(contentWidth / 2, Dp(86));
        int iconSide = rewardHeight;
        RewardLabel = new Rectangle(contentLeft, rewardTop, labelWidth, rewardHeight);
        RewardIcon = new Rectangle(RewardLabel.Right, rewardTop, iconSide, iconSide);
        RewardText = new Rectangle(RewardIcon.Right + gap / 2, rewardTop,
            Math.Max(1, Sheet.Right - pad - (RewardIcon.Right + gap / 2)), rewardHeight);
        Action = new Rectangle(contentLeft, Sheet.Bottom - pad - actionHeight, contentWidth, actionHeight);
    }
}
