using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// A short, text-free directional trace from the
    /// selected addend into the base number being changed.
    /// </summary>
    public sealed class UINumberFlowEffect : UIControl
    {
        private const float DurationSeconds = 0.42f;
        private readonly Texture2D _pixel;
        private Rectangle _originBounds;
        private Rectangle _targetBounds;
        private float _elapsed;

        public bool IsPlaying { get; private set; }

        public UINumberFlowEffect(int id, Texture2D pixel) : base(id) => _pixel = pixel;

        public void Start(Rectangle originBounds, Rectangle targetBounds)
        {
            _originBounds = originBounds;
            _targetBounds = targetBounds;
            _elapsed = 0f;
            IsPlaying = true;
            IsVisible = true;
        }

        public void Stop()
        {
            IsPlaying = false;
            IsVisible = false;
        }

        public override void Update(GameTime gameTime)
        {
            if (UIMotion.Reduced) { Stop(); return; }
            if (!IsPlaying) return;
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_elapsed >= DurationSeconds) Stop();
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            if (!IsPlaying || !IsVisible) return;

            float progress = MathHelper.Clamp(_elapsed / DurationSeconds, 0f, 1f);
            Vector2 origin = _originBounds.Center.ToVector2();
            Vector2 target = _targetBounds.Center.ToVector2();
            float distance = Vector2.Distance(origin, target);
            Vector2 control = (origin + target) * 0.5f;
            control.Y -= MathHelper.Clamp(distance * 0.22f, 48f, 150f);

            DrawEndpointPulse(sb, _targetBounds, progress, outward: false);

            // Several offset particles form one readable stream. The brightest
            // head moves toward the receiving number while its tail fades behind it.
            float head = MathHelper.Clamp(progress * 1.28f, 0f, 1.12f);
            for (int i = 0; i < 6; i++)
            {
                float t = head - i * 0.055f;
                if (t < 0f || t > 1f) continue;
                Vector2 point = QuadraticBezier(origin, control, target,
                    MathHelper.SmoothStep(0f, 1f, t));
                float strength = 1f - i / 16f;
                float scale = UIDesktopLayout.LayoutScale(sb.GraphicsDevice.Viewport);
                Color color = UITheme.Gold * strength;
                Vector2 tangent = QuadraticBezier(origin, control, target, Math.Min(1, t + 0.02f)) - point;
                sb.Draw(_pixel, point, null, color, MathF.Atan2(tangent.Y, tangent.X),
                    new Vector2(0.5f), new Vector2((8 + strength * 14) * scale, Math.Max(1, 2 * scale)),
                    SpriteEffects.None, 0f);
            }
        }

        private void DrawEndpointPulse(SpriteBatch sb, Rectangle bounds, float progress, bool outward)
        {
            float wave = MathHelper.Clamp((progress - 0.65f) / 0.35f, 0, 1);
            int expansion = 2 + (int)(wave * 3);
            var pulseBounds = new Rectangle(
                bounds.X - expansion,
                bounds.Y - expansion,
                bounds.Width + expansion * 2,
                bounds.Height + expansion * 2);
            Color endpoint = UITheme.Gold * (MathF.Sin(wave * MathHelper.Pi));
            UIUtils.DrawGlowFrame(sb, pulseBounds, _pixel,
                endpoint, 1, 0.72f);
        }

        private static Vector2 QuadraticBezier(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * a + 2f * inverse * t * control + t * t * b;
        }
    }
}
