#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

public sealed record CampaignRouteDetails(string Id, string Title, string Story, int EnemyCount,
    int SkillBudget, string Reward, SkillType? RewardSkill, string Chapter, bool Locked = false,
    int Tier = 0, IReadOnlyList<CampaignVow>? Vows = null, bool Side = false);
public sealed record CampaignChapterDetails(string Id, string Title, string Story, int Cleared,
    int Total, bool Side = false);

/// <summary>Route visuals only. Receives read-only display data and bounds; owns no campaign or input state.</summary>
public sealed class UICampaignRouteView
{
    private readonly SpriteFont _body, _heading;
    private readonly Texture2D _pixel, _line;
    private static readonly Color Header = new(27,46,62);
    private static readonly Color Cyan = new(142,222,226);
    /// <summary>Unavailable sheets use one grey stock at every level of the browser.</summary>
    private static readonly Color LockedFace = new(160,173,176);
    /// <summary>Artwork on a locked sheet is faded towards cold grey, never left in full colour.</summary>
    private static readonly Color LockedWashColor = new(104,118,124);
    private const float LockedWash = .58f;

    public UICampaignRouteView(SpriteFont body, SpriteFont heading, Texture2D pixel, Texture2D line)
    { _body = body; _heading = heading; _pixel = pixel; _line = line; }

    private void Text(SpriteBatch batch, string text, Rectangle area, float height, Color color, bool heading = false, bool center = false)
    {
        SpriteFont font = heading ? _heading : _body;
        string safe = UIFontGuard.Sanitize(font, text);
        Vector2 size = font.MeasureString(safe);
        float scale = UITextRenderer.FitScale(size, area.Width, area.Height, height / font.LineSpacing);
        UITextRenderer.DrawString(batch, font, safe,
            new Vector2(center ? area.Center.X - size.X * scale / 2 : area.Left, area.Center.Y - size.Y * scale / 2), color, scale: scale);
    }

    public void DrawHeader(SpriteBatch batch, CampaignMapLayout layout, string? chapter = null,
        ChapterStyle? style = null)
    {
        float s = layout.Scale;
        UIPaperFrame.Draw(batch, layout.Element("HeaderStrip"), Header, UIPaperFrameShape.Rectangle, 0);
        Text(batch, "Campaign", layout.Header, 60 * s, UITheme.Ink, true);
        if (string.IsNullOrWhiteSpace(chapter)) return;
        var look = style ?? ChapterStyle.Default;
        var area = layout.Header;
        int offset = (int)(_heading.MeasureString("Campaign").X * (60 * s / _heading.LineSpacing)) + (int)(26 * s);
        area.X += offset;
        area.Width = Math.Max(1, area.Width - offset);
        Text(batch, "/  " + chapter, area, 34 * s,
            look.Side ? Lighten(look.Accent) : UITheme.InkMuted);
    }

    private static void DrawPanel(SpriteBatch batch, CampaignMapLayout layout)
    {
        UIPaperFrame.Stack(batch,layout.Details,layout.Element("BackingFar"),layout.Element("BackingNear"),layout.Scale);
    }

    public void DrawChapterDetails(SpriteBatch batch, CampaignMapLayout layout, CampaignChapterDetails detail,
        float hover, float press, ChapterStyle? style = null)
    {
        var look = style ?? ChapterStyle.Default;
        float s=layout.Scale;
        Rectangle E(string name)=>layout.Element(name);
        DrawPanel(batch,layout);
        string eyebrow = detail.Side ? $"{detail.Id}   SIDE CHAPTER" : detail.Id;
        Text(batch,eyebrow,E("NodeId"),27*s,detail.Side ? Lighten(look.Accent) : UITheme.InkMuted);
        Text(batch,detail.Title,E("Title"),51*s,UITheme.Ink,true);
        if (detail.Side)
            batch.Draw(_pixel,new Rectangle(E("Title").Left,E("Title").Bottom-(int)(4*s),(int)(150*s),Math.Max(2,(int)(4*s))),look.Accent);
        var image=E("Image");image.Y+=(int)(38*s);image.Height-=(int)(38*s);
        UIPaper.ChapterLocation(batch,image,string.IsNullOrWhiteSpace(look.ArtKey) ? detail.Title : look.ArtKey);
        foreach(string divider in new[]{"DividerImage","DividerStory","DividerMetric"})
            batch.Draw(_pixel,E(divider),new Color(77,94,101)*.65f);
        Paragraph(batch,detail.Story,E("Story"),38*s);
        Text(batch,$"{detail.Cleared} / {detail.Total} cleared",E("Metric"),33*s,UITheme.Ink);
        DrawAction(batch,layout,"Open chapter",hover,press);
    }

