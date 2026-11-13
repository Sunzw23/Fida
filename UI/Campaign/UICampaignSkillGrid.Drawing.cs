using System;
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
        string display = label switch { "EQUIP SKILL" => "Equip skill", "REMOVE SKILL" => "Remove skill", "START BATTLE" => "Start battle", _ => label };
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
        if (layout.Portrait) UIPaperFrame.Draw(batch, layout.Footer, UITheme.PanelDark, UIPaperFrameShape.Node, s);
        int gap = Math.Max(6,(int)(16*s));
        int small = (int)LoadoutLineHeight(UITextRole.Small);
        int caption = (int)LoadoutLineHeight(UITextRole.Caption);
        var backMotion = ActionMotion(layout.Back, true);
        UIIconButton.DrawBackButton(batch, _pixel, layout.Back, hoverAmount: backMotion.Hover,
            pressAmount: backMotion.Press, enabled: !InputBlocked);
        int budgetWidth = layout.Portrait ? layout.Header.Width : (int)(470*s);
        Rectangle title = new(layout.Header.Left, layout.Header.Top,
            layout.Portrait ? layout.Header.Width : layout.Header.Width-budgetWidth-gap, layout.Header.Height);
        Text(batch,UseUnifiedActionButton ? HeaderText : "Loadout",title,UITextRole.Heading,UITheme.Ink,true);
        // The campaign node name is context, not a second page heading.
        var budget = new Rectangle(layout.Portrait ? layout.Header.Left : layout.Header.Right-budgetWidth,
            layout.Portrait ? title.Bottom : title.Center.Y-caption/2,budgetWidth,caption);
        int spent = CampaignSkillBalance.TotalCost(_selected);
        if (ShowSkillPoints)
        {
            Text(batch,$"{spent} / {_node.SkillBudget} SP",
                new Rectangle(budget.X,budget.Y,layout.Portrait ? budget.Width*55/100 : budget.Width,budget.Height),UITextRole.Caption,UITheme.Ink,true);
        }
        string[] tabs = { "All", "Offense", "Defense", "Control" };
        for (int i=0;i<4;i++)
        {
            var tab = layout.Tab(i); bool active = (int)Category==i;
            var face = tab; face.Inflate(-3, -3);
            var tabMotion = ActionMotion(tab, true);
            UIPaper.Button(batch, face, active ? UITheme.Gold : UITheme.PanelLight,
                tabMotion.Hover, tabMotion.Press, !InputBlocked, cut: 5);
            Text(batch,tabs[i],tab,UITextRole.Caption,active ? UIPaper.Ink : UITheme.Ink);
        }
        var visible = _grid.PageItems;
        for (int i=0;i<visible.Count;i++) DrawTile(batch,layout.Tile(i),visible[i],layout);
        if (_grid.PageCount>1)
        {
            Button(batch,layout.Previous,"<",CurrentPage>0);
            Button(batch,layout.Next,">",CurrentPage+1<PageCount);
            Text(batch,$"{CurrentPage+1} / {PageCount}",new Rectangle(layout.Previous.Right,layout.Previous.Y,
                layout.Next.Left-layout.Previous.Right,layout.Previous.Height),UITextRole.Small,UITheme.InkMuted);
        }
        if (_grid.HasFocus) DrawDetails(batch,layout,spent);
        else
        {
            Text(batch, "No skills in this category", layout.Grid, UITextRole.Body, UITheme.InkMuted);
            Text(batch, "Choose another category.", layout.Details, UITextRole.Body, UITheme.InkMuted);
        }
        Rule(batch,layout.Footer.Left,layout.Footer.Top,layout.Footer.Width);
        Text(batch,UseUnifiedActionButton && !ShowSkillPoints ? "Ban selection" : $"Equipped  {_selected.Count}",new Rectangle(layout.Footer.Left,layout.Footer.Top+3,layout.Chips.Width,caption),
            UITextRole.Small,UITheme.InkMuted,true);
        var chips = EquippedChips(layout);
        foreach (var chip in chips)
        {
            ActionSurface(batch, chip.Bounds, UIPaper.Face, CanToggle(chip.Skill), 5);
            int iconSize = Math.Min(chip.Bounds.Height-10,Math.Max(20,(int)(30*s)));
            UISkillArt.Draw(batch,chip.Skill,new Rectangle(chip.Bounds.Left+5,chip.Bounds.Center.Y-iconSize/2,iconSize,iconSize),UIPaper.Ink);
            Text(batch,$"{GameText.SkillName(chip.Skill)}  {CampaignSkillBalance.Cost(chip.Skill)} SP",
                new Rectangle(chip.Bounds.Left+iconSize+10,chip.Bounds.Top,chip.Bounds.Width-iconSize-34,chip.Bounds.Height),
                UITextRole.Small,UIPaper.Ink,true);
            Text(batch,"×",new Rectangle(chip.Bounds.Right-26,chip.Bounds.Top,22,chip.Bounds.Height),UITextRole.Caption,UIPaper.MutedInk);
        }
        var overflow = ChipOverflow(layout);
        if (overflow.Width>0) Button(batch,overflow,$"+{_selected.Count-chips.Count}",true);
        if (_selected.Count==0) Text(batch,UseUnifiedActionButton && !ShowSkillPoints ? "Inspect a skill, then confirm the ban." :
            "Choose skills within the budget.",layout.Chips,UITextRole.Small,UITheme.InkMuted,true);
        Button(batch,layout.Start,StartActionText,StartActionEnabled && !InputBlocked && (!StartActionTogglesSelection || _grid.HasFocus),true);
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
        string state = selected && AvailabilityLabelOverride == null ? "" : StateLabel(skill);
        int costWidth = Math.Min(bounds.Width/3,Math.Max(44,(int)(74*layout.Scale)));
        Text(batch,state,new Rectangle(bounds.Left+pad,bounds.Bottom-pad-caption,bounds.Width-pad*2-costWidth,caption),
            UITextRole.Small,selected ? UITheme.PanelHover : UIPaper.MutedInk,true);
        Text(batch,$"{CampaignSkillBalance.Cost(skill)} SP",new Rectangle(bounds.Right-pad-costWidth,bounds.Bottom-pad-caption,costWidth,caption),
            UITextRole.Small,UIPaper.MutedInk);
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
        var art = new Rectangle(contentLeft,bounds.Top+8,iconSize,iconSize);
        UISkillArt.Draw(batch,skill,art,UITheme.Ink);
        int titleTop = art.Top;
        int titleLeft = art.Right+pad;
        int titleWidth = bounds.Right-pad-titleLeft;
        Text(batch,GameText.SkillName(skill),new Rectangle(titleLeft,titleTop,titleWidth,heading),UITextRole.Heading,UITheme.Ink,true);
        int costY = titleTop+heading+4;
        Text(batch,$"{CampaignSkillBalance.Cost(skill)} SP",new Rectangle(titleLeft,costY,titleWidth,small),UITextRole.Caption,UITheme.InkMuted,true);
        int ruleY = Math.Max(art.Bottom,costY+small)+pad;
        Rule(batch,contentLeft,ruleY,contentWidth);
        int budgetY = layout.Action.Top-small-pad;
        int descriptionTop = ruleY+pad;
        var descriptionArea = new Rectangle(contentLeft,descriptionTop,contentWidth,Math.Max(1,budgetY-pad-descriptionTop));
        UITextRenderer.DrawParagraph(batch,_font,description,descriptionArea,UITheme.Ink);
        string status = state==CampaignSkillAvailability.OverBudget ? $"Over budget by {spent+CampaignSkillBalance.Cost(skill)-_node.SkillBudget} SP" :
            state is CampaignSkillAvailability.Locked or CampaignSkillAvailability.Conflicting ? StateLabel(skill) :
            "";
        Text(batch,ShowSkillPoints ? status : StateLabel(skill),new Rectangle(contentLeft,budgetY,contentWidth,small),UITextRole.Caption,UITheme.InkMuted,true);
        if (!StartActionTogglesSelection)
            Button(batch,layout.Action,state==CampaignSkillAvailability.Selected ? SelectedActionText : AvailableActionText,CanToggle(skill));
    }

}
