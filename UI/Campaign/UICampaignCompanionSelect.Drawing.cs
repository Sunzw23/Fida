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

    private void Surface(SpriteBatch batch, Rectangle bounds, Color color, bool enabled = true, int cut = 7)
    {
        var motion = Motion(bounds, enabled);
        UIPaper.Button(batch, bounds, color, motion.Hover, motion.Press, enabled && !InputBlocked,
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
        var title = new Rectangle(layout.Header.Left, layout.Header.Top,
            Math.Max(1, layout.Header.Width - (int)(320 * layout.Scale)), layout.Header.Height);
        Text(batch, HeaderText, title, UITextRole.Heading, UITheme.Ink, true);
        var hint = new Rectangle(title.Right, title.Center.Y - captionHeight / 2,
            Math.Max(1, layout.Header.Right - title.Right - gap * 2), captionHeight);
        string hintText = string.IsNullOrWhiteSpace(StatusText) ? HintText : StatusText;
        if (!string.IsNullOrWhiteSpace(hintText))
            Text(batch, hintText, hint, UITextRole.Caption, UITheme.InkMuted);

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
        int stateTop = area.Bottom - caption;
        var description = new Rectangle(area.Left, ruleY + pad,
            area.Width, Math.Max(1, stateTop - pad - (ruleY + pad)));
        if (!string.IsNullOrWhiteSpace(companion.Description))
            UITextRenderer.DrawParagraph(batch, _font, companion.Description, description, UITheme.Ink);
        Text(batch, StateText(companion.Id), new Rectangle(area.Left, stateTop, area.Width, caption),
            UITextRole.Caption, StateColor(companion.Id), true);
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
        Surface(batch, bounds, UIPaper.Face, enabled, 7);

        if (CardCompanion(cardId) is not { } companion) return;
        UICompanionArt.Draw(batch, _resources, companion, layout.CardArt(index));
        Text(batch, companion.Name, layout.CardName(index), UITextRole.Name, UIPaper.Ink);
        Text(batch, Caption(companion), layout.CardCaption(index), UITextRole.Small,
            selected ? UITheme.PanelHover : UIPaper.MutedInk);

        if (selected)
        {
            int side = Math.Max(14, (int)(24 * layout.Scale));
            UIPaper.Checkmark(batch,
                new Rectangle(bounds.Right - pad - side, bounds.Top + pad, side, side));
        }
    }

    private void DrawPartyBand(SpriteBatch batch, CompanionSelectLayout layout)
    {
        Rectangle band = layout.Party;
        int caption = (int)layout.LineHeight(UITextRole.Caption);
        int small = (int)layout.LineHeight(UITextRole.Small);
        int gap = Math.Max(6, (int)(14 * layout.Scale));
        Rule(batch, band.Left, band.Top, band.Width);
        int pagerWidth = PageCount > 1 ? (int)(layout.Previous.Left - band.Left) : band.Width;
        var label = new Rectangle(band.Left, band.Top + 4,
            Math.Min(pagerWidth, Math.Max(60, (int)(92 * layout.Scale))), small);
        Text(batch, "PARTY", label, UITextRole.Small, UITheme.InkMuted, true);
        var counter = new Rectangle(label.Right, band.Top + 4,
            Math.Min(Math.Max(50, pagerWidth - label.Width), Math.Max(50, (int)(90 * layout.Scale))), small);
        Text(batch, $"{_selected.Count} / {PartyLimit}", counter, UITextRole.Small, UITheme.InkMuted, true);
        var chips = PartyChips(layout);
        if (chips.Count == 0)
        {
            Text(batch, "Traveling alone", new Rectangle(band.Left, band.Top + small + 4,
                Math.Max(1, pagerWidth - gap), Math.Max(1, caption)), UITextRole.Caption,
                UITheme.InkMuted, true);
        }
        foreach (var (chipId, chipBounds) in chips)
        {
            var companion = CampaignCompanions.Get(chipId);
            Surface(batch, chipBounds, UIPaper.Face, true, 5);
            int iconSide = Math.Min(chipBounds.Height - 10, Math.Max(18, (int)(30 * layout.Scale)));
            var icon = new Rectangle(chipBounds.Left + 5, chipBounds.Center.Y - iconSide / 2,
                iconSide, iconSide);
            if (companion != null) UICompanionArt.Draw(batch, _resources, companion, icon);
            Text(batch, companion?.Name ?? chipId, new Rectangle(icon.Right + 8, chipBounds.Top,
                Math.Max(1, chipBounds.Width - iconSide - 34), chipBounds.Height),
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
        int caption = (int)layout.LineHeight(UITextRole.Caption);
        int gap = Math.Max(6, (int)(14 * layout.Scale));
        int top = band.Top + Math.Max(caption + 12, (int)(46 * layout.Scale));
        int height = Math.Max(28, Math.Min((int)(62 * layout.Scale), band.Bottom - top - 6));
        if (height < 20) return Array.Empty<(string, Rectangle)>();
        int x = band.Left;
        int right = PageCount > 1 ? layout.Previous.Left - gap * 2 : band.Right;
        float scale = UITypography.Scale(_captionFont, UITextRole.Small, _device.Viewport);
        var chips = new List<(string, Rectangle)>();
        foreach (string chipId in _selected)
        {
            var companion = CampaignCompanions.Get(chipId);
            string label = companion?.Name ?? chipId;
            float measured = _captionFont.MeasureString(UIFontGuard.Sanitize(_captionFont, label)).X;
            int width = Math.Clamp((int)(measured * scale) + height + (int)(40 * layout.Scale),
                110, Math.Max(120, (int)(320 * layout.Scale)));
            if (x + width > right) break;
            chips.Add((chipId, new Rectangle(x, top, width, height)));
            x += width + gap;
        }
        return chips;
    }

    private CampaignCompanion? CardCompanion(string cardId) => _roster.FirstOrDefault(companion =>
        string.Equals(companion.Id, cardId, StringComparison.OrdinalIgnoreCase));

    private string Caption(CampaignCompanion companion)
    {
        if (IsSelected(companion.Id)) return "in the party";
        if (_selected.Count >= PartyLimit) return "party full";
        return $"HP {companion.Health}";
    }

    private string Stats(CampaignCompanion companion)
    {
        string skills = companion.Skills.Count == 0
            ? "no skills"
            : string.Join(", ", companion.Skills.Select(GameText.SkillName));
        return $"HP {companion.Health}   ·   {skills}";
    }

    private string StateText(string companionId)
    {
        if (IsSelected(companionId)) return "In the party - click again to dismiss";
        if (_selected.Count >= PartyLimit) return "Party full - dismiss someone first";
        return "Not invited yet - click to invite";
    }

    private Color StateColor(string companionId) => _selected.Count >= PartyLimit
        ? UIPaper.MutedInk
        : IsSelected(companionId) ? UITheme.PanelHover : UITheme.InkMuted;
}
