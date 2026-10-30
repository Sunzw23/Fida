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
        SpriteFont font = role == UITextRole.Heading ? _titleFont : role == UITextRole.Name ? _labelFont :
            role == UITextRole.Body ? _font : _captionFont;
        string safe = UIFontGuard.Sanitize(font, text);
        Vector2 size = font.MeasureString(safe);
        float scale = UITextRenderer.FitScale(size, area.Width, area.Height,
            UITypography.Scale(font, role, _graphicsDevice.Viewport));
        Vector2 position = new(left ? area.Left : area.Center.X - size.X * scale / 2, area.Center.Y - size.Y * scale / 2);
        UITextRenderer.DrawString(batch, font, safe, position, color, scale: scale);
    }
    private void Rule(SpriteBatch batch, int x, int y, int width) => batch.Draw(_pixel, new Rectangle(x,y,width,1), UITheme.Line);
    private void Button(SpriteBatch batch, Rectangle bounds, string label, bool enabled, bool primary = false)
    {
        bool hover = bounds.Contains(_previousMouse.Position) && enabled && !InputBlocked;
        UIPaper.Sheet(batch, bounds, enabled ? hover ? UITheme.Gold : UIPaper.Face : UITheme.PanelLight, 3, 6);
        Text(batch, label.Length>1 ? label.TrimEnd(' ','>') : label, new Rectangle(bounds.X+8,bounds.Y+3,Math.Max(1,bounds.Width-16),Math.Max(1,bounds.Height-6)),
            UITextRole.Name, enabled ? UIPaper.Ink : UITheme.InkMuted);
    }
    private string StateLabel(SkillType skill) => AvailabilityLabelOverride?.Invoke(skill) ?? Availability(skill) switch
    {
        CampaignSkillAvailability.Selected => "Equipped",
        CampaignSkillAvailability.Locked => "Locked",
        CampaignSkillAvailability.OverBudget => "Over budget",
        CampaignSkillAvailability.Conflicting => "Conflict",
        _ => ""
    };

    private void DrawGridScreen(SpriteBatch batch, SkillSelectionLayout layout)
    {
        float s = layout.Scale;
        UIPaper.Sheet(batch, layout.Panel, UITheme.PanelDark, 4, 12);
        var detailSheet = layout.Details; detailSheet.Inflate(8, 8);
        UIPaper.Sheet(batch, detailSheet, UITheme.Panel, 3, 9);
        UIPaper.Sheet(batch, layout.Footer, UITheme.Panel, 2, 7);
        int gap = Math.Max(6,(int)(16*s));
        int small = (int)UITypography.LineHeight(UITextRole.Small,_graphicsDevice.Viewport.Width,_graphicsDevice.Viewport.Height);
        int caption = (int)UITypography.LineHeight(UITextRole.Caption,_graphicsDevice.Viewport.Width,_graphicsDevice.Viewport.Height);
        UIIconButton.DrawBackButton(batch,_pixel,layout.Back,layout.Back.Contains(_previousMouse.Position));
        int budgetWidth = layout.Portrait ? layout.Header.Width : (int)(470*s);
        Rectangle title = new(layout.Header.Left,layout.Header.Top,
            layout.Portrait ? layout.Header.Width : layout.Header.Width-budgetWidth-gap,Math.Max(28,(int)(45*s)));
        Text(batch,UseUnifiedActionButton ? HeaderText : "Loadout",title,UITextRole.Heading,UITheme.Ink,true);
        Text(batch,string.IsNullOrEmpty(StatusText) ? UseUnifiedActionButton ? "Choose a skill" : HeaderText : StatusText,
            new Rectangle(title.Left,title.Bottom+4,title.Width,small+4),UITextRole.Small,UITheme.InkMuted,true);
        var budget = new Rectangle(layout.Portrait ? layout.Header.Left : layout.Header.Right-budgetWidth,
            layout.Portrait ? title.Bottom+small+12 : layout.Header.Top,budgetWidth,caption);
        int spent = CampaignSkillBalance.TotalCost(_selected);
        if (ShowSkillPoints)
        {
            Text(batch,$"USED  {spent} / {_node.SkillBudget} SP",
                new Rectangle(budget.X,budget.Y,layout.Portrait ? budget.Width*55/100 : budget.Width,budget.Height),UITextRole.Caption,UITheme.Ink,true);
            int segments = Math.Clamp(_node.SkillBudget,1,16);
            int trackWidth = layout.Portrait ? Math.Min(budget.Width/2,400) : budget.Width;
            int segmentGap = Math.Max(2,(int)(5*s));
            int segmentWidth = Math.Max(1,(trackWidth-(segments-1)*segmentGap)/segments);
            int left = layout.Portrait ? budget.Right-trackWidth : budget.Left;
            int y = layout.Portrait ? budget.Y+(caption-8)/2 : budget.Bottom+8;
            for (int i=0;i<segments;i++)
            {
                var cell = new Rectangle(left+i*(segmentWidth+segmentGap),y,segmentWidth,Math.Max(5,(int)(12*s)));
                float filled = _node.SkillBudget <= 0 ? 0 : spent/(float)_node.SkillBudget*segments-i;
                batch.Draw(_pixel,cell,filled>=1 ? UITheme.Gold : UITheme.PanelLight);
                UIUtils.DrawRectangleOutlines(batch,cell,UITheme.Line,1,_pixel);
                if (filled>0 && filled<1) batch.Draw(_pixel,new Rectangle(cell.X,cell.Y,(int)(cell.Width*filled),cell.Height),UITheme.Gold);
            }
        }
        if (!layout.Portrait) Rule(batch,layout.Panel.Left,layout.Tabs.Top-gap,layout.Panel.Width);
        string[] tabs = { "ALL", "OFFENSE", "DEFENSE", "CONTROL" };
        for (int i=0;i<4;i++)
        {
            var tab = layout.Tab(i); bool active = (int)Category==i;
            var face = tab; face.Inflate(-3, -3);
            UIPaper.Sheet(batch, face, active ? UITheme.Gold : UITheme.PanelLight, 2, 5);
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
        if (!layout.Portrait)
            batch.Draw(_pixel,new Rectangle(layout.Details.Left-gap,layout.Details.Top,1,layout.Details.Height),UITheme.Line);
        DrawDetails(batch,layout,spent);
        Rule(batch,layout.Footer.Left,layout.Footer.Top,layout.Footer.Width);
        Text(batch,UseUnifiedActionButton && !ShowSkillPoints ? "BAN SELECTION" : $"EQUIPPED  {_selected.Count}",new Rectangle(layout.Footer.Left,layout.Footer.Top+3,layout.Chips.Width,caption),
            UITextRole.Small,UITheme.InkMuted,true);
        var chips = EquippedChips(layout);
        foreach (var chip in chips)
        {
            UIPaper.Sheet(batch,chip.Bounds,UIPaper.Face,2,5);
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
        Button(batch,layout.Start,StartActionText,StartActionEnabled && !InputBlocked,true);
    }

    private void DrawTile(SpriteBatch batch, Rectangle bounds, SkillType skill, SkillSelectionLayout layout)
    {
        bool focused = FocusedSkill==skill;
        var availability = Availability(skill);
        bool selected = _selected.Contains(skill) || availability==CampaignSkillAvailability.Selected;
        bool unavailable = availability is CampaignSkillAvailability.Locked or CampaignSkillAvailability.OverBudget or CampaignSkillAvailability.Conflicting;
        int pad = Math.Max(7,(int)(18*layout.Scale));
        int caption = (int)UITypography.LineHeight(UITextRole.Small,_graphicsDevice.Viewport.Width,_graphicsDevice.Viewport.Height);
        int name = (int)UITypography.LineHeight(UITextRole.Name,_graphicsDevice.Viewport.Width,_graphicsDevice.Viewport.Height);
        UIPaper.Sheet(batch,bounds,focused ? UITheme.Gold : unavailable ? Color.Lerp(UIPaper.Face,UITheme.PanelLight,.22f) : UIPaper.Face,focused ? 4 : 2,7);
        var nameArea = new Rectangle(bounds.Left+pad,bounds.Bottom-pad-caption-name-3,bounds.Width-pad*2,name);
        var art = new Rectangle(bounds.Left+pad,bounds.Top+pad,bounds.Width-pad*2,Math.Max(8,nameArea.Top-bounds.Top-pad*2));
        UISkillArt.Draw(batch,skill,art,unavailable ? UIPaper.MutedInk : UIPaper.Ink);
        Text(batch,GameText.SkillName(skill),nameArea,UITextRole.Name,UIPaper.Ink,true);
        string state = StateLabel(skill);
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
        int small = (int)UITypography.LineHeight(UITextRole.Caption,_graphicsDevice.Viewport.Width,_graphicsDevice.Viewport.Height);
        int heading = (int)UITypography.LineHeight(UITextRole.Heading,_graphicsDevice.Viewport.Width,_graphicsDevice.Viewport.Height);
        int contentLeft = bounds.Left+pad, contentWidth = bounds.Width-pad*2;
        Text(batch,family,new Rectangle(contentLeft,bounds.Top,contentWidth,small),UITextRole.Caption,UITheme.InkMuted,true);
        int iconSize = Math.Clamp((int)(bounds.Height*.18f),50,(int)(180*layout.Scale));
        var art = new Rectangle(layout.Portrait ? contentLeft : bounds.Center.X-iconSize/2,bounds.Top+small+8,iconSize,iconSize);
        UISkillArt.Draw(batch,skill,art,UITheme.Ink);
        int titleTop = layout.Portrait ? art.Top : art.Bottom+Math.Max(5,(int)(14*layout.Scale));
        int titleLeft = layout.Portrait ? art.Right+pad : contentLeft;
        int titleWidth = layout.Portrait ? bounds.Right-pad-titleLeft : contentWidth;
        Text(batch,GameText.SkillName(skill),new Rectangle(titleLeft,titleTop,titleWidth,heading),UITextRole.Heading,UITheme.Ink);
        int costY = titleTop+heading+4;
        Text(batch,$"{CampaignSkillBalance.Cost(skill)} SP",new Rectangle(titleLeft,costY,titleWidth,small),UITextRole.Caption,UITheme.Ink);
        int ruleY = Math.Max(art.Bottom,costY+small)+pad;
        Rule(batch,contentLeft,ruleY,contentWidth);
        int previewHeight = Math.Max(65,(int)(110*layout.Scale));
        int budgetY = layout.Action.Top-small-pad;
        int previewY = budgetY-previewHeight-pad;
        int descriptionTop = ruleY+pad;
        var descriptionArea = new Rectangle(contentLeft,descriptionTop,contentWidth,Math.Max(1,previewY-pad-descriptionTop));
        UITextRenderer.DrawParagraph(batch,_font,description,descriptionArea,UITheme.Ink);
        DrawExample(batch,skill,new Rectangle(contentLeft,previewY,contentWidth,previewHeight));
        string budget = state==CampaignSkillAvailability.OverBudget ? $"Over budget by {spent+CampaignSkillBalance.Cost(skill)-_node.SkillBudget} SP" :
            state is CampaignSkillAvailability.Locked or CampaignSkillAvailability.Conflicting ? StateLabel(skill) :
            $"After {(_selected.Contains(skill) ? "removal" : "equip")}: {spent+(_selected.Contains(skill) ? -1 : 1)*CampaignSkillBalance.Cost(skill)} / {_node.SkillBudget} SP";
        Text(batch,ShowSkillPoints ? budget : StateLabel(skill),new Rectangle(contentLeft,budgetY,contentWidth,small),UITextRole.Caption,UITheme.InkMuted,true);
        if (!StartActionTogglesSelection)
            Button(batch,layout.Action,state==CampaignSkillAvailability.Selected ? SelectedActionText : AvailableActionText,CanToggle(skill));
    }

    private void DrawExample(SpriteBatch batch, SkillType skill, Rectangle area)
    {
        var example = skill switch
        {
            SkillType.PlusAttack => ("BASE","2","DAMAGE","3"),
            SkillType.DoubleAttack => ("BASE","2","DAMAGE","4"),
            SkillType.PlusDefence => ("INCOMING","3","TAKEN","2"),
            SkillType.DoubleDefence => ("INCOMING","4","TAKEN","2"),
            SkillType.DoubleHealth => ("BASE HP","11","MAX HP","22"),
            SkillType.TripleHealth => ("BASE HP","11","MAX HP","33"),
            SkillType.Xue => ("DAMAGE","3","HEAL","3"),
            _ => ("","","","")
        };
        if (example.Item2.Length==0)
        {
            Text(batch,"CONDITIONAL EFFECT",area,UITextRole.Caption,UITheme.InkMuted);
            return;
        }
        int labelHeight = Math.Min(24,area.Height/3);
        int width = area.Width*35/100;
        var left = new Rectangle(area.Left,area.Top+labelHeight,width,area.Height-labelHeight);
        var right = new Rectangle(area.Right-width,left.Top,width,left.Height);
        Text(batch,example.Item1,new Rectangle(left.X,area.Y,width,labelHeight),UITextRole.Small,UITheme.InkMuted);
        Text(batch,example.Item3,new Rectangle(right.X,area.Y,width,labelHeight),UITextRole.Small,UITheme.InkMuted);
        UIPaper.Sheet(batch,left,UIPaper.Face,2,5); UIPaper.Sheet(batch,right,UITheme.Gold,2,5);
        UIAngularNumber.Draw(batch,example.Item2,new Rectangle(left.X+8,left.Y+6,left.Width-16,left.Height-12),UIPaper.Ink);
        UIAngularNumber.Draw(batch,example.Item4,new Rectangle(right.X+8,right.Y+6,right.Width-16,right.Height-12),UIPaper.Ink);
        Vector2 center = new((left.Right+right.Left)/2f,left.Center.Y);
        batch.Draw(_pixel,new Rectangle((int)center.X-18,(int)center.Y-1,24,2),UITheme.Ink);
        UISkillSymbols.Play(batch,_pixel,center,UITheme.Ink);
    }
}
