#nullable enable

using System;

using System.Collections.Generic;

using System.Linq;

using Fida.Campaign;

using Fida.Logic;

using Microsoft.Xna.Framework;

using Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework.Input;



namespace Fida.UI.Campaign;



public sealed partial class UICampaignCompanionSelect

{

    private void UpdateMotion(GameTime gameTime)

    {

        double seconds = gameTime.ElapsedGameTime.TotalSeconds;

        foreach (var bounds in _actionMotion.Keys.ToArray())

        {

            var state = _actionMotion[bounds];

            bool hover = state.Enabled && !InputBlocked && bounds.Contains(_previousMouse.Position);

            bool press = hover && _previousMouse.LeftButton == ButtonState.Pressed &&

                _pressedTarget.Length > 0 && _pressedTarget == HitTarget(_previousMouse.Position, Layout);

            _actionMotion[bounds] = (UIMotion.Approach(state.Hover, hover ? 1f : 0f, seconds),

                UIMotion.Approach(state.Press, press ? 1f : 0f, seconds, 24), state.Enabled);

        }

    }



    private (float Hover, float Press) Motion(Rectangle bounds, bool enabled)

    {

        if (_actionMotion.Count > 128) _actionMotion.Clear();

        _actionMotion.TryGetValue(bounds, out var state);

        _actionMotion[bounds] = (state.Hover, state.Press, enabled);

        return enabled && !InputBlocked ? (state.Hover, state.Press) : (0, 0);

    }



    private void Surface(SpriteBatch batch, Rectangle bounds, Color color, bool enabled = true, int cut = 7, bool hover = true)

    {

        var motion = Motion(bounds, enabled);

        UIPaper.Button(batch, bounds, color, hover ? motion.Hover : 0f, motion.Press, enabled && !InputBlocked,

            cut: cut, shape: UIPaperShape.Ticket);

    }



    private void Text(SpriteBatch batch, string text, Rectangle area, UITextRole role, Color color,

        bool left = false)

    {

        if (area.Width <= 0 || area.Height <= 0) return;

        SpriteFont font = role is UITextRole.Heading or UITextRole.Name ? _titleFont : _font;

        string safe = UIFontGuard.Sanitize(font, text);

        Vector2 size = font.MeasureString(safe);

        if (size.X <= 0f) return;

        float scale = UITextRenderer.FitScale(size, area.Width, area.Height,

            Layout.LineHeight(role) / font.LineSpacing);

        Vector2 position = new(left ? area.Left : area.Center.X - size.X * scale / 2f,

            area.Center.Y - size.Y * scale / 2f);

        UITextRenderer.DrawString(batch, font, safe, position, color, scale: scale);

    }



    private void Rule(SpriteBatch batch, int x, int y, int width) =>

        batch.Draw(_pixel, new Rectangle(x, y, width, 1), UITheme.Line);



    public override void Draw(SpriteBatch batch, GameTime gameTime)

    {

        if (!IsVisible) return;

        var layout = Layout;

        if (!layout.Portrait)

        {

            var campaign = new CampaignMapLayout(_device.Viewport);

            UIPaperFrame.Draw(batch, campaign.Element("HeaderStrip"), new Color(27, 46, 62),

                UIPaperFrameShape.Rectangle, 0);

        }



        var backMotion = Motion(layout.Back, true);

        UIIconButton.DrawBackButton(batch, _pixel, layout.Back, hoverAmount: backMotion.Hover,

            pressAmount: backMotion.Press, enabled: !InputBlocked);



        int gap = Math.Max(6, (int)(16 * layout.Scale));

        int captionHeight = (int)layout.LineHeight(UITextRole.Caption);

        // The roster title stands alone; only an online draft adds a status line, so the

        // party cap is not repeated in three places (header, band and detail).

        bool hasStatus = !string.IsNullOrWhiteSpace(StatusText);

        int statusWidth = hasStatus

            ? Math.Min((int)(360 * layout.Scale), Math.Max(1, layout.Header.Width / 2))

            : 0;

        var title = new Rectangle(layout.Header.Left, layout.Header.Top,

            Math.Max(1, layout.Header.Width - (hasStatus ? statusWidth + gap * 2 : 0)), layout.Header.Height);

        Text(batch, HeaderText, title, UITextRole.Heading, UITheme.Ink, true);

        if (hasStatus)

            Text(batch, StatusText, new Rectangle(layout.Header.Right - statusWidth,

                title.Center.Y - captionHeight / 2, statusWidth, captionHeight),

                UITextRole.Caption, UITheme.InkMuted);



        DrawDetailSheet(batch, layout);

        DrawRoster(batch, layout);

        DrawPartyBand(batch, layout);

        Surface(batch, layout.Continue, UIPaper.Face, !InputBlocked, 8);

        Text(batch, ContinueActionText, Inset(layout.Continue, 10, 3), UITextRole.Name, UIPaper.Ink);

    }