    private static Color Lighten(Color color) => Color.Lerp(color, Color.White, .35f);

    private void DrawAction(SpriteBatch batch,CampaignMapLayout layout,string label,float hover,float press,
        bool enabled=true)
    {
        float s=layout.Scale;
        // Sheet tints come from the paper palette, so an unavailable ticket is dimmed by
        // opacity instead: the navy backing shows through and the paper reads as shut.
        UIPaper.Button(batch,layout.Action,UIPaper.Face,hover,press,enabled,enabled ? 1f : .45f,
            shape:UIPaperShape.Ticket);
        var text=layout.Action;text.Inflate(-(int)(25*s),-(int)(12*s));text.Width-=(int)(32*s);
        Text(batch,label,text,50*s,enabled ? UIPaper.Ink : UIPaper.MutedInk,true,true);
        Vector2 arrow=new(layout.Action.Right-35*s,layout.Action.Center.Y);
        if(enabled) UISkillSymbols.Play(batch,_pixel,arrow,UIPaper.Ink);
        else
        {
            int side=Math.Max(12,(int)(30*s));
            UISkillSymbols.Status(batch,_pixel,new Rectangle((int)(arrow.X-side/2f),(int)(arrow.Y-side/2f),side,side),
                Fida.Campaign.CampaignSkillAvailability.Locked,UIPaper.MutedInk);
        }
    }

    public void DrawChapterCard(SpriteBatch batch,Rectangle bounds,string number,string title,int cleared,int total,
        bool selected,bool locked,float hover,float press,ChapterStyle? style=null)
    {
        var look=style ?? ChapterStyle.Default;
        float s=bounds.Height/200f;
        if(selected)
        {
            var back=bounds;back.Offset(-(int)(5*s),(int)(4*s));
            UIPaperFrame.Draw(batch,back,look.Side ? look.Accent : Cyan,UIPaperFrameShape.Node,1.5f*s);
        }
        Color stock=look.Side ? look.Face : UIPaper.Face;
        Color face=locked ? Color.Lerp(LockedFace,Color.White,hover*.06f)
            : Color.Lerp(stock,Color.White,hover*.08f);
        UIPaperFrame.Draw(batch,bounds,face,UIPaperFrameShape.Node,1.5f*s);
        int pad=Math.Max(8,(int)(12*s)),header=Math.Max(32,(int)(46*s));
        Color muted=locked ? Color.Lerp(UIPaper.MutedInk,LockedFace,.45f) : look.MutedInk;
        Color ink=locked ? UIPaper.MutedInk : look.Ink;
        Text(batch,number,new Rectangle(bounds.Left+pad,bounds.Top+5,(int)(52*s),header-5),29*s,
            locked ? muted : look.Side ? look.Accent : muted,true);
        batch.Draw(_pixel,new Rectangle(bounds.Left+pad+(int)(54*s),bounds.Top+(int)(10*s),1,(int)(24*s)),muted*.45f);
        Text(batch,title,new Rectangle(bounds.Left+pad+(int)(68*s),bounds.Top+5,bounds.Width-pad*2-(int)(68*s),header-5),33*s,
            ink,true);
        var image=new Rectangle(bounds.Left+pad,bounds.Top+header,bounds.Width-pad*2,Math.Max(1,bounds.Height-header-pad));
        UIPaper.ChapterLocation(batch,image,string.IsNullOrWhiteSpace(look.ArtKey) ? title : look.ArtKey);
        var count=new Rectangle(image.Right-(int)(92*s),image.Bottom-(int)(30*s),(int)(92*s),(int)(30*s));
        batch.Draw(_pixel,count,look.Side && !locked ? Color.Lerp(look.Ink,Color.Black,.12f) : new Color(15,29,39)*.88f);
        Text(batch,$"{cleared} / {total}",count,26*s,
            locked ? UITheme.InkMuted : look.Side ? look.Face : UITheme.Ink,true,true);
        if(locked)
        {
            // A locked chapter is unprinted stock: grey sheet, faded artwork and the same badge.
            batch.Draw(_pixel,image,LockedWashColor*LockedWash);
            int side=Math.Max(14,(int)(22*s));
            UISkillSymbols.Status(batch,_pixel,new Rectangle(image.Right-side-5,image.Top+5,side,side),
                Fida.Campaign.CampaignSkillAvailability.Locked,UITheme.Ink);
        }
    }

