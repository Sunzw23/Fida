using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Fida.Logic;

namespace Fida.UI;

/// <summary>Quiet battle structure, with no duplicate portraits or numerical controls.</summary>
public sealed class UIBattleChrome : UIControl
{
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly GameManager _game;
    public UIBattleChrome(int id, Texture2D pixel, SpriteFont font, GameManager game) : base(id)
    { _pixel = pixel; _font = font; _game = game; }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var vp = batch.GraphicsDevice.Viewport;
        float scale = UIDesktopLayout.Scale(vp);
        void Text(string text, Vector2 point, Color color, float size, float width) =>
            UIUtils.DrawCenteredFit(batch, _font, text, UIDesktopLayout.MapPoint(point, vp), width * scale,
                size > 0.5f ? UITypography.Scale(_font, UITextRole.Heading, vp) : UITypography.Scale(_font, UITextRole.Caption, vp), color);
        Text("BATTLE", new Vector2(160, 50), UITheme.Ink, 0.9f, 240);
        Text($"TURN {_game.TurnNumber + 1:00}", new Vector2(960, 50), UITheme.InkMuted, 0.3f, 700);
        foreach (int x in new[] { 105, 1540 })
            batch.Draw(_pixel, UIDesktopLayout.MapRectangle(new Rectangle(x, 585, 275, 1), vp), UITheme.InkMuted * .65f);
        var footer = UIDesktopLayout.MapRectangle(new Rectangle(55, 1100, 1810, 74), vp);
        UIPaper.Sheet(batch, footer, UITheme.PanelDark, 1, 4);
        UIUtils.DrawRectangleOutlines(batch, footer, UITheme.Line * .55f, 1, _pixel);
        Text("Hover portrait for skills    /    Tab: Field guide", new Vector2(960, 1137),
            UITheme.InkMuted, 0.26f, 1400);
    }
}