    private static Rectangle Inset(Rectangle bounds, int horizontal, int vertical) =>

        new(bounds.X + horizontal, bounds.Y + vertical,

            Math.Max(1, bounds.Width - horizontal * 2), Math.Max(1, bounds.Height - vertical * 2));



    private void DrawDetailSheet(SpriteBatch batch, CompanionSelectLayout layout)

    {

        var sheet = layout.Details;

        sheet.Inflate(8, 8);

        var far = sheet;

        far.Offset((int)(12 * layout.Scale), (int)(10 * layout.Scale));

        var near = sheet;

        near.Offset(-(int)(8 * layout.Scale), (int)(5 * layout.Scale));

        UIPaperFrame.Stack(batch, sheet, far, near, layout.Scale);



        if (FocusedCompanion is not { } companion)

        {

            Text(batch, "No companions available", layout.DetailText(), UITextRole.Heading,

                UITheme.InkMuted, true);

            return;

        }



        UICompanionArt.Draw(batch, _resources, companion, layout.DetailArt());

        var area = layout.DetailText();

        int pad = Math.Max(12, (int)(22 * layout.Scale));

        int heading = (int)layout.LineHeight(UITextRole.Heading);

        int caption = (int)layout.LineHeight(UITextRole.Caption);

        Text(batch, companion.Name, new Rectangle(area.Left, area.Top, area.Width, heading),

            UITextRole.Heading, UITheme.Ink, true);

        int statsTop = area.Top + heading + 4;

        Text(batch, Stats(companion), new Rectangle(area.Left, statsTop, area.Width, caption),

            UITextRole.Caption, UITheme.InkMuted, true);

        int ruleY = statsTop + caption + pad / 2;

        Rule(batch, area.Left, ruleY, area.Width);

        var description = new Rectangle(area.Left, ruleY + pad,

            area.Width, Math.Max(1, area.Bottom - pad - (ruleY + pad)));

        // A short flavour paragraph is enough here; the full text lives in the roster data.

        DrawDescription(batch, companion.Description, description, layout.Portrait ? 3 : 4);

    }



    /// <summary>

    /// Wraps a description into at most <paramref name="maximumLines"/> lines. A finished

    /// sentence inside the clamp ends the paragraph cleanly; only an unfinished one gets an

    /// ellipsis, so the sheet is not a wall of prose or a cut-off phrase.

    /// </summary>

    private void DrawDescription(SpriteBatch batch, string text, Rectangle area, int maximumLines)

    {

        if (area.Width <= 0 || area.Height <= 0 || string.IsNullOrWhiteSpace(text)) return;

        float scale = UITypography.Scale(_font, UITextRole.Body, _device.Viewport);

        var lines = UITextRenderer.WrapLines(UIFontGuard.Sanitize(_font, text), area.Width,

            value => _font.MeasureString(value).X * scale);

        float lineHeight = _font.LineSpacing * scale * 1.2f;

        int fit = Math.Max(1, Math.Min(maximumLines, (int)(area.Height / lineHeight)));

        if (lines.Count > fit)

        {

            string visible = string.Join(" ", lines.Take(fit));

            int sentence = visible.LastIndexOfAny(new[] { '.', '!', '?' });

            if (sentence >= visible.Length / 2)

            {

                lines = UITextRenderer.WrapLines(UIFontGuard.Sanitize(_font, visible[..(sentence + 1)]),

                    area.Width, value => _font.MeasureString(value).X * scale);

            }

            else

            {

                var clipped = lines.Take(fit).ToArray();

                clipped[fit - 1] = Ellipsize(clipped[fit - 1], area.Width, scale);

                lines = clipped;

            }

        }

        for (int i = 0; i < Math.Min(lines.Count, fit); i++)

            UITextRenderer.DrawString(batch, _font, lines[i],

                new Vector2(area.Left, area.Top + i * lineHeight), UITheme.Ink, scale: scale);

    }