    /// <summary>Small diamonds printed on the location art: one per difficulty tier.</summary>
    private void DrawTierPips(SpriteBatch batch, Rectangle art, float s, ChapterStyle look, int tier)
    {
        float radius = Math.Max(2.5f, 4.2f * s);
        float gap = radius * 2.7f;
        float x = art.Left + radius + 6 * s;
        float y = art.Bottom - radius - 6 * s;
        Color color = Color.Lerp(look.Side ? look.Ink : UIPaper.Ink, Color.White, .25f);
        for (int i = 0; i < tier; i++)
        {
            Vector2 center = new(x + i * gap, y);
            Vector2 top = center + new Vector2(0, -radius);
            Vector2 right = center + new Vector2(radius, 0);
            Vector2 bottom = center + new Vector2(0, radius);
            Vector2 left = center + new Vector2(-radius, 0);
            foreach (var (a, b) in new[] { (top, right), (right, bottom), (bottom, left), (left, top) })
            {
                Line(batch, a + new Vector2(1, 1.5f), b + new Vector2(1, 1.5f), Color.Black * .45f, Math.Max(1, 2.2f * s));
                Line(batch, a, b, color, Math.Max(1, 1.5f * s));
            }
        }
    }

    public void DrawDetails(SpriteBatch batch, CampaignMapLayout layout, CampaignRouteDetails detail,
        float hover, float press, ChapterStyle? style = null)
    {
        var look = style ?? ChapterStyle.Default;
        float s = layout.Scale;
        Rectangle E(string name) => layout.Element(name);
        DrawPanel(batch,layout);
        string eyebrow = detail.Side ? $"{detail.Id}   ·   SIDE CHAPTER" : detail.Id;
        Text(batch, eyebrow, E("NodeId"), 27 * s, detail.Side ? Lighten(look.Accent) : UITheme.InkMuted);
        Text(batch, detail.Title, E("Title"), 51 * s, UITheme.Ink, true);
        var image=E("Image");
        int imageTop=Math.Max(image.Top,E("Title").Bottom+Math.Max(2,(int)(12*s)));
        image.Height=Math.Max(1,image.Bottom-imageTop);
        image.Y=imageTop;
        UIPaper.CampaignNodeLocation(batch, image, detail.Id, detail.Title, detail.Chapter);
        if (detail.Side)
            DrawDifficultyBadge(batch, image, s, look, detail.Tier, detail.Vows);
        foreach (string divider in new[] { "DividerImage", "DividerStory", "DividerMetric" })
            batch.Draw(_pixel, E(divider), new Color(77,94,101));
        Paragraph(batch, detail.Story, E("Story"), 38 * s);
        Text(batch, $"{detail.EnemyCount} enemies  /  {detail.SkillBudget} SP", E("Metric"), 33 * s, UITheme.Ink);
        Text(batch, "Reward", E("RewardLabel"), 27 * s, UITheme.InkMuted, true);
        if (detail.RewardSkill is SkillType reward)
        {
            Rectangle icon = E("RewardIcon");
            UIPaperFrame.Draw(batch, icon, new Color(242,195,117), UIPaperFrameShape.Seal, 2*s);
            icon.Inflate(-(int)(12*s), -(int)(17*s));
            if (reward == SkillType.DoubleAttack) UIPaperFrame.Draw(batch,icon,UIPaper.Ink,UIPaperFrameShape.Bolt,0);
            else UISkillArt.Draw(batch, reward, icon, UIPaper.Ink);
        }
        Text(batch, detail.Reward, E("RewardText"), 31 * s, UITheme.AccentYellow, true);
        DrawAction(batch,layout,detail.Locked ? "Locked" : "Configure skills",hover,press,!detail.Locked);
    }

