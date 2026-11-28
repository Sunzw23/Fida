using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public enum UIReferenceScreen { Rules, Online, Connection, Hosting, Connecting }

/// <summary>Plain reference text and online flow labels, with no decorative rule artwork.</summary>
public sealed class UIReferenceSurface : UIControl
{
    private readonly ResourceManager _resources;
    private readonly UIReferenceScreen _screen;
    public bool RoomCodeRequired { get; set; } = true;
    /// <summary>Serverless session: the waiting screen shows an address instead of a room code.</summary>
    public bool Direct { get; set; }
    /// <summary>Connected page: this player opened the session instead of joining it.</summary>
    public bool IsHost { get; set; }
    /// <summary>Connected page: caption for the endpoint row, "Room" for relay hosts.</summary>
    public string DetailLabel { get; set; } = "Server";
    /// <summary>Connected page: room code or server address shown as the endpoint value.</summary>
    public string DetailValue { get; set; } = string.Empty;
    public UIReferenceSurface(ResourceManager resources, UIReferenceScreen screen) : base(-60)
    { _resources = resources; _screen = screen; }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var layout = new UIReferenceLayout(batch.GraphicsDevice.Viewport);
        var l = layout.Canvas;
        bool p = l.Portrait;
        UIPaper.Background(batch, batch.GraphicsDevice.Viewport);
        // Same layered backdrop as the menu, settings and lobby surfaces.
        UIPaper.Backdrop(batch, batch.GraphicsDevice.Viewport, time);
        UIPaper.Sheet(batch, p ? l.Rect(30, 30, 840, 135) : l.Rect(45, 30, 1830, 135), UITheme.Panel, 3, 9);
        string title = _screen switch { UIReferenceScreen.Rules => "Rules", UIReferenceScreen.Hosting => Direct ? "Create game" : "Create room", UIReferenceScreen.Connecting => "Connecting", UIReferenceScreen.Connection => "Connection", _ => "Online" };
        Text(batch, title, l.Header, 65);
        if (_screen == UIReferenceScreen.Online)
        {
            UIPaper.Sheet(batch, p ? l.Rect(50, 245, 800, 420) : l.Rect(110, 265, 780, 790), UITheme.Panel, 4, 9);
            UIPaper.Sheet(batch, p ? l.Rect(50, 755, 800, 650) : l.Rect(1030, 265, 780, 790), UITheme.Panel, 4, 9);
            Rule(batch, p ? l.Rect(80, 720, 740, 1) : l.Rect(960, 290, 1, 710));
            Text(batch, Direct ? "Create game" : "Create room", p ? l.Rect(80, 280, 740, 80) : l.Rect(150, 630, 700, 90), 42, true);
            Paragraph(batch, Direct ? "Start a session on this network." : "Start a session and share its code.", p ? l.Rect(80, 390, 740, 90) : l.Rect(260, 745, 480, 110));
            if (!p)
            {
                // Two lobby identities, side by side. The previous version put both figures on a
                // saturated cyan slab that cut them in half and drove a heavy black cross through
                // the seam, so the artwork was the loudest thing on an otherwise quiet screen.
                // Now the busts carry the illustration and the invitation mark is a small cyan
                // plus on the panel itself.
                Texture2D atlas = _resources.GetTexture("cold_roster");
                const int bustSize = 250;
                UIPaperAvatar.Draw(batch, atlas, l.Rect(230, 300, bustSize, bustSize),
                    UIPaperAvatar.PlayerCell);
                UIPaperAvatar.Draw(batch, atlas, l.Rect(520, 300, bustSize, bustSize),
                    UIPaperAvatar.OpponentCell);
                Color mark = UITheme.AccentBlue;
                batch.Draw(_resources.Pixel, l.Rect(487, 397, 26, 6), mark);
                batch.Draw(_resources.Pixel, l.Rect(497, 387, 6, 26), mark);
            }
            Text(batch, Direct ? "Join game" : "Join room", p ? l.Rect(80, 790, 740, 80) : l.Rect(1070, 630, 700, 90), 42,true);
            Paragraph(batch, RoomCodeRequired ? "Enter the code from your opponent." : "Type the host address, or leave it empty.",
                p ? l.Rect(80, 900, 740, 80) : l.Rect(1180, 745, 480, 80));
            if (RoomCodeRequired)
                Text(batch, "4 characters - letters or numbers", p ? l.Rect(80, 1170, 740, 65) : l.Rect(1070, 835, 700, 45), 26, true, muted: true);
            else
                Text(batch, "IP address, or blank to scan the network", p ? l.Rect(80, 1170, 740, 65) : l.Rect(1070, 835, 700, 45), 26, true, muted: true);
            return;
        }
        if (_screen == UIReferenceScreen.Connection)
        {
            // A connected session is a status card: the headline state plus the two facts worth
            // knowing (own role, what we are attached to). The header arrow is the way back.
            UIPaper.Sheet(batch, p ? l.Rect(50, 380, 800, 700) : l.Rect(510, 340, 900, 550), UITheme.Panel, 4, 9);
            int left = p ? 90 : 570;
            int right = p ? 810 : 1350;
            int labelWidth = p ? 230 : 250;

            int statusTop = p ? 450 : 390;
            batch.Draw(_resources.Pixel, l.Rect(left, statusTop + 6, 5, 54), UITheme.AccentBlue);
            Text(batch, "Connected", l.Rect(left + 26, statusTop, right - left - 26, 68), 42, color: UITheme.AccentBlue);
            int firstRule = p ? 600 : 494;
            Rule(batch, l.Rect(left, firstRule, right - left, 1));

            int roleTop = firstRule + 30;
            Text(batch, "Role", l.Rect(left, roleTop + 8, labelWidth, 40), 26, muted: true);
            Text(batch, IsHost ? "Host" : "Guest",
                l.Rect(left + labelWidth, roleTop, right - left - labelWidth, 56), 34);
            int secondRule = p ? 730 : 608;
            Rule(batch, l.Rect(left, secondRule, right - left, 1));

            int endpointTop = secondRule + 30;
            Text(batch, DetailLabel, l.Rect(left, endpointTop + 8, labelWidth, 40), 26, muted: true);
            Text(batch, string.IsNullOrWhiteSpace(DetailValue) ? "----" : DetailValue,
                l.Rect(left + labelWidth, endpointTop, right - left - labelWidth, 56), 34);
            int thirdRule = p ? 860 : 722;
            Rule(batch, l.Rect(left, thirdRule, right - left, 1));
        }
        else if (_screen == UIReferenceScreen.Hosting)
        {
            UIPaper.Sheet(batch, p ? l.Rect(50, 260, 800, 1100) : l.Rect(220, 240, 1480, 820), UITheme.Panel, 4, 9);
            Text(batch, Direct ? "Address" : "Room code", p ? l.Rect(80, 300, 740, 80) : l.Rect(600, 300, 720, 80), 38, true);
            if (!Direct)
                Text(batch, "Share this code to invite your opponent.",
                    p ? l.Rect(80,960,740,100) : l.Rect(460,820,1000,60),30,true,muted:true);
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

    private void Rule(SpriteBatch batch, Rectangle bounds) => batch.Draw(_resources.Pixel, bounds, UITheme.Line * .7f);
    private void Text(SpriteBatch batch, string text, Rectangle area, float lineHeight, bool center = false, bool muted = false, Color? color = null)
    {
        SpriteFont font = lineHeight > 30 ? _resources.LabelFont : _resources.CaptionFont;
        var l = new UIEditorialLayout(batch.GraphicsDevice.Viewport);
        string safe = UIFontGuard.Sanitize(font, text);
        Vector2 size = font.MeasureString(safe);
#if FIDA_ANDROID
        // The line height argument is a design size; the drawn size comes from the
        // dp ramp, and the box grows to it instead of shrinking the type back down.
        float lineBox = UITypography.LineHeight(UITypography.FromDesignPixels(lineHeight),
            l.Width, l.Height);
        float preferred = lineBox / font.LineSpacing;
        if (lineBox > area.Height)
        {
            int grown = (int)System.Math.Ceiling(lineBox);
            area = new Rectangle(area.X, area.Y - (grown - area.Height) / 2, area.Width, grown);
        }
#else
        float preferred = lineHeight * l.Scale / font.LineSpacing;
#endif
        float scale = UITextRenderer.FitScale(size, area.Width, area.Height, preferred);
        Vector2 position = new(center ? area.Center.X - size.X * scale / 2 : area.Left, area.Center.Y - size.Y * scale / 2);
        Color ink = muted ? UITheme.InkMuted : UITheme.Ink;
        UITextRenderer.DrawString(batch, font, safe, position, color ?? ink, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
    private void Paragraph(SpriteBatch batch, string text, Rectangle area, float lineHeight = 36)
    {
        var font = _resources.CaptionFont;
        var l = new UIEditorialLayout(batch.GraphicsDevice.Viewport);
#if FIDA_ANDROID
        float scale = UITypography.LineHeight(UITypography.FromDesignPixels(lineHeight),
            l.Width, l.Height) / font.LineSpacing;
#else
        float scale = Math.Max(22, lineHeight * l.Scale) / font.LineSpacing;
#endif
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
            UITextRenderer.DrawString(batch, font, line, new Vector2(area.Left, y), UITheme.InkMuted, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            y += row;
        }
    }
}