    private string Ellipsize(string line, float width, float scale)

    {

        string safe = UIFontGuard.Sanitize(_font, line);

        while (safe.Length > 1 && _font.MeasureString(safe + "…").X * scale > width)

            safe = safe[..^1].TrimEnd();

        return safe + "…";

    }



    private void DrawRoster(SpriteBatch batch, CompanionSelectLayout layout)

    {

        IReadOnlyList<string> visible = VisibleCardIds;

        for (int index = 0; index < visible.Count; index++)

            DrawCard(batch, layout, visible[index], index);

    }



    private void DrawCard(SpriteBatch batch, CompanionSelectLayout layout, string cardId, int index)

    {

        Rectangle bounds = layout.Card(index);

        bool focused = string.Equals(cardId, FocusedCardId, StringComparison.Ordinal);

        bool selected = IsSelected(cardId);

        bool enabled = CanToggle(cardId);

        int pad = Math.Max(6, (int)(16 * layout.Scale));



        if (focused)

        {

            var frame = bounds;

            frame.Inflate(3, 3);

            UIPaper.Sheet(batch, frame, UITheme.AccentBlue, 1, 7, shape: UIPaperShape.Ticket);

        }

        // Invited cards carry a warm paper tint; the focused card keeps the cyan outline.
        Color face = selected ? Color.Lerp(UIPaper.Face, new Color(206, 168, 104), .45f) : UIPaper.Face;
        Surface(batch, bounds, face, enabled, 7, hover: false);



        if (CardCompanion(cardId) is not { } companion) return;

        UICompanionArt.Draw(batch, _resources, companion, layout.CardArt(index));

        Text(batch, companion.Name, layout.CardName(index), UITextRole.Name, UIPaper.Ink);

        Text(batch, Caption(companion), layout.CardCaption(index), UITextRole.Small,

            selected ? UITheme.PanelHover : UIPaper.MutedInk);



        // Selection is shown by a stamp instead of a sentence: a check once invited, a plus

        // while the card can still be invited (a full party simply dims the card).

        int side = Math.Max(14, (int)(24 * layout.Scale));

        var mark = new Rectangle(bounds.Right - pad - side, bounds.Top + pad, side, side);

        if (selected) UIPaper.Checkmark(batch, mark);

        else if (enabled) DrawPlus(batch, mark, UIPaper.MutedInk);

    }



    private void DrawPlus(SpriteBatch batch, Rectangle bounds, Color color)

    {

        int thickness = Math.Max(2, bounds.Width / 7);

        int arm = Math.Max(4, bounds.Width / 3);

        int cx = bounds.Center.X, cy = bounds.Center.Y;

        batch.Draw(_pixel, new Rectangle(cx - arm, cy - thickness / 2, arm * 2, thickness), color);

        batch.Draw(_pixel, new Rectangle(cx - thickness / 2, cy - arm, thickness, arm * 2), color);

    }



    private void DrawPartyBand(SpriteBatch batch, CompanionSelectLayout layout)
    {
        Rectangle band = layout.Party;
        int small = (int)layout.LineHeight(UITextRole.Small);
        int gap = Math.Max(6, (int)(14 * layout.Scale));
        Rule(batch, band.Left, band.Top, band.Width);
        int pagerWidth = PageCount > 1 ? (int)(layout.Previous.Left - band.Left) : band.Width;
        Text(batch, $"PARTY  {_selected.Count} / {PartyLimit}",
            new Rectangle(band.Left, band.Top + 4, Math.Max(1, pagerWidth - gap), small),
            UITextRole.Small, UITheme.InkMuted, true);
        var chips = PartyChips(layout);
        foreach (var (chipId, chipBounds) in chips)
        {
            var companion = CampaignCompanions.Get(chipId);
            Surface(batch, chipBounds, UIPaper.Face, true, 5);
            int iconSide = Math.Min(chipBounds.Height - 10, Math.Max(18, (int)(30 * layout.Scale)));
            var icon = new Rectangle(chipBounds.Left + 5, chipBounds.Center.Y - iconSide / 2,
                iconSide, iconSide);
            if (companion != null) UICompanionArt.Draw(batch, _resources, companion, icon);
            Text(batch, companion?.Name ?? chipId, new Rectangle(icon.Right + 8, chipBounds.Top,
                Math.Max(1, chipBounds.Width - iconSide - 46), chipBounds.Height),
                UITextRole.Small, UIPaper.Ink, true);
            Text(batch, "×", new Rectangle(chipBounds.Right - 26, chipBounds.Top, 22, chipBounds.Height),
                UITextRole.Caption, UIPaper.MutedInk);
        }

        if (PageCount <= 1) return;
        Surface(batch, layout.Previous, UIPaper.Face, Page > 0, 5);
        Text(batch, "<", layout.Previous, UITextRole.Name, UIPaper.Ink);
        Surface(batch, layout.Next, UIPaper.Face, Page + 1 < PageCount, 5);
        Text(batch, ">", layout.Next, UITextRole.Name, UIPaper.Ink);
        Text(batch, $"{Page + 1} / {PageCount}", new Rectangle(layout.Previous.Right, layout.Previous.Top,
            Math.Max(1, layout.Next.Left - layout.Previous.Right), layout.Previous.Height),
            UITextRole.Small, UITheme.InkMuted);
    }