    /// <summary>Difficulty diamonds and vows, printed on a dark strip over the location art.</summary>
    private void DrawDifficultyBadge(SpriteBatch batch, Rectangle image, float s, ChapterStyle look,
        int tier, IReadOnlyList<CampaignVow>? vows)
    {
        int pips = tier is >= 1 and <= 4 ? tier : 0;
        string vowText = vows is { Count: > 0 } ? "VOW " + CampaignVows.Labels(vows) : "";
        if (pips == 0 && vowText.Length == 0) return;
        if (image.Width < 40) return;

        float scale = MathHelper.Clamp(s * .62f, .4f, 1.1f);
        string safe = UIFontGuard.Sanitize(_body, vowText);
        Vector2 textSize = safe.Length == 0 ? Vector2.Zero : _body.MeasureString(safe) * scale;
        float pipRadius = Math.Max(2.5f, 4.5f * s);
        float pipStep = pipRadius * 2.4f;
        float pipWidth = pips * pipStep;
        int height = (int)(Math.Max(textSize.Y, pipRadius * 2) + 14 * s);
        int width = (int)(pipWidth + (safe.Length == 0 ? 0 : textSize.X + pipStep) + 22 * s);
        var strip = new Rectangle(image.Left, image.Bottom - height, Math.Min(image.Width, width), height);
        batch.Draw(_pixel, strip, new Color(15, 29, 39) * .88f);

        Color ink = Lighten(look.Accent);
        float x = strip.Left + 11 * s;
        float centerY = strip.Center.Y;
        for (int i = 0; i < pips; i++)
        {
            Vector2 center = new(x + pipRadius, centerY);
            Line(batch, center + new Vector2(0, -pipRadius), center + new Vector2(pipRadius, 0), ink, Math.Max(1, 1.6f * s));
            Line(batch, center + new Vector2(pipRadius, 0), center + new Vector2(0, pipRadius), ink, Math.Max(1, 1.6f * s));
            Line(batch, center + new Vector2(0, pipRadius), center + new Vector2(-pipRadius, 0), ink, Math.Max(1, 1.6f * s));
            Line(batch, center + new Vector2(-pipRadius, 0), center + new Vector2(0, -pipRadius), ink, Math.Max(1, 1.6f * s));
            x += pipStep;
        }
        if (safe.Length > 0)
            UITextRenderer.DrawString(batch, _body, safe,
                new Vector2(x + pipStep * .4f, centerY - textSize.Y / 2), ink, scale: scale);
    }

    private void Paragraph(SpriteBatch batch, string text, Rectangle area, float height)
    {
        string safe = UIFontGuard.Sanitize(_body, text);
        float scale = height / _body.LineSpacing;
        var lines = UITextRenderer.WrapLines(safe, area.Width, value => _body.MeasureString(value).X * scale);
        float lineHeight = height * 1.15f;
        if (lines.Count * lineHeight > area.Height)
        { scale *= area.Height / (lines.Count * lineHeight); lineHeight = area.Height / (float)lines.Count; }
        float y = area.Top;
        foreach (string line in lines)
        {
            UITextRenderer.DrawString(batch, _body, line, new Vector2(area.Left,y), UITheme.Ink, scale: scale);
            y += lineHeight;
        }
    }

