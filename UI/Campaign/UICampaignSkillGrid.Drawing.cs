using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

public sealed partial class UICampaignSkillGrid
{
    private void Text(SpriteBatch batch, string text, Rectangle area, UITextRole role, Color color, bool left = false)
    {
        SpriteFont font = role == UITextRole.Heading ? _titleFont : role == UITextRole.Name ? _titleFont :
            _font;
        string safe = UIFontGuard.Sanitize(font, text);
        Vector2 size = font.MeasureString(safe);
        float scale = UITextRenderer.FitScale(size, area.Width, area.Height,
            LoadoutLineHeight(role) / font.LineSpacing);
        Vector2 position = new(left ? area.Left : area.Center.X - size.X * scale / 2, area.Center.Y - size.Y * scale / 2);
        UITextRenderer.DrawString(batch, font, safe, position, color, scale: scale);
    }
    // Match the Campaign route's type scale; keep dense metadata quieter than names.
    private float LoadoutLineHeight(UITextRole role)
    {
        if (Layout.Portrait) return UITypography.LineHeight(role, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height) * (role == UITextRole.Heading ? 1f : 1.15f);
        float s = new CampaignMapLayout(_graphicsDevice.Viewport).Scale;
        return (role switch { UITextRole.Heading => 51f, UITextRole.Name => 40f,
            UITextRole.Body => 42f, UITextRole.Caption => 34f, _ => 30f }) * s;
    }
    private void Rule(SpriteBatch batch, int x, int y, int width) => batch.Draw(_pixel, new Rectangle(x,y,width,1), UITheme.Line);

    /// <summary>One SP diamond; the same mark is used for a tile's cost, the budget and pages.</summary>
    private void Diamond(SpriteBatch batch, Vector2 center, float size, Color color) =>
        batch.Draw(_pixel, center, null, color, MathHelper.PiOver4, new Vector2(.5f, .5f),
            new Vector2(size, size), SpriteEffects.None, 0f);

    /// <summary>A row of SP diamonds, right-anchored at <paramref name="right"/>.</summary>
    private void Pips(SpriteBatch batch, float right, float centerY, int count, Color color, float size)
    {
        if (count <= 0) return;
        float step = size * 1.8f;
        for (int i = 0; i < count; i++)
            Diamond(batch, new Vector2(right - size * .7f - (count - 1 - i) * step, centerY), size, color);
    }

    /// <summary>Three-by-three dot grid: the "all categories" tab.</summary>
    private void DrawAllGlyph(SpriteBatch batch, Rectangle area, Color color)
    {
        float step = Math.Max(5f, area.Width / 3.4f);
        float size = Math.Max(3f, step * .42f);
        for (int row = 0; row < 3; row++)
        for (int col = 0; col < 3; col++)
            Diamond(batch, new Vector2(area.Center.X + (col - 1) * step,
                area.Center.Y + (row - 1) * step), size, color);
    }

    private void Line(SpriteBatch batch, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 delta = end - start;
        if (delta.LengthSquared() < .01f) return;
        batch.Draw(_pixel, start, null, color, MathF.Atan2(delta.Y, delta.X), new Vector2(0f, .5f),
            new Vector2(delta.Length(), Math.Max(1f, thickness)), SpriteEffects.None, 0f);
    }
    private (float Hover, float Press) ActionMotion(Rectangle bounds, bool enabled)
    {
        if (_actionMotion.Count > 128) _actionMotion.Clear();
        _actionMotion.TryGetValue(bounds, out var state);
        _actionMotion[bounds] = (state.Hover, state.Press, enabled);
        return enabled && !InputBlocked ? (state.Hover, state.Press) : (0, 0);
    }
    private void ActionSurface(SpriteBatch batch, Rectangle bounds, Color color, bool enabled = true, int cut = 6)
    {
        var motion = ActionMotion(bounds, enabled);
        UIPaper.Button(batch, bounds, color, motion.Hover, motion.Press, enabled && !InputBlocked, cut: cut,
            shape: UIPaperShape.Ticket);
    }
    private void Button(SpriteBatch batch, Rectangle bounds, string label, bool enabled, bool primary = false)
    {
        ActionSurface(batch, bounds, enabled ? UIPaper.Face : UITheme.PanelLight, enabled);
        string display = label switch { "EQUIP SKILL" => "Equip skill", "REMOVE SKILL" => "Remove skill",
            "EQUIP" => "Equip", "REMOVE" => "Remove", "START BATTLE" => "Start battle",
            "START" => "Start battle", _ => label };
        Text(batch, display.Length>1 ? display.TrimEnd(' ','>') : display, new Rectangle(bounds.X+8,bounds.Y+3,Math.Max(1,bounds.Width-16),Math.Max(1,bounds.Height-6)),
            UITextRole.Name, enabled ? UIPaper.Ink : UITheme.InkMuted);
    }
    private string StateLabel(SkillType skill) => AvailabilityLabelOverride?.Invoke(skill) ?? Availability(skill) switch
    {
        CampaignSkillAvailability.Selected => "Equipped",
        CampaignSkillAvailability.Locked => "",
        CampaignSkillAvailability.OverBudget => "Over budget",
        CampaignSkillAvailability.Conflicting => "Conflict",
        _ => ""
    };

