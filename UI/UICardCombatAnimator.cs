using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// Drives whole-card anticipation, lunge, impact and recovery. It changes only
    /// presentation transforms; gameplay remains authoritative in GameManager.
    /// </summary>
    public sealed class UICardCombatAnimator : UIControl
    {
        private const float Duration = 0.95f;
        private UIPlayer _source;
        private UIPlayer _target;
        private float _elapsed;
        private bool _damaging;

        public bool IsPlaying { get; private set; }

        public UICardCombatAnimator(int id) : base(id) => IsVisible = false;

        public void Start(UIPlayer source, UIPlayer target, bool damaging)
        {
            Stop();
            if (source == null || target == null) return;
            _source = source;
            _target = target;
            _damaging = damaging;
            _elapsed = 0f;
            IsPlaying = true;
            _target.DeferHealthVisual = true;
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsPlaying) return;
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            float t = MathHelper.Clamp(_elapsed / Duration, 0f, 1f);

            Vector2 sourceCenter = _source.CardBounds.Center.ToVector2();
            Vector2 targetCenter = _target.CardBounds.Center.ToVector2();
            Vector2 direction = targetCenter - sourceCenter;
            if (direction.LengthSquared() > 0.01f) direction.Normalize();

            // Target lock: the whole selected card lifts and remains framed.
            _target.VisualScale = 1.025f;
            _target.SelectionStrength = 1f;

            if (!_damaging)
            {
                float wave = (float)Math.Sin(t * MathHelper.Pi);
                _source.VisualScale = 1f + wave * 0.035f;
                _target.VisualScale = 1.025f + wave * 0.025f;
                _target.ImpactStrength = wave * 0.45f;
                if (t >= 0.5f) _target.CommitDeferredHealthVisual();
            }
            else if (t < 0.22f)
            {
                // Anticipation: recoil away from the target before the strike.
                float p = MathHelper.SmoothStep(0f, 1f, t / 0.22f);
                _source.VisualOffset = -direction * (18f * p);
                _source.VisualScale = 1f - 0.035f * p;
            }
            else if (t < 0.56f)
            {
                // Lunge: never covers the full distance, retaining card readability.
                float p = MathHelper.SmoothStep(0f, 1f, (t - 0.22f) / 0.34f);
                float travel = Math.Min(Vector2.Distance(sourceCenter, targetCenter) * 0.24f, 165f);
                _source.VisualOffset = Vector2.Lerp(-direction * 18f, direction * travel, p);
                _source.VisualScale = MathHelper.Lerp(0.965f, 1.075f, p);
            }
            else
            {
                // Impact and recovery: target shakes while the attacker snaps back.
                float p = MathHelper.Clamp((t - 0.56f) / 0.44f, 0f, 1f);
                float recovery = 1f - MathHelper.SmoothStep(0f, 1f, p);
                float travel = Math.Min(Vector2.Distance(sourceCenter, targetCenter) * 0.24f, 165f);
                _source.VisualOffset = direction * travel * recovery;
                _source.VisualScale = 1f + 0.075f * recovery;

                float shake = (float)Math.Sin(p * MathHelper.Pi * 9f) * 15f * recovery;
                Vector2 perpendicular = new(-direction.Y, direction.X);
                _target.VisualOffset = perpendicular * shake;
                _target.ImpactStrength = recovery;
                _target.CommitDeferredHealthVisual();
            }

            if (_elapsed >= Duration) Stop();
        }

        public void Stop()
        {
            ResetCard(_source);
            ResetCard(_target);
            _source = null;
            _target = null;
            IsPlaying = false;
            _elapsed = 0f;
        }

        private static void ResetCard(UIPlayer card)
        {
            if (card == null) return;
            card.VisualOffset = Vector2.Zero;
            card.VisualScale = 1f;
            card.SelectionStrength = 0f;
            card.ImpactStrength = 0f;
            card.CommitDeferredHealthVisual();
            card.CommitDeferredNumberVisual();
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime) { }
    }
}
