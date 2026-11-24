using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public enum UIEditorialScreen { Menu, Settings, Lobby }

/// <summary>Noninteractive artwork and labels. All actions remain ordinary named buttons.</summary>
public sealed class UIEditorialSurface : UIControl
{
    private readonly ResourceManager _resources;
    private readonly UIEditorialScreen _screen;
    public bool Connected { get; set; }
    public bool LocalReady { get; set; }
    public bool RemoteReady { get; set; }
    public bool IsHost { get; set; }
    public UIEditorialSurface(ResourceManager resources, UIEditorialScreen screen) : base(-50)
    { _resources = resources; _screen = screen; }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var l = new UIEditorialLayout(batch.GraphicsDevice.Viewport);
        bool p = l.Portrait;
        if (_screen == UIEditorialScreen.Menu)
        {
            if (!p)
            {
                batch.Draw(_resources.GetTexture("paper_main_art"), batch.GraphicsDevice.Viewport.Bounds, Color.White);
                if (Connected) Text(batch, l, "Connected", l.Rect(150, 1090, 620, 40), 26, color: UITheme.AccentBlue);
                return;
            }
            // Portrait keeps a quiet backdrop and live title; no numeric decoration.
            UIPaper.Background(batch, batch.GraphicsDevice.Viewport);
            Text(batch, l, "FIDA", l.Rect(p ? 80 : 150, p ? 100 : 65, p ? 740 : 680, 150), 142, false, true);
            if (Connected) Text(batch, l, "Connected", l.Rect(p ? 80 : 150, p ? 1490 : 1090, 620, 40), 26, color: UITheme.AccentBlue);
            return;
        }
        UIPaper.Background(batch, batch.GraphicsDevice.Viewport);
        UIPaper.Backdrop(batch, batch.GraphicsDevice.Viewport, time);
        UIPaper.Sheet(batch, p ? l.Rect(30, 30, 840, 135) : l.Rect(45, 30, 1830, 135), UITheme.Panel, 3, 9);
        Text(batch, l, _screen == UIEditorialScreen.Settings ? "Settings" : "Lobby",
            l.Header, 65);
        if (_screen == UIEditorialScreen.Settings)
        {
            UIPaper.Sheet(batch, p ? l.Rect(50, 250, 800, 1190) : l.Rect(550, 270, 1250, 790), UITheme.Panel, 4, 9);
            if (!p)
            {
                Rule(batch, l.Rect(500, 290, 1, 740));
                batch.Draw(_resources.Pixel, l.Rect(150, 350, 4, 50), UITheme.AccentBlue);
                Text(batch, l, "Settings", l.Rect(180, 340, 280, 70), 42);
            }
            string[] rows = { "Display mode", "Resolution", "Interface motion", "Music volume", "Effects volume" };
            for (int i = 0; i < rows.Length; i++)
            {
                Text(batch, l, rows[i], p ? l.Rect(80, 290 + i * 225, 740, 65) : l.Rect(600, 325 + i * 145, 420, 65), 38);
                Rule(batch, p ? l.Rect(80, 480 + i * 225, 740, 1) : l.Rect(600, 430 + i * 145, 1150, 1));
            }
            Text(batch, l, "Changes apply immediately. Click volume to mute.", p ? l.Rect(80, 1470, 740, 60) : l.Rect(600, 1100, 1150, 60), 26, color: UITheme.InkMuted);
            return;
        }
        var connected = p ? l.Rect(550,190,270,60) : l.Rect(1490,65,300,70);
        UIPaper.Sheet(batch,connected,UITheme.AccentBlue,2,7);
        Text(batch,l,"Connected",connected,28,true,color:UIPaper.Ink);
        UIPaper.Sheet(batch, p ? l.Rect(70, 370, 350, 650) : l.Rect(300, 300, 550, 590), UITheme.Panel, 4, 9);
        UIPaper.Sheet(batch, p ? l.Rect(480, 370, 350, 650) : l.Rect(1070, 300, 550, 590), UITheme.Panel, 4, 9);
        int y = p ? 400 : 330;
        Texture2D identities = _resources.GetTexture("cold_roster");
        UIPaperAvatar.Draw(batch, identities, p ? l.Rect(110,y,280,340) : l.Rect(400,y,350,360),
            UIPaperAvatar.PlayerCell);
        UIPaperAvatar.Draw(batch, identities, p ? l.Rect(510,y,280,340) : l.Rect(1170,y,350,360),
            UIPaperAvatar.OpponentCell);
        Text(batch, l, "VS", p ? l.Rect(390, 560, 120, 80) : l.Rect(850, 490, 220, 90), 56, true, color: UITheme.InkMuted);
        string local = IsHost ? "Player 01" : "Player 02", remote = IsHost ? "Player 02" : "Player 01";
        Text(batch, l, local, p ? l.Rect(80, 800, 340, 70) : l.Rect(310, 740, 530, 70), 38, true);
        Text(batch, l, remote, p ? l.Rect(480, 800, 340, 70) : l.Rect(1080, 740, 530, 70), 38, true);
        var localStatus = p ? l.Rect(90,900,320,70) : l.Rect(340,810,470,80);
        var remoteStatus = p ? l.Rect(490,900,320,70) : l.Rect(1110,810,470,80);
        UIPaper.Sheet(batch,localStatus,LocalReady ? UITheme.AccentBlue : UITheme.PanelLight,2,6);
        UIPaper.Sheet(batch,remoteStatus,RemoteReady ? UITheme.AccentBlue : UITheme.PanelLight,2,6);
        Text(batch,l,LocalReady ? "Ready" : "Not ready",localStatus,30,true,color:LocalReady ? UIPaper.Ink : UITheme.InkMuted);
        Text(batch,l,RemoteReady ? "Ready" : "Waiting",remoteStatus,30,true,color:RemoteReady ? UIPaper.Ink : UITheme.InkMuted);
    }

    private void Portrait(SpriteBatch batch, UIEditorialLayout l, Rectangle bounds, int column, GameTime time, float phase)
    {
        Texture2D texture = _resources.GetTexture("cold_roster");
        Rectangle source = new(column * texture.Width / 3, 0, texture.Width / 3, texture.Height / 2);
        float scale = Math.Min(bounds.Width / (float)source.Width, bounds.Height / (float)source.Height);
        float breath = UIMotion.Reduced ? 0 : (float)Math.Sin(time.TotalGameTime.TotalSeconds * 1.2 + phase);
        batch.Draw(texture, new Vector2(bounds.Center.X, bounds.Bottom - breath * 2 * l.Scale), source, UITheme.Ink,
            0, new Vector2(source.Width / 2f, source.Height), new Vector2(scale, scale * (1 + breath * .006f)), SpriteEffects.None, 0);
    }

    private void Rule(SpriteBatch batch, Rectangle rect) => batch.Draw(_resources.Pixel, rect, UITheme.Line);

    private void Text(SpriteBatch batch, UIEditorialLayout layout, string text, Rectangle area, float size,
        bool center = false, bool title = false, Color? color = null)
    {
        SpriteFont font = title ? _resources.TitleFont : size <= 30 ? _resources.CaptionFont : _resources.LabelFont;
        string safe = UIFontGuard.Sanitize(font, text);
        Vector2 measured = font.MeasureString(safe);
        float scale = UITextRenderer.FitScale(measured, area.Width, area.Height, size * layout.Scale / font.LineSpacing);
        Vector2 pos = new(center ? area.Center.X - measured.X * scale / 2 : area.Left, area.Center.Y - measured.Y * scale / 2);
        UITextRenderer.DrawString(batch, font, safe, pos, color ?? UITheme.Ink, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
}

