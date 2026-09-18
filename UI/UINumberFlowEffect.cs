using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// A text-free directional effect that carries golden particles from the
    /// selected addend into the base number being changed.
    /// </summary>
    public sealed class UINumberFlowEffect : UIControl
    {
        private const float DurationSeconds = 1.05f;
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

            DrawEndpointPulse(sb, _originBounds, progress, outward: true);
            DrawEndpointPulse(sb, _targetBounds, progress, outward: false);
            DrawPath(sb, origin, control, target);

            // Several offset particles form one readable stream. The brightest
            // head moves toward the receiving number while its tail fades behind it.
            float head = MathHelper.Clamp(progress * 1.28f, 0f, 1.12f);
            for (int i = 0; i < 15; i++)
            {
                float t = head - i * 0.055f;
                if (t < 0f || t > 1f) continue;
                Vector2 point = QuadraticBezier(origin, control, target,
                    MathHelper.SmoothStep(0f, 1f, t));
                float strength = 1f - i / 16f;
                int size = 5 + (int)(strength * 8f);
                Color color = Color.Lerp(UITheme.Gold, UITheme.GoldBright, strength)
                    * (0.28f + strength * 0.72f);
                sb.Draw(_pixel, point, null, color, MathHelper.PiOver4,
                    new Vector2(0.5f), new Vector2(size, size),
                    SpriteEffects.None, 0f);
            }
        }

        private void DrawEndpointPulse(SpriteBatch sb, Rectangle bounds, float progress, bool outward)
        {
            float wave = 0.5f + 0.5f * (float)Math.Sin(progress * MathHelper.TwoPi * 3f);
            int expansion = 4 + (int)(wave * 6f);
            if (!outward) expansion += (int)(progress * 4f);
            var pulseBounds = new Rectangle(
                bounds.X - expansion,
                bounds.Y - expansion,
                bounds.Width + expansion * 2,
                bounds.Height + expansion * 2);
            UIUtils.DrawRectangleOutlines(sb, pulseBounds,
                Color.Lerp(UITheme.Gold, UITheme.GoldBright, wave), 3, _pixel);
        }

        private void DrawPath(SpriteBatch sb, Vector2 origin, Vector2 control, Vector2 target)
        {
            Vector2 previous = origin;
            for (int segment = 1; segment <= 24; segment++)
            {
                float t = segment / 24f;
                Vector2 current = QuadraticBezier(origin, control, target, t);
                DrawLine(sb, previous, current, UITheme.Gold * 0.28f, 2f);
                previous = current;
            }
        }

        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 delta = end - start;
            if (delta.LengthSquared() <= 0.01f) return;
            sb.Draw(_pixel, start, null, color, (float)Math.Atan2(delta.Y, delta.X),
                Vector2.Zero, new Vector2(delta.Length(), thickness), SpriteEffects.None, 0f);
        }

        private static Vector2 QuadraticBezier(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * a + 2f * inverse * t * control + t * t * b;
        }
    }
}
