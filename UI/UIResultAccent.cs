using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>One brief geometric flourish; no textures, emitters, or gameplay state.</summary>
public sealed class UIResultAccent : UIControl
{
    private readonly Texture2D _pixel;
    private readonly bool _won;
    private float _age;
    public UIResultAccent(Texture2D pixel, bool won) : base(-100)
    { _pixel = pixel; _won = won; }
    public override void Update(GameTime time) => _age += (float)time.ElapsedGameTime.TotalSeconds;
    public override void Draw(SpriteBatch batch, GameTime time)
    {
        if (UIMotion.Reduced || _age >= 1.2f) return;
        Rectangle panel = UITheme.PanelRect(batch.GraphicsDevice.Viewport);
        float progress = MathHelper.Clamp(_age / 1.2f, 0, 1);
        float fade = (1 - progress) * UIMotion.Reveal(_age, 0.18f);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * MathHelper.TwoPi / 12;
            float radius = _won ? 32 + progress * 100 : 110 - progress * 60;
            Vector2 position = new(panel.Center.X + MathF.Cos(angle) * (panel.Width / 2 + radius),
                panel.Center.Y + MathF.Sin(angle) * (panel.Height / 3 + radius));
            batch.Draw(_pixel, position, null,
                (_won ? UITheme.RouteAccent(i) : UITheme.InkMuted) * fade,
                angle, Vector2.Zero, new Vector2(18, 4), SpriteEffects.None, 0);
        }
    }
}
