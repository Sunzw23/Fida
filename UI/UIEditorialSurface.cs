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
        UIPaper.Background(batch, batch.GraphicsDevice.Viewport);
        if (_screen == UIEditorialScreen.Menu)
        {
            if (!p)
            {
                batch.Draw(_resources.GetTexture("paper_main_art"), batch.GraphicsDevice.Viewport.Bounds, Color.White);
                if (Connected) Text(batch, l, "Connected", l.Rect(150, 1090, 620, 40), 26, color: UITheme.AccentBlue);
                return;
            }
            Text(batch, l, "FIDA", l.Rect(p ? 80 : 150, p ? 100 : 65, p ? 740 : 680, 150), 142, false, true);
            float drift = UIMotion.Reduced ? 0 : (float)Math.Sin(time.TotalGameTime.TotalSeconds * .45) * 7;
            var seven = p ? l.Rect(170, 320, 330, Connected ? 210 : 290) : l.Rect(950, 280, 490, 650);
            var two = p ? l.Rect(510, Connected ? 350 : 385, 200, Connected ? 190 : 220) : l.Rect(1420, 580, 300, 400);
            seven.Y += (int)(drift * l.Scale); two.Y -= (int)(drift * l.Scale);
            var sevenShadow = seven; sevenShadow.Offset(4, 7);
            var twoShadow = two; twoShadow.Offset(4, 7);
            UIAngularNumber.Draw(batch, "7", sevenShadow, UITheme.BgTop * .8f, displaySize: true);
            UIAngularNumber.Draw(batch, "2", twoShadow, UITheme.BgTop * .8f, displaySize: true);
            UIAngularNumber.Draw(batch, "7", seven, UITheme.Ink, displaySize: true);
            UIAngularNumber.Draw(batch, "2", two, UITheme.AccentBlue, displaySize: true);
            if (Connected) Text(batch, l, "Connected", l.Rect(p ? 80 : 150, p ? 1490 : 1090, 620, 40), 26, color: UITheme.AccentBlue);
            return;
        }
        UIPaper.Sheet(batch, p ? l.Rect(30, 30, 840, 135) : l.Rect(45, 30, 1830, 135), UITheme.Panel, 3, 9);
        Text(batch, l, _screen == UIEditorialScreen.Settings ? "Settings" : "Lobby",
            l.Header, 65);
        if (_screen == UIEditorialScreen.Settings)
        {
            UIPaper.Sheet(batch, p ? l.Rect(50, 290, 800, 850) : l.Rect(550, 290, 1250, 710), UITheme.Panel, 4, 9);
            if (!p)
            {
                Rule(batch, l.Rect(500, 290, 1, 690));
                batch.Draw(_resources.Pixel, l.Rect(150, 350, 4, 50), UITheme.AccentBlue);
                Text(batch, l, "Display", l.Rect(180, 340, 280, 70), 42);
            }
            string[] rows = { "Display mode", "Resolution", "Interface motion" };
            for (int i = 0; i < rows.Length; i++)
            {
                Text(batch, l, rows[i], p ? l.Rect(80, 350 + i * 250, 740, 70) : l.Rect(600, 350 + i * 230, 420, 70), 42);
                Rule(batch, p ? l.Rect(80, 580 + i * 250, 740, 1) : l.Rect(600, 495 + i * 230, 1150, 1));
            }
            Text(batch, l, "Changes apply immediately", p ? l.Rect(80, 1300, 740, 60) : l.Rect(600, 1080, 1150, 60), 26, color: UITheme.InkMuted);
            return;
        }
        Text(batch, l, "Connected", p ? l.Rect(550, 190, 270, 60) : l.Rect(1400, 70, 360, 60), 28, color: UITheme.AccentBlue);
        UIPaper.Sheet(batch, p ? l.Rect(70, 370, 350, 650) : l.Rect(300, 300, 550, 590), UITheme.Panel, 4, 12);
        UIPaper.Sheet(batch, p ? l.Rect(480, 370, 350, 650) : l.Rect(1070, 300, 550, 590), UITheme.Panel, 4, 12);
        int y = p ? 400 : 330;
        Portrait(batch, l, p ? l.Rect(110, y, 280, 340) : l.Rect(400, y, 350, 360), 0, time, 0);
        Portrait(batch, l, p ? l.Rect(510, y, 280, 340) : l.Rect(1170, y, 350, 360), 1, time, 2);
        Text(batch, l, "VS", p ? l.Rect(390, 560, 120, 80) : l.Rect(850, 490, 220, 90), 56, true, color: UITheme.InkMuted);
        string local = IsHost ? "Player 01" : "Player 02", remote = IsHost ? "Player 02" : "Player 01";
        Text(batch, l, local, p ? l.Rect(80, 800, 340, 70) : l.Rect(310, 740, 530, 70), 38, true);
        Text(batch, l, remote, p ? l.Rect(480, 800, 340, 70) : l.Rect(1080, 740, 530, 70), 38, true);
        Text(batch, l, LocalReady ? "Ready" : "Not ready", p ? l.Rect(80, 900, 340, 60) : l.Rect(310, 820, 530, 60), 30, true, color: LocalReady ? UITheme.AccentBlue : UITheme.InkMuted);
        Text(batch, l, RemoteReady ? "Ready" : "Waiting", p ? l.Rect(480, 900, 340, 60) : l.Rect(1080, 820, 530, 60), 30, true, color: RemoteReady ? UITheme.AccentBlue : UITheme.InkMuted);
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