    public void DrawNode(SpriteBatch batch, Rectangle bounds, string id, string name, string chapter, bool selected, bool completed, bool locked,
        ChapterStyle? style = null, int tier = 0)
    {
        var look = style ?? ChapterStyle.Default;
        float s = bounds.Height / 180f;
        Rectangle R(float x, float y, float w, float h) => new(bounds.X+(int)(x*s),bounds.Y+(int)(y*s),
            Math.Max(1,(int)(w*s)),Math.Max(1,(int)(h*s)));
        var tab = R(-17,12,29,144);
        Color tabColor = selected ? look.Accent : look.Side ? look.Accent : new Color(180,188,184);
        UIPaperFrame.Draw(batch, tab, tabColor, UIPaperFrameShape.Tab, 1.5f*s);
        var shadow=bounds; shadow.Offset((int)(3*s),Math.Max(2,(int)(9*s)));
        UIPaperFrame.Draw(batch,shadow,new Color(6,15,22),UIPaperFrameShape.RouteNode,0);
        var backing = bounds; backing.Offset(-(int)(6*s), (int)(6*s));
        UIPaperFrame.Draw(batch, backing, new Color(56,76,87), UIPaperFrameShape.RouteNode, 1.5f*s);
        var rim=bounds; rim.Offset(0,Math.Max(1,(int)(3*s)));
        UIPaperFrame.Draw(batch,rim,selected ? Color.Lerp(look.Accent,Color.Black,.25f) : new Color(151,157,151),UIPaperFrameShape.RouteNode,0);
        Color face=locked ? LockedFace : look.Side ? look.Face : UIPaper.Face;
        UIPaperFrame.Draw(batch, bounds, selected ? Color.Lerp(face,look.Accent,locked ? .3f : 1f) : face, UIPaperFrameShape.RouteNode, 1.5f*s);
        var art = new Rectangle(bounds.Left+(int)(10*s), bounds.Top+(int)(70*s), bounds.Width-(int)(20*s), bounds.Height-(int)(84*s));
        UIPaper.CampaignNodeLocation(batch, art, id, name, chapter, miniature: true);
        if(locked) batch.Draw(_pixel,art,new Color(13,28,39)*.48f);
        Text(batch, id, R(22,18,60,42), 37*s, look.Side && !locked ? look.Ink : UIPaper.Ink, true);
        Text(batch, name, new Rectangle(bounds.Left+(int)(98*s),bounds.Top+(int)(40*s),bounds.Width-(int)(110*s),(int)(38*s)), 25*s,
            look.Side && !locked ? look.Ink : UIPaper.Ink, true);
        if(!locked && tier is >= 1 and <= 4) DrawTierPips(batch, art, s, look, tier);
        Vector2 center = new(bounds.Right-29*s,bounds.Bottom-27*s);
        if(locked)
        {
            var badge=new Rectangle(bounds.Right-(int)(132*s),bounds.Bottom-(int)(49*s),
                Math.Max(1,(int)(120*s)),Math.Max(1,(int)(34*s)));
            batch.Draw(_pixel,badge,new Color(15,29,40)*.94f);
            UISkillSymbols.Status(batch,_pixel,new Rectangle(badge.Left+(int)(6*s),badge.Top+(int)(4*s),
                Math.Max(10,(int)(24*s)),Math.Max(10,(int)(24*s))),
                Fida.Campaign.CampaignSkillAvailability.Locked,UITheme.Ink);
            Text(batch,"Locked",new Rectangle(badge.Left+(int)(36*s),badge.Top,
                Math.Max(1,badge.Width-(int)(40*s)),badge.Height),22*s,UITheme.Ink,true);
            return;
        }
        float radius = 9*s;
        for(int i=0;i<20;i++)
        {
            float a=i*MathHelper.TwoPi/20, b=(i+1)*MathHelper.TwoPi/20;
            Line(batch, center + new Vector2(MathF.Cos(a),MathF.Sin(a))*radius,
                center + new Vector2(MathF.Cos(b),MathF.Sin(b))*radius, UITheme.Ink, Math.Max(1,2*s));
        }
        if(completed) batch.Draw(_pixel,new Rectangle((int)(center.X-3*s),(int)(center.Y-3*s),Math.Max(1,(int)(6*s)),Math.Max(1,(int)(6*s))),UITheme.Ink);
    }