    private void DrawGridScreen(SpriteBatch batch, SkillSelectionLayout layout)
    {
        float s = layout.Scale;
        if (!layout.Portrait)
        {
            var campaign = new CampaignMapLayout(_graphicsDevice.Viewport);
            UIPaperFrame.Draw(batch, campaign.Element("HeaderStrip"), new Color(27,46,62), UIPaperFrameShape.Rectangle, 0);
        }
        var detailSheet = layout.Details;
        if (!layout.Portrait) detailSheet.Height = layout.Footer.Bottom - detailSheet.Top + 12;
        detailSheet.Inflate(8, 8);
        var far = detailSheet; far.Offset((int)(12*s), (int)(10*s));
        var near = detailSheet; near.Offset(-(int)(8*s), (int)(5*s));
        UIPaperFrame.Stack(batch, detailSheet, far, near, s);
        int gap = Math.Max(6,(int)(16*s));
        int small = (int)LoadoutLineHeight(UITextRole.Small);
        int caption = (int)LoadoutLineHeight(UITextRole.Caption);
        var backMotion = ActionMotion(layout.Back, true);
        UIIconButton.DrawBackButton(batch, _pixel, layout.Back, hoverAmount: backMotion.Hover,
            pressAmount: backMotion.Press, enabled: !InputBlocked);
        // Title on the left, budget on the right. The budget used to be drawn under the title
        // on phones, where the tab bar covered it; it now shares the header row and gets a
        // thin meter so the spent/budget pair reads at a glance.
        int budgetWidth = layout.Portrait ? (int)(layout.Header.Width * 0.34f) : (int)(470*s);
        var title = new Rectangle(layout.Header.Left, layout.Header.Top,
            Math.Max(1, layout.Header.Width - budgetWidth - gap), layout.Header.Height);
        Text(batch,UseUnifiedActionButton ? HeaderText : "Loadout",title,UITextRole.Heading,UITheme.Ink,true);
        int spent = CampaignSkillBalance.TotalCost(_selected);
        if (ShowSkillPoints)
        {
            var budget = new Rectangle(layout.Header.Right-budgetWidth,
                layout.Header.Center.Y-caption/2,budgetWidth,caption);
            // A diamond is one SP everywhere on this page (tiles, budget, pages), so the
            // header does not need the "SP" word: the numbers sit beside the mark.
            float pipSize = Math.Max(8f, 10f * s);
            string numbers = $"{spent} / {_node.SkillBudget}";
            SpriteFont numberFont = _font;
            float numberScale = LoadoutLineHeight(UITextRole.Caption) / numberFont.LineSpacing;
            Vector2 measured = numberFont.MeasureString(UIFontGuard.Sanitize(numberFont, numbers)) * numberScale;
            float numbersLeft = budget.Right - measured.X;
            Diamond(batch, new Vector2(numbersLeft - pipSize * 1.6f, budget.Center.Y), pipSize, UITheme.InkMuted);
            Text(batch,numbers,
                new Rectangle((int)numbersLeft, budget.Y, (int)Math.Ceiling(measured.X) + 2, budget.Height),
                UITextRole.Caption,UITheme.Ink,true);
            if (_node.SkillBudget > 0)
            {
                var meter = new Rectangle(budget.Left, budget.Bottom + 3, Math.Max(1, budget.Width - 2), 4);
                batch.Draw(_pixel, meter, UITheme.PanelDark);
                float fill = Math.Clamp(spent / (float)_node.SkillBudget, 0f, 1f);
                batch.Draw(_pixel, new Rectangle(meter.X, meter.Y,
                    Math.Max(1, (int)(meter.Width * fill)), meter.Height), UITheme.AccentBlue);
            }
        }
        // Category tabs are glyphs now: All is a dot grid, the three families reuse the
        // mark their skills already carry. The selected tab's cyan face is the label.
        for (int i=0;i<4;i++)
        {
            var tab = layout.Tab(i); bool active = (int)Category==i;
            var face = tab; face.Inflate(-3, -3);
            var tabMotion = ActionMotion(tab, true);
            UIPaper.Button(batch, face, active ? UITheme.Gold : UITheme.PanelLight,
                tabMotion.Hover, tabMotion.Press, !InputBlocked, cut: 5);
            Color ink = active ? UIPaper.Ink : UITheme.Ink;
            int side = Math.Min((int)(tab.Height*.58f),(int)(tab.Width*.40f));
            var icon = new Rectangle(tab.Center.X-side/2, tab.Center.Y-side/2, side, side);
            if (i == 0) DrawAllGlyph(batch, icon, ink);
            else UISkillArt.Draw(batch, i==1 ? SkillType.PlusAttack : i==2 ? SkillType.PlusDefence : SkillType.Bian,
                icon, ink);
        }
        var visible = _grid.PageItems;
        for (int i=0;i<visible.Count;i++) DrawTile(batch,layout.TileInPage(i,visible.Count),visible[i],layout);
        if (_grid.PageCount>1)
        {
            Button(batch,layout.Previous,"<",CurrentPage>0);
            Button(batch,layout.Next,">",CurrentPage+1<PageCount);
            // Page dots instead of "1 / 3": the diamond row reads as progress and keeps
            // the pager wordless.
            float dot = Math.Max(6f, 8f * s);
            float step = dot * 2.6f;
            float centerX = (layout.Previous.Right + layout.Next.Left) / 2f;
            float startX = centerX - (PageCount - 1) * step / 2f;
            for (int i = 0; i < PageCount; i++)
                Diamond(batch, new Vector2(startX + i * step, layout.Previous.Center.Y),
                    i == CurrentPage ? dot * 1.35f : dot,
                    i == CurrentPage ? UITheme.AccentBlue : UITheme.InkMuted * .65f);
        }
        if (_grid.HasFocus) DrawDetails(batch,layout,spent);
        else Text(batch, "No skills in this category", layout.Grid, UITextRole.Body, UITheme.InkMuted);
        Rule(batch,layout.Footer.Left,layout.Footer.Top,layout.Footer.Width);
        var equippedLabel = new Rectangle(layout.Chips.Left,layout.Chips.Top-caption-4,layout.Chips.Width,caption);
        if (UseUnifiedActionButton && !ShowSkillPoints)
        {
            Text(batch,"Ban selection",equippedLabel,UITextRole.Small,UITheme.InkMuted,true);
        }
        else
        {
            // A check plus the count, matching the check that marks an equipped tile.
            int mark = (int)Math.Min(caption * 1.6f, 40 * s);
            UIPaper.Checkmark(batch, new Rectangle(equippedLabel.Left,
                equippedLabel.Center.Y - mark / 2, mark, mark), UITheme.InkMuted);
            Text(batch,_selected.Count.ToString(),
                new Rectangle(equippedLabel.Left + mark + 8, equippedLabel.Top,
                    Math.Max(1, equippedLabel.Width - mark - 8), equippedLabel.Height),
                UITextRole.Caption,UITheme.InkMuted,true);
        }
        var chips = EquippedChips(layout);
        foreach (var chip in chips)
        {
            ActionSurface(batch, chip.Bounds, UIPaper.Face, CanToggle(chip.Skill), 5);
            int iconSize = Math.Min(chip.Bounds.Height-10,Math.Max(20,(int)(30*s)));
            UISkillArt.Draw(batch,chip.Skill,new Rectangle(chip.Bounds.Left+5,chip.Bounds.Center.Y-iconSize/2,iconSize,iconSize),UIPaper.Ink);
            // The chip removes a skill; its cost already sits on the tile and in the budget.
            Text(batch,GameText.SkillName(chip.Skill),
                new Rectangle(chip.Bounds.Left+iconSize+10,chip.Bounds.Top,chip.Bounds.Width-iconSize-54,chip.Bounds.Height),
                UITextRole.Small,UIPaper.Ink,true);
            Text(batch,"×",new Rectangle(chip.Bounds.Right-26,chip.Bounds.Top,22,chip.Bounds.Height),UITextRole.Caption,UIPaper.MutedInk);
        }
        var overflow = ChipOverflow(layout);
        if (overflow.Width>0) Button(batch,overflow,$"+{_selected.Count-chips.Count}",true);
        if (_selected.Count==0 && UseUnifiedActionButton && !ShowSkillPoints)
            Text(batch,"Inspect a skill, then confirm the ban.",layout.Chips,UITextRole.Small,UITheme.InkMuted,true);
        bool canStart = StartActionEnabled && !InputBlocked &&
            (!StartActionTogglesSelection || _grid.HasFocus);
        // The phone ticket reads CONTINUE; the ban/ready modes keep their own words.
        Button(batch,layout.Start,
            layout.Portrait && !StartActionTogglesSelection ? "CONTINUE" : StartActionText,
            canStart,true);

    }

