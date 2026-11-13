using System;
using System.Linq;
using Fida.Logic;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public enum UIReferenceScreen { Rules, Online, Connection, Hosting, Connecting }

/// <summary>Plain reference text and online flow labels, with no decorative rule artwork.</summary>
public sealed class UIReferenceSurface : UIControl
{
    public static readonly string[] Chapters = { "Your turn", "Number effects", "Skill timing", "Skills I", "Skills II" };
    private readonly ResourceManager _resources;
    private readonly UIReferenceScreen _screen;
    private readonly int _page;
    public bool RoomCodeRequired { get; set; } = true;
    public UIReferenceSurface(ResourceManager resources, UIReferenceScreen screen, int page = 0) : base(-60)
    { _resources = resources; _screen = screen; _page = page; }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var layout = new UIReferenceLayout(batch.GraphicsDevice.Viewport);
        var l = layout.Canvas;
        bool p = l.Portrait;
        UIPaper.Background(batch, batch.GraphicsDevice.Viewport);
        // Same layered backdrop as the menu, settings and lobby surfaces.
        UIPaper.Backdrop(batch, batch.GraphicsDevice.Viewport, time);
        UIPaper.Sheet(batch, p ? l.Rect(30, 30, 840, 135) : l.Rect(45, 30, 1830, 135), UITheme.Panel, 3, 9);
        string title = _screen switch { UIReferenceScreen.Rules => "Rules", UIReferenceScreen.Hosting => "Create room", UIReferenceScreen.Connecting => "Connecting", UIReferenceScreen.Connection => "Connection", _ => "Online" };
        Text(batch, title, l.Header, 65);
        if (_screen == UIReferenceScreen.Rules) { DrawRules(batch, layout); return; }
        if (_screen == UIReferenceScreen.Online)
        {
            UIPaper.Sheet(batch, p ? l.Rect(50, 245, 800, 420) : l.Rect(110, 265, 780, 790), UITheme.Panel, 4, 9);
            UIPaper.Sheet(batch, p ? l.Rect(50, 755, 800, 650) : l.Rect(1030, 265, 780, 790), UITheme.Panel, 4, 9);
            Rule(batch, p ? l.Rect(80, 720, 740, 1) : l.Rect(960, 290, 1, 710));
            Text(batch, "Create room", p ? l.Rect(80, 280, 740, 80) : l.Rect(150, 630, 700, 90), 42, true);
            Paragraph(batch, "Start a session and share its code.", p ? l.Rect(80, 390, 740, 90) : l.Rect(260, 745, 480, 110));
            if (!p)
            {
                // Was a fully saturated cyan block with ivory marks on it, which read as a
                // glare next to the dark panel. Now the accent sheet is half-transparent
                // over the panel and the marks are ink, so it stays a highlight, not a light.
                UIPaper.Sheet(batch, l.Rect(340, 380, 320, 190), UITheme.AccentBlue * .34f, 2, 9);
                UIPaperAvatar.Draw(batch, l.Rect(255, 350, 190, 240));
                UIPaperAvatar.Draw(batch, l.Rect(555, 350, 190, 240), 1);
                batch.Draw(_resources.Pixel, l.Rect(465, 445, 90, 15), UIPaper.Ink);
                batch.Draw(_resources.Pixel, l.Rect(502, 408, 15, 90), UIPaper.Ink);
            }
            Text(batch, "Join room", p ? l.Rect(80, 790, 740, 80) : l.Rect(1070, 630, 700, 90), 42,true);
            Paragraph(batch, RoomCodeRequired ? "Enter the code from your opponent." : "Connect to the configured server.",
                p ? l.Rect(80, 900, 740, 80) : l.Rect(1180, 745, 480, 80));
            if (RoomCodeRequired)
                Text(batch, "4 characters - letters or numbers", p ? l.Rect(80, 1170, 740, 65) : l.Rect(1070, 835, 700, 45), 26, true, muted: true);
            else Text(batch, "Direct connection", layout.Code, 44, true);
            return;
        }
        if (_screen == UIReferenceScreen.Connection)
        {
            UIPaper.Sheet(batch, p ? l.Rect(50, 320, 800, 780) : l.Rect(540, 280, 1250, 570), UITheme.Panel, 4, 9);
            Text(batch, "Connected", p ? l.Rect(80, 400, 740, 90) : l.Rect(600, 350, 1150, 90), 42, color: UITheme.AccentBlue);
            Paragraph(batch, "Campaign and Duel are available from the main menu.",
                p ? l.Rect(80, 550, 740, 200) : l.Rect(600, 500, 1150, 170));
        }
        else if (_screen == UIReferenceScreen.Hosting)
        {
            UIPaper.Sheet(batch, p ? l.Rect(50, 260, 800, 1100) : l.Rect(220, 240, 1480, 820), UITheme.Panel, 4, 9);
            Text(batch, "Room code", p ? l.Rect(80, 300, 740, 80) : l.Rect(600, 300, 720, 80), 38, true);
            Text(batch,"Share this code to invite your opponent.",p ? l.Rect(80,960,740,100) : l.Rect(460,820,1000,60),30,true,muted:true);
        }
        else
        {
            UIPaper.Sheet(batch, p ? l.Rect(50, 320, 800, 1040) : l.Rect(220, 240, 1480, 820), UITheme.Panel, 4, 9);
            for(int i=0;i<3;i++)
            {
                float pulse = UIMotion.Reduced ? .75f : .65f + .25f * (float)Math.Sin(time.TotalGameTime.TotalSeconds*3-i*.8);
                var dot = p ? l.Rect(285+i*125,520,70,70) : l.Rect(800+i*140,460,70,70);
                UIPaper.Sheet(batch,dot,UITheme.AccentBlue*pulse,2,8);
            }
        }
    }

    private void DrawRules(SpriteBatch batch, UIReferenceLayout layout)
    {
        var l = layout.Canvas;
        if (!layout.Portrait) Rule(batch, l.Rect(500, 290, 1, 690));
        Rectangle area = layout.Content;
        var paper = area; paper.Inflate(24, 20);
        UIPaper.Sheet(batch, paper, UIPaper.Face, 4, 9);
        Text(batch, $"{_page + 1} / {Chapters.Length}", layout.Page, 30, true, muted: true);
        if (_page >= 3)
        {
            var skills = SkillFactory.SelectableSkills.Skip((_page - 3) * 10).Take(10).ToArray();
            for (int i = 0; i < skills.Length; i++)
            {
                int col = layout.Portrait ? 0 : i / 5, row = layout.Portrait ? i : i % 5;
                int gap = (int)(40 * l.Scale), width = layout.Portrait ? area.Width : (area.Width - gap) / 2;
                int height = area.Height / (layout.Portrait ? 10 : 5);
                var bounds = new Rectangle(area.Left + col * (width + gap), area.Top + row * height, width, height);
                Text(batch, GameText.SkillName(skills[i]), new Rectangle(bounds.Left, bounds.Top, bounds.Width, (int)(height * .34f)), layout.Portrait ? 33 : 40);
                Paragraph(batch, CampaignSkillPresentation.Describe(skills[i]).detail,
                    new Rectangle(bounds.Left, bounds.Top + (int)(height * .36f), bounds.Width, (int)(height * .55f)), layout.Portrait ? 29 : 32);
                if (row > 0) Rule(batch, new Rectangle(bounds.Left, bounds.Top - 4, bounds.Width, 1));
            }
            return;
        }
        (string heading, string body)[] rows = _page switch
        {
            0 => new[] {
                ("Choose", "Pick one of your two numbers."),
                ("Add", "Choose another number from any player, including yourself or an ally."),
                ("Resolve", "Add the values and keep the last digit. Then apply the number effect."),
                ("Example", "4 + 7 = 11, so the result is 1.") },
            1 => new[] {
                ("7 or 8", "Attack for 1 damage."), ("1 + 6", "Attack for 2 damage."),
                ("5 + 5", "Attack for 3 damage."), ("6 + 6", "Attack for 5 damage."),
                ("9", "Heal 1 health."), ("0", "Block one hit."), ("0 + 0", "Heal 10 health.") },
            _ => new[] {
                ("Order matters", "Skills with the same trigger resolve from top to bottom in your loadout."),
                ("Once per turn", "Limited skills become available again at the start of a new turn."),
                ("Before damage", "Defensive effects resolve before health is reduced."),
                ("After damage", "Damage reactions resolve before post-attack effects."),
                ("Defeat", "A player is out when health reaches zero.") }
        };
        for (int i = 0; i < rows.Length; i++)
        {
            int h = area.Height / rows.Length, top = area.Top + i * h;
            int headingWidth = layout.Portrait ? area.Width : (int)(285 * l.Scale);
            Rectangle heading = new(area.Left, top + 12, headingWidth, (int)(40 * l.Scale));
            Text(batch, rows[i].heading, heading, 40);
            Rectangle body = layout.Portrait
                ? new Rectangle(area.Left, heading.Bottom + 6, area.Width, Math.Max(1, h - heading.Height - 28))
                : new Rectangle(area.Left + headingWidth + 20, top + 18, area.Width - headingWidth - 20, h - 30);
            Paragraph(batch, rows[i].body, body, 36);
            Rule(batch, new Rectangle(area.Left, top + h - 1, area.Width, 1));
        }
    }

    private void Rule(SpriteBatch batch, Rectangle bounds) => batch.Draw(_resources.Pixel, bounds, UITheme.Line * .7f);
    private void Text(SpriteBatch batch, string text, Rectangle area, float lineHeight, bool center = false, bool muted = false, Color? color = null)
    {
        SpriteFont font = lineHeight > 30 ? _resources.LabelFont : _resources.CaptionFont;
        var l = new UIEditorialLayout(batch.GraphicsDevice.Viewport);
        string safe = UIFontGuard.Sanitize(font, text);
        Vector2 size = font.MeasureString(safe);
        float scale = UITextRenderer.FitScale(size, area.Width, area.Height, lineHeight * l.Scale / font.LineSpacing);
        Vector2 position = new(center ? area.Center.X - size.X * scale / 2 : area.Left, area.Center.Y - size.Y * scale / 2);
        bool reading = _screen == UIReferenceScreen.Rules && new UIReferenceLayout(batch.GraphicsDevice.Viewport).Content.Contains(area.Center);
        Color ink = reading ? muted ? UIPaper.MutedInk : UIPaper.Ink : muted ? UITheme.InkMuted : UITheme.Ink;
        UITextRenderer.DrawString(batch, font, safe, position, color ?? ink, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
    private void Paragraph(SpriteBatch batch, string text, Rectangle area, float lineHeight = 36)
    {
        var font = _resources.CaptionFont;
        var l = new UIEditorialLayout(batch.GraphicsDevice.Viewport);
        float scale = Math.Max(22, lineHeight * l.Scale) / font.LineSpacing;
        string safe = UIFontGuard.Sanitize(font, text);
        var lines = UITextRenderer.WrapLines(safe, area.Width, value => font.MeasureString(value).X * scale);
        float row = font.LineSpacing * scale * 1.12f;
        if (lines.Count * row > area.Height)
        {
            scale *= area.Height / (lines.Count * row);
            lines = UITextRenderer.WrapLines(safe, area.Width, value => font.MeasureString(value).X * scale);
            row = font.LineSpacing * scale * 1.12f;
        }
        float y = area.Top;
        foreach (string line in lines)
        {
            UITextRenderer.DrawString(batch, font, line, new Vector2(area.Left, y), _screen == UIReferenceScreen.Rules ? UIPaper.MutedInk : UITheme.InkMuted, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            y += row;
        }
    }
}