    public void DrawConnection(SpriteBatch batch, Rectangle from, Rectangle to, Rectangle clip, float scale, bool branches, bool merges)
    {
        int sign = Math.Sign(to.Center.X-from.Center.X);
        Vector2 start = new(from.Center.X+(branches ? sign*from.Width*.28f : 0),from.Bottom-16*scale);
        Vector2 end = new(to.Center.X-(merges ? sign*to.Width*.28f : 0),to.Top+16*scale);
        float bend = start.Y+(end.Y-start.Y)*.52f;
        Vector2 elbow = new(end.X,bend);
        // Rectangle clipping preserves pan/zoom without allowing paths into the detail card or header.
        // Paint the whole shadow first so the second segment cannot cut into the first face.
        foreach (bool shadow in new[]{true,false})
        {
            Segment(start,elbow,shadow);
            Segment(elbow,end,shadow);
        }
        void Segment(Vector2 a, Vector2 b,bool shadow)
        {
            float t0=0,t1=1; Vector2 d=b-a;
            if(!Clip(-d.X,a.X-clip.Left)||!Clip(d.X,clip.Right-a.X)||!Clip(-d.Y,a.Y-clip.Top)||!Clip(d.Y,clip.Bottom-a.Y))return;
            Vector2 p=a+d*t0,q=a+d*t1;
            // Overlap the butt ends at the elbow; the shared line texture fades at its ends.
            Vector2 overlap=Vector2.Normalize(b-a)*3*scale;
            p-=overlap; q+=overlap;
            if(shadow) RouteLine(p+new Vector2(scale,2*scale),q+new Vector2(scale,2*scale),Color.Black*.55f,10*scale);
            else RouteLine(p,q,new Color(184,188,181),9*scale);
            bool Clip(float p,float q)
            {
                if(Math.Abs(p)<.0001f)return q>=0;
                float r=q/p;
                if(p<0){if(r>t1)return false;t0=Math.Max(t0,r);}else{if(r<t0)return false;t1=Math.Min(t1,r);}return true;
            }
        }
        void RouteLine(Vector2 a,Vector2 b,Color color,float thickness)
        {
            Vector2 d=b-a;
            if(d.LengthSquared()<.01f)return;
            var source=new Rectangle(2,0,_line.Width-4,_line.Height);
            batch.Draw(_line,a,source,color,MathF.Atan2(d.Y,d.X),new Vector2(0,_line.Height/2f),
                new Vector2(d.Length()/source.Width,(thickness+1)/_line.Height),SpriteEffects.None,0);
        }
    }

    private void Line(SpriteBatch batch,Vector2 a,Vector2 b,Color color,float thickness)
    {
        Vector2 d=b-a;
        if(d.LengthSquared()<.01f)return;
        batch.Draw(_line,a,null,color,MathF.Atan2(d.Y,d.X),new Vector2(0,_line.Height/2f),
            new Vector2(d.Length()/_line.Width,(thickness+1)/_line.Height),SpriteEffects.None,0);
    }

    public void DrawZoom(SpriteBatch batch, CampaignMapLayout layout, int percent, float minusHover, float minusPress,
        float plusHover, float plusPress)
    {
        foreach(var (name,label,hover,press) in new[]{("Minus","−",minusHover,minusPress),("Plus","+",plusHover,plusPress)})
        {
            var bounds=layout.Element(name);
            UIPaper.Button(batch,bounds,UITheme.PanelLight,hover,press,cut:3);
            Text(batch,label,bounds,45*layout.Scale,UITheme.Ink,center:true);
        }
        Text(batch,$"{percent}%",layout.Element("Fit"),25*layout.Scale,UITheme.Ink,center:true);
    }
}