    private void DrawTile(SpriteBatch batch, Rectangle bounds, SkillType skill, SkillSelectionLayout layout)
    {
        bool focused = FocusedSkill==skill;
        var availability = Availability(skill);
        bool selected = _selected.Contains(skill) || availability==CampaignSkillAvailability.Selected;
        bool unavailable = availability is CampaignSkillAvailability.Locked or CampaignSkillAvailability.OverBudget or CampaignSkillAvailability.Conflicting;
        int pad = Math.Max(7,(int)(18*layout.Scale));
        int caption = (int)LoadoutLineHeight(UITextRole.Small);
        int name = (int)LoadoutLineHeight(UITextRole.Name);
        if (focused)
        {
            var frame = bounds; frame.Inflate(3, 3);
            UIPaper.Sheet(batch, frame, UITheme.AccentBlue, 1, 7, shape: UIPaperShape.Ticket);
        }
        ActionSurface(batch, bounds, UIPaper.Face, !InputBlocked, 7);
        var nameArea = new Rectangle(bounds.Left+pad,bounds.Bottom-pad-caption-name-3,bounds.Width-pad*2,name);
        var art = new Rectangle(bounds.Left+pad,bounds.Top+pad,bounds.Width-pad*2,Math.Max(8,nameArea.Top-bounds.Top-pad*2));
        UISkillArt.Draw(batch,skill,art,unavailable ? UIPaper.MutedInk : UIPaper.Ink);
        Text(batch,GameText.SkillName(skill),nameArea,UITextRole.Name,UIPaper.Ink);
        // One stat per tile: one diamond per SP. The inspector spells out "SP" once, so
        // the grid stays wordless and the cost is read at a glance.
        Pips(batch, bounds.Right-pad, bounds.Bottom-pad-caption*.45f,
            CampaignSkillBalance.Cost(skill),
            unavailable ? UIPaper.MutedInk * .55f : UIPaper.MutedInk,
            Math.Max(8f, 11f*layout.Scale));
        if (selected || availability==CampaignSkillAvailability.Locked)
        {
            int side = Math.Max(14,(int)(24*layout.Scale));
            UISkillSymbols.Status(batch,_pixel,new Rectangle(bounds.Right-pad-side,bounds.Top+pad,side,side),
                selected ? CampaignSkillAvailability.Selected : CampaignSkillAvailability.Locked,selected ? UITheme.PanelHover : UIPaper.MutedInk);
        }
    }