    /// <summary>Party chips: the current party, in invitation order, each removable by click.</summary>
    private IReadOnlyList<(string CompanionId, Rectangle Bounds)> PartyChips(CompanionSelectLayout layout)
    {
        if (_selected.Count == 0) return Array.Empty<(string, Rectangle)>();
        Rectangle band = layout.Party;
        int small = (int)layout.LineHeight(UITextRole.Small);
        int gap = Math.Max(6, (int)(14 * layout.Scale));
        int top = band.Top + Math.Max(small + 8, (int)(40 * layout.Scale));
        int height = Math.Max(40, Math.Min((int)(64 * layout.Scale), band.Bottom - top - 6));
        if (height < 28) return Array.Empty<(string, Rectangle)>();
        int x = band.Left;
        int right = PageCount > 1 ? layout.Previous.Left - gap * 2 : band.Right;
        float scale = UITypography.Scale(_captionFont, UITextRole.Small, _device.Viewport);
        var chips = new List<(string, Rectangle)>();
        foreach (string chipId in _selected)
        {
            var companion = CampaignCompanions.Get(chipId);
            string label = companion?.Name ?? chipId;
            float measured = _captionFont.MeasureString(UIFontGuard.Sanitize(_captionFont, label)).X;
            int width = Math.Clamp((int)(measured * scale) + height + (int)(64 * layout.Scale),
                170, Math.Max(180, (int)(320 * layout.Scale)));
            if (x + width > right) break;
            chips.Add((chipId, new Rectangle(x, top, width, height)));
            x += width + gap;
        }
        return chips;
    }
    private void DrawPager(SpriteBatch batch, CompanionSelectLayout layout)
    {
        Surface(batch, layout.Previous, UIPaper.Face, Page > 0, 5);
        Text(batch, "<", layout.Previous, UITextRole.Name, UIPaper.Ink);
        Surface(batch, layout.Next, UIPaper.Face, Page + 1 < PageCount, 5);
        Text(batch, ">", layout.Next, UITextRole.Name, UIPaper.Ink);
        Text(batch, $"{Page + 1} / {PageCount}", new Rectangle(layout.Previous.Right, layout.Previous.Top,
            Math.Max(1, layout.Next.Left - layout.Previous.Right), layout.Previous.Height),
            UITextRole.Small, UITheme.InkMuted);
    }
    private CampaignCompanion? CardCompanion(string cardId) => _roster.FirstOrDefault(companion =>

        string.Equals(companion.Id, cardId, StringComparison.OrdinalIgnoreCase));



    /// <summary>Cards only print the one stat that matters; the invite stamp shows the state.</summary>

    private static string Caption(CampaignCompanion companion) => $"HP {companion.Health}";



    private string Stats(CampaignCompanion companion)

    {

        var parts = new List<string> { $"HP {companion.Health}" };

        parts.AddRange(companion.Skills.Select(GameText.SkillName));

        if (PassiveName(companion.Passive) is { Length: > 0 } passive) parts.Add(passive);

        if (companion.Summons is { } summon) parts.Add(summon.Name);

        return string.Join("   ·   ", parts);

    }



    /// <summary>The roster's mechanical identity lives beside HP; the prose is flavour only.</summary>

    private static string PassiveName(string passive) => passive switch

    {

        "gate_ward" => "Gate ward",

        _ => ""

    };

}

