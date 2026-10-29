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
            UIUtils.DrawCenteredFit(batch, _font, text, UIDesktopLayout.MapPoint(point, vp), width * scale, size * scale, color);
        Text("FIDA", new Vector2(150, 65), UITheme.Ink, 0.9f, 200);
        Text("ENEMY", new Vector2(90, 345), UITheme.InkMuted, 0.26f, 115);
        Text("ALLY", new Vector2(90, 865), UITheme.InkMuted, 0.26f, 115);
        foreach (int y in new[] { 115, 1110 })
            batch.Draw(_pixel, UIDesktopLayout.MapRectangle(new Rectangle(64, y, 1792, 1), vp), UITheme.Line * 0.6f);
        Text($"TURN {_game.TurnNumber + 1:00}", new Vector2(960, 65), UITheme.InkMuted, 0.3f, 700);
        Text("HOVER PORTRAIT: SKILLS    /    TAB: FIELD GUIDE", new Vector2(960, 1150),
            UITheme.InkMuted, 0.26f, 1000);
    }
}