    private void DrawDetails(SpriteBatch batch, SkillSelectionLayout layout, int spent)
    {
        var skill = FocusedSkill; var state = Availability(skill); var (family,description) = CampaignSkillPresentation.Describe(skill);
        var bounds = layout.Details; int pad = Math.Max(12,(int)(22*layout.Scale));
        int small = (int)LoadoutLineHeight(UITextRole.Caption);
        int heading = (int)LoadoutLineHeight(UITextRole.Heading);
        int contentLeft = bounds.Left+pad, contentWidth = bounds.Width-pad*2;
        int iconSize = Math.Clamp((int)(bounds.Height*.18f),50,(int)(180*layout.Scale));
        var art = new Rectangle(contentLeft,bounds.Top+pad,iconSize,iconSize);
        UISkillArt.Draw(batch,skill,art,UITheme.Ink);
        int titleTop = art.Top;
        int titleLeft = art.Right+pad;
        // On a phone the equip ticket lives in the header, so the title stops before it and
        // the description gets the full width underneath. Desktop keeps its bottom ticket.
        bool actionOnTop = layout.Action.Top < bounds.Center.Y;
        int titleRight = actionOnTop ? layout.Action.Left - pad : bounds.Right - pad;
        int titleWidth = Math.Max(1, titleRight - titleLeft);
        Text(batch,GameText.SkillName(skill),new Rectangle(titleLeft,titleTop,titleWidth,heading),UITextRole.Heading,UITheme.Ink,true);
        int costY = titleTop+heading+4;
        // The same diamond as the tiles and the budget: one diamond, one SP; the digit
        // follows the mark instead of a word.
        float costPip = Math.Max(8f, 10f*layout.Scale);
        Diamond(batch, new Vector2(titleLeft + costPip, costY + small * .5f), costPip, UITheme.InkMuted);
        Text(batch,CampaignSkillBalance.Cost(skill).ToString(),
            new Rectangle(titleLeft + (int)(costPip * 2.6f), costY,
                Math.Max(1, titleWidth - (int)(costPip * 2.6f)), small),
            UITextRole.Caption,UITheme.InkMuted,true);
        int ruleY = Math.Max(art.Bottom,costY+small)+pad;
        Rule(batch,contentLeft,ruleY,contentWidth);
        // The inspector stacks: header, description, status, then the equipped chips at its
        // foot. Desktop keeps the old bottom ticket and the chips in the footer column.
        int statusY = actionOnTop
            ? Math.Max(ruleY+pad, layout.Chips.Top - small*2 - 8)
            : Math.Max(ruleY+pad, layout.Action.Top - small - pad);
        int descriptionTop = ruleY+pad;
        var descriptionArea = new Rectangle(contentLeft,descriptionTop,contentWidth,Math.Max(1,statusY-pad-descriptionTop));
        DrawDescriptionCentered(batch,description,descriptionArea);
        string status = state==CampaignSkillAvailability.OverBudget ? $"Over budget by {spent+CampaignSkillBalance.Cost(skill)-_node.SkillBudget} SP" :
            state is CampaignSkillAvailability.Locked or CampaignSkillAvailability.Conflicting ? StateLabel(skill) :
            "";
        Text(batch,ShowSkillPoints ? status : StateLabel(skill),new Rectangle(contentLeft,statusY,contentWidth,small),UITextRole.Caption,UITheme.InkMuted);
        if (!StartActionTogglesSelection)
        {
            // On a phone the ticket carries a plus/minus mark instead of a word; the desktop
            // keeps its long ticket, where a lone mark would look lost.
            if (layout.Portrait)
                ToggleGlyph(batch, layout.Action, state==CampaignSkillAvailability.Selected, CanToggle(skill));
            else
                Button(batch,layout.Action,state==CampaignSkillAvailability.Selected ? SelectedActionText : AvailableActionText,CanToggle(skill));
        }
    }

