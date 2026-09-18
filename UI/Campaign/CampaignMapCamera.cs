using System;
using Microsoft.Xna.Framework;

namespace Fida.UI.Campaign
{
    /// <summary>A resolution-independent 2D camera for the campaign graph.</summary>
    public sealed class CampaignMapCamera
    {
        private readonly Rectangle _contentBounds;
        private readonly float _padding;
        private readonly float _minimumZoom;
        private readonly float _maximumZoom;
        private Rectangle _viewport;
        private Vector2 _center;

        public float Zoom { get; private set; } = 1f;
        public Rectangle Viewport => _viewport;
        public Vector2 Center => _center;

        public CampaignMapCamera(Rectangle contentBounds, float padding, float minimumZoom, float maximumZoom)
        {
            _contentBounds = contentBounds;
            _padding = Math.Max(0f, padding);
            _minimumZoom = Math.Max(0.05f, minimumZoom);
            _maximumZoom = Math.Max(_minimumZoom, maximumZoom);
            _center = new Vector2(contentBounds.Center.X, contentBounds.Center.Y);
        }

        public void SetViewport(Rectangle viewport, bool fitWhenFirstAssigned = true)
        {
            bool firstAssignment = _viewport.Width <= 0 || _viewport.Height <= 0;
            _viewport = viewport;
            if (firstAssignment && fitWhenFirstAssigned)
                FitAll();
            else
                ClampCenter();
        }

        public void FitAll()
        {
            if (_viewport.Width <= 0 || _viewport.Height <= 0) return;

            float width = Math.Max(1f, _contentBounds.Width + _padding * 2f);
            float height = Math.Max(1f, _contentBounds.Height + _padding * 2f);
            Zoom = MathHelper.Clamp(Math.Min(_viewport.Width / width, _viewport.Height / height),
                _minimumZoom, _maximumZoom);
            _center = new Vector2(_contentBounds.Center.X, _contentBounds.Center.Y);
            ClampCenter();
        }

        public Vector2 WorldToScreen(Vector2 world) =>
            new(_viewport.Center.X + (world.X - _center.X) * Zoom,
                _viewport.Center.Y + (world.Y - _center.Y) * Zoom);

        public Vector2 ScreenToWorld(Vector2 screen) =>
            new(_center.X + (screen.X - _viewport.Center.X) / Zoom,
                _center.Y + (screen.Y - _viewport.Center.Y) / Zoom);

        public Rectangle WorldToScreen(Rectangle world)
        {
            Vector2 topLeft = WorldToScreen(new Vector2(world.Left, world.Top));
            return new Rectangle((int)Math.Round(topLeft.X), (int)Math.Round(topLeft.Y),
                Math.Max(1, (int)Math.Round(world.Width * Zoom)),
                Math.Max(1, (int)Math.Round(world.Height * Zoom)));
        }

        /// <summary>Moves the camera in world units.</summary>
        public void Pan(Vector2 worldDelta)
        {
            _center += worldDelta;
            ClampCenter();
        }

        /// <summary>Moves graph content by a screen-space drag delta.</summary>
        public void Drag(Vector2 screenDelta) => Pan(-screenDelta / Zoom);

        public void ZoomAt(float factor, Vector2 screenAnchor)
        {
            if (factor <= 0f || _viewport.Width <= 0 || _viewport.Height <= 0) return;

            Vector2 anchoredWorldPoint = ScreenToWorld(screenAnchor);
            Zoom = MathHelper.Clamp(Zoom * factor, _minimumZoom, _maximumZoom);
            _center = anchoredWorldPoint - (screenAnchor - new Vector2(_viewport.Center.X, _viewport.Center.Y)) / Zoom;
            ClampCenter();
        }

        public void EnsureVisible(Vector2 worldPoint, float marginInPixels = 90f)
        {
            Vector2 screen = WorldToScreen(worldPoint);
            float left = _viewport.Left + marginInPixels;
            float right = _viewport.Right - marginInPixels;
            float top = _viewport.Top + marginInPixels;
            float bottom = _viewport.Bottom - marginInPixels;
            Vector2 correction = Vector2.Zero;

            if (screen.X < left) correction.X = screen.X - left;
            else if (screen.X > right) correction.X = screen.X - right;
            if (screen.Y < top) correction.Y = screen.Y - top;
            else if (screen.Y > bottom) correction.Y = screen.Y - bottom;

            Pan(correction / Zoom);
        }

        private void ClampCenter()
        {
            if (_viewport.Width <= 0 || _viewport.Height <= 0) return;

            float halfWidth = _viewport.Width / (2f * Zoom);
            float halfHeight = _viewport.Height / (2f * Zoom);
            float minX = _contentBounds.Left - _padding + halfWidth;
            float maxX = _contentBounds.Right + _padding - halfWidth;
            float minY = _contentBounds.Top - _padding + halfHeight;
            float maxY = _contentBounds.Bottom + _padding - halfHeight;

            _center.X = minX > maxX ? _contentBounds.Center.X : MathHelper.Clamp(_center.X, minX, maxX);
            _center.Y = minY > maxY ? _contentBounds.Center.Y : MathHelper.Clamp(_center.Y, minY, maxY);
        }
    }
}