    /// <summary>Add/remove stamp for the phone ticket: a plus to equip, a minus to remove.</summary>
    private void ToggleGlyph(SpriteBatch batch, Rectangle bounds, bool selected, bool enabled)
    {
        ActionSurface(batch, bounds, enabled ? UIPaper.Face : UITheme.PanelLight, enabled, 6);
        Color ink = enabled ? UIPaper.Ink : UITheme.InkMuted;
        int arm = Math.Max(7, Math.Min(bounds.Width, bounds.Height) / 4);
        int thickness = Math.Max(3, arm / 3);
        Vector2 center = bounds.Center.ToVector2();
        batch.Draw(_pixel, new Rectangle((int)center.X - arm, (int)center.Y - thickness / 2,
            arm * 2, thickness), ink);
        if (!selected)
            batch.Draw(_pixel, new Rectangle((int)center.X - thickness / 2, (int)center.Y - arm,
                thickness, arm * 2), ink);
    }


    /// <summary>
    /// The inspector's effect sentence, centred in the space between the rule and the
    /// equipped chips. A one-line effect then leaves air above and below instead of a
    /// hole before the chips.
    /// </summary>
    private void DrawDescriptionCentered(SpriteBatch batch, string text, Rectangle area)
    {
        if (area.Width <= 0 || area.Height <= 0 || string.IsNullOrWhiteSpace(text)) return;
        float scale = UITypography.Scale(_font, UITextRole.Body, _graphicsDevice.Viewport);
        IReadOnlyList<string> lines;
        float lineHeight;
        for (int attempt = 0; ; attempt++)
        {
            lines = UITextRenderer.WrapLines(UIFontGuard.Sanitize(_font, text), area.Width,
                value => _font.MeasureString(value).X * scale);
            lineHeight = _font.LineSpacing * scale * 1.2f;
            if (lines.Count * lineHeight <= area.Height || attempt >= 40) break;
            scale *= 0.95f;
        }
        float y = area.Top + Math.Max(0f, (area.Height - lines.Count * lineHeight) / 2f);
        foreach (string line in lines)
        {
            UITextRenderer.DrawString(batch, _font, line, new Vector2(area.Left, y), UITheme.Ink, scale: scale);
            y += lineHeight;
        }
    }

}
