using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI.Campaign
{
    /// <summary>
    /// Interactive campaign browser. It owns rendering, hit testing and camera navigation,
    /// while campaign rules and node data remain in the Campaign module.
    /// </summary>
    public sealed class UICampaignMap : UIControl
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Texture2D _pixel;
        private readonly Texture2D _antialiasedLine;
        private readonly SpriteFont _font;
        private readonly SpriteFont _titleFont;
        private readonly IReadOnlyList<CampaignNode> _nodes;
        private readonly CampaignProgress _progress;
        private readonly CampaignMapOptions _options;
        private readonly CampaignMapCamera _camera;

        private MouseState _previousMouse;
        private KeyboardState _previousKeyboard;
        private GamePadState _previousGamePad;
        private bool _inputInitialized;
        private string _pressedTarget = "";
        private Point _pressPosition;
        private bool _dragging;
        private int _selectedIndex;
        private string _pendingNodeId = "";
        private bool _backRequested;

        public float Zoom => _camera.Zoom;
        public string SelectedNodeId => _selectedIndex >= 0 && _selectedIndex < _nodes.Count
            ? _nodes[_selectedIndex].Id
            : "";

        public UICampaignMap(int id, GraphicsDevice graphicsDevice, Texture2D pixel,
            Texture2D antialiasedLine, SpriteFont font, SpriteFont titleFont,
            IReadOnlyList<CampaignNode> nodes, CampaignProgress progress,
            CampaignMapOptions options = null) : base(id)
        {
            _graphicsDevice = graphicsDevice;
            _pixel = pixel;
            _antialiasedLine = antialiasedLine ?? pixel;
            _font = font;
            _titleFont = titleFont;
            _nodes = nodes ?? Array.Empty<CampaignNode>();
            _progress = progress;
            _options = options ?? new CampaignMapOptions();
            _camera = new CampaignMapCamera(CalculateContentBounds(), _options.ContentPadding,
                _options.MinimumZoom, _options.MaximumZoom);
            _camera.SetViewport(_options.GetMapViewport(_graphicsDevice.Viewport));
            _selectedIndex = FindInitialSelection();
        }

        public bool TryConsumeSelectedNode(out string nodeId)
        {
            nodeId = _pendingNodeId;
            _pendingNodeId = "";
            return !string.IsNullOrEmpty(nodeId);
        }

        public bool ConsumeBackRequest()
        {
            bool requested = _backRequested;
            _backRequested = false;
            return requested;
        }

        public void ZoomIn() => _camera.ZoomAt(1.2f, ViewportCenter());
        public void ZoomOut() => _camera.ZoomAt(1f / 1.2f, ViewportCenter());
        public void FitAll() => _camera.FitAll();

        public override void Update(GameTime gameTime)
        {
            EnsureViewport();
            var keyboard = Keyboard.GetState();
            var gamePad = GamePad.GetState(PlayerIndex.One);

            if (!_inputInitialized)
            {
                _previousKeyboard = keyboard;
                _previousGamePad = gamePad;
                return;
            }

            float seconds = Math.Min(0.05f, (float)gameTime.ElapsedGameTime.TotalSeconds);
            const float panSpeed = 620f;
            Vector2 pan = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.A)) pan.X -= panSpeed * seconds / _camera.Zoom;
            if (keyboard.IsKeyDown(Keys.D)) pan.X += panSpeed * seconds / _camera.Zoom;
            if (keyboard.IsKeyDown(Keys.W)) pan.Y -= panSpeed * seconds / _camera.Zoom;
            if (keyboard.IsKeyDown(Keys.S)) pan.Y += panSpeed * seconds / _camera.Zoom;
            _camera.Pan(pan);

            if (Pressed(keyboard, Keys.Left)) MoveSelection(new Vector2(-1, 0));
            if (Pressed(keyboard, Keys.Right)) MoveSelection(new Vector2(1, 0));
            if (Pressed(keyboard, Keys.Up)) MoveSelection(new Vector2(0, -1));
            if (Pressed(keyboard, Keys.Down)) MoveSelection(new Vector2(0, 1));
            if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space)) ConfirmSelection();
            if (Pressed(keyboard, Keys.Escape)) _backRequested = true;
            if (Pressed(keyboard, Keys.Home) || Pressed(keyboard, Keys.R)) FitAll();
            if (Pressed(keyboard, Keys.OemPlus) || Pressed(keyboard, Keys.Add) || Pressed(keyboard, Keys.E)) ZoomIn();
            if (Pressed(keyboard, Keys.OemMinus) || Pressed(keyboard, Keys.Subtract) || Pressed(keyboard, Keys.Q)) ZoomOut();

            if (gamePad.IsConnected)
            {
                if (Pressed(gamePad, Buttons.DPadLeft)) MoveSelection(new Vector2(-1, 0));
                if (Pressed(gamePad, Buttons.DPadRight)) MoveSelection(new Vector2(1, 0));
                if (Pressed(gamePad, Buttons.DPadUp)) MoveSelection(new Vector2(0, -1));
                if (Pressed(gamePad, Buttons.DPadDown)) MoveSelection(new Vector2(0, 1));
                if (Pressed(gamePad, Buttons.A)) ConfirmSelection();
                if (Pressed(gamePad, Buttons.B) || Pressed(gamePad, Buttons.Back)) _backRequested = true;
                if (Pressed(gamePad, Buttons.Y)) FitAll();
                if (Pressed(gamePad, Buttons.RightShoulder)) ZoomIn();
                if (Pressed(gamePad, Buttons.LeftShoulder)) ZoomOut();

                Vector2 stick = gamePad.ThumbSticks.Right;
                if (stick.LengthSquared() > 0.08f)
                    _camera.Pan(new Vector2(stick.X, -stick.Y) * panSpeed * seconds / _camera.Zoom);

                float triggerZoom = gamePad.Triggers.Right - gamePad.Triggers.Left;
                if (Math.Abs(triggerZoom) > 0.08f)
                    _camera.ZoomAt((float)Math.Pow(1.8, triggerZoom * seconds), ViewportCenter());
            }

            _previousKeyboard = keyboard;
            _previousGamePad = gamePad;
        }

        public override void Update(MouseState mouse)
        {
            EnsureViewport();
            if (!_inputInitialized)
            {
                _previousMouse = mouse;
                _previousKeyboard = Keyboard.GetState();
                _previousGamePad = GamePad.GetState(PlayerIndex.One);
                _inputInitialized = true;
                return;
            }

            Point position = new(mouse.X, mouse.Y);
            if (_camera.Viewport.Contains(position))
            {
                int wheelDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
                if (wheelDelta != 0)
                    _camera.ZoomAt((float)Math.Pow(1.15, wheelDelta / 120f), position.ToVector2());

                int hoveredNode = HitNode(position);
                if (hoveredNode >= 0) _selectedIndex = hoveredNode;
            }

            bool leftPressed = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
            bool rightPressed = mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;
            if (leftPressed || rightPressed)
            {
                _pressPosition = position;
                _dragging = false;
                _pressedTarget = rightPressed ? "map" : HitTarget(position);
            }

            bool dragHeld = mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed;
            if (dragHeld && _pressedTarget == "map")
            {
                Point delta = position - _previousMouse.Position;
                if (Vector2.Distance(position.ToVector2(), _pressPosition.ToVector2()) > 4f) _dragging = true;
                if (_dragging) _camera.Drag(delta.ToVector2());
            }

            bool released = (mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed) ||
                            (mouse.RightButton == ButtonState.Released && _previousMouse.RightButton == ButtonState.Pressed);
            if (released)
            {
                string releasedTarget = HitTarget(position);
                if (!_dragging && releasedTarget == _pressedTarget)
                    ActivateTarget(releasedTarget);
                _pressedTarget = "";
                _dragging = false;
            }

            _previousMouse = mouse;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            EnsureViewport();
            Rectangle viewport = _camera.Viewport;

            DrawHeader(spriteBatch);
            spriteBatch.Draw(_pixel, viewport, UITheme.Panel);
            UIUtils.DrawRectangleOutlines(spriteBatch, viewport, UITheme.Gold, 3, _pixel);

            DrawConnections(spriteBatch);
            for (int i = 0; i < _nodes.Count; i++) DrawNode(spriteBatch, i);
            DrawFooter(spriteBatch);
        }

        private void DrawHeader(SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDevice.Viewport;
            DrawCentered(spriteBatch, _titleFont, "Echoes of the Crown - Campaign Map",
                new Vector2(viewport.Width / 2f, 34f), viewport.Width - 80f, 0.58f, UITheme.PanelLight);
            DrawCentered(spriteBatch, _font,
                "Browse: drag or WASD | Zoom: wheel or +/- | Select: arrows or D-pad | Open: Enter or A",
                new Vector2(viewport.Width / 2f, 87f), viewport.Width - 80f, 0.17f, UITheme.GoldBright);
        }

        private void DrawConnections(SpriteBatch spriteBatch)
        {
            var positions = _nodes.ToDictionary(n => n.Id, n => _camera.WorldToScreen(new Vector2(n.MapX, n.MapY)));
            foreach (var node in _nodes)
            {
                if (!positions.TryGetValue(node.Id, out Vector2 nodeEnd)) continue;
                foreach (string parentId in node.Parents)
                {
                    if (!positions.TryGetValue(parentId, out Vector2 start)) continue;
                    Vector2 end = nodeEnd;
                    if (!ClipLine(_camera.Viewport, ref start, ref end)) continue;
                    bool active = _progress.IsCompleted(parentId) || _progress.IsCompleted(node.Id);
                    DrawLine(spriteBatch, start, end, active ? UITheme.Gold : UITheme.InkMuted, active ? 3 : 2);
                }
            }
        }

        private void DrawNode(SpriteBatch spriteBatch, int index)
        {
            CampaignNode node = _nodes[index];
            Rectangle bounds = NodeScreenBounds(node);
            if (!_camera.Viewport.Contains(bounds)) return;

            bool unlocked = CampaignCatalog.IsUnlocked(_progress, node);
            bool completed = _progress.IsCompleted(node.Id);
            bool selected = index == _selectedIndex;
            Color background = completed ? new Color(220, 197, 139) : unlocked ? UITheme.PanelLight : UITheme.PanelDark;
            Color foreground = unlocked ? UITheme.Ink : UITheme.InkMuted;

            spriteBatch.Draw(_pixel, bounds, background);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds,
                selected ? UITheme.GoldBright : completed ? UITheme.Gold : UITheme.InkMuted,
                selected ? 4 : 2, _pixel);

            string marker = completed ? "[C] " : unlocked ? "" : "[L] ";
            DrawCentered(spriteBatch, _font, marker + node.Id + "  " + node.Name,
                bounds.Center.ToVector2(), bounds.Width - 14f,
                MathHelper.Clamp(0.24f * _camera.Zoom, 0.14f, 0.43f), foreground);
        }

        private void DrawFooter(SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDevice.Viewport;
            Rectangle minus = MinusButton(viewport);
            Rectangle fit = FitButton(viewport);
            Rectangle plus = PlusButton(viewport);
            Rectangle back = BackButton(viewport);
            DrawSmallButton(spriteBatch, minus, "-");
            DrawSmallButton(spriteBatch, fit, "Fit");
            DrawSmallButton(spriteBatch, plus, "+");
            DrawSmallButton(spriteBatch, back, "Back to Main Menu");

            if (_selectedIndex >= 0 && _selectedIndex < _nodes.Count)
            {
                CampaignNode selected = _nodes[_selectedIndex];
                string status = _progress.IsCompleted(selected.Id) ? "CLEARED" :
                    CampaignCatalog.IsUnlocked(_progress, selected) ? "OPEN" : "LOCKED";
                string detail = $"{selected.Chapter} | {selected.Id}: {selected.Name} | {selected.SkillBudget} SP | {status}";
#if FIDA_ANDROID
                DrawCentered(spriteBatch, _font, detail,
                    new Vector2(viewport.Width / 2f, viewport.Height - 122f),
                    viewport.Width - 56f, 0.26f, UITheme.PanelLight);
#else
                float centerX = (plus.Right + back.Left) / 2f;
                DrawCentered(spriteBatch, _font, detail, new Vector2(centerX, viewport.Height - 47f),
                    Math.Max(120f, back.Left - plus.Right - 35f), 0.18f, UITheme.PanelLight);
#endif
            }
        }

        private void DrawSmallButton(SpriteBatch spriteBatch, Rectangle bounds, string text)
        {
            spriteBatch.Draw(_pixel, bounds, UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds, UITheme.Gold, 2, _pixel);
            DrawCentered(spriteBatch, _font, text, bounds.Center.ToVector2(), bounds.Width - 12f, 0.19f, UITheme.Ink);
        }

        private void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 center,
            float maximumWidth, float preferredScale, Color color)
        {
            text = UIFontGuard.Sanitize(font, text);
            Vector2 measured = font.MeasureString(text);
            if (measured.X <= 0f) return;
            float scale = Math.Min(preferredScale, maximumWidth / measured.X);
            scale = Math.Max(0.09f, scale);
            spriteBatch.DrawString(font, text, center - measured * scale / 2f, color, 0f,
                Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void MoveSelection(Vector2 direction)
        {
            int next = CampaignMapNavigator.FindNext(_nodes, _selectedIndex, direction);
            if (next < 0 || next == _selectedIndex) return;
            _selectedIndex = next;
            CampaignNode node = _nodes[_selectedIndex];
            _camera.EnsureVisible(new Vector2(node.MapX, node.MapY));
        }

        private void ConfirmSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _nodes.Count) return;
            CampaignNode node = _nodes[_selectedIndex];
            if (CampaignCatalog.IsUnlocked(_progress, node)) _pendingNodeId = node.Id;
        }

        private int HitNode(Point screenPoint)
        {
            if (!_camera.Viewport.Contains(screenPoint)) return -1;
            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                if (NodeScreenBounds(_nodes[i]).Contains(screenPoint)) return i;
            }
            return -1;
        }

        private string HitTarget(Point point)
        {
            var viewport = _graphicsDevice.Viewport;
            if (MinusButton(viewport).Contains(point)) return "zoom_out";
            if (FitButton(viewport).Contains(point)) return "fit";
            if (PlusButton(viewport).Contains(point)) return "zoom_in";
            if (BackButton(viewport).Contains(point)) return "back";
            int node = HitNode(point);
            if (node >= 0) return "node:" + node;
            return _camera.Viewport.Contains(point) ? "map" : "";
        }

        private void ActivateTarget(string target)
        {
            if (target == "zoom_out") ZoomOut();
            else if (target == "fit") FitAll();
            else if (target == "zoom_in") ZoomIn();
            else if (target == "back") _backRequested = true;
            else if (target.StartsWith("node:", StringComparison.Ordinal) &&
                     int.TryParse(target.AsSpan(5), out int nodeIndex))
            {
                _selectedIndex = nodeIndex;
                ConfirmSelection();
            }
        }

        private Rectangle CalculateContentBounds()
        {
            if (_nodes.Count == 0) return new Rectangle(0, 0, 1, 1);
            float halfWidth = _options.NodeWidth / 2f;
            float halfHeight = _options.NodeHeight / 2f;
            int left = (int)Math.Floor(_nodes.Min(n => n.MapX - halfWidth));
            int top = (int)Math.Floor(_nodes.Min(n => n.MapY - halfHeight));
            int right = (int)Math.Ceiling(_nodes.Max(n => n.MapX + halfWidth));
            int bottom = (int)Math.Ceiling(_nodes.Max(n => n.MapY + halfHeight));
            return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }

        private Rectangle NodeScreenBounds(CampaignNode node)
        {
            var world = new Rectangle((int)(node.MapX - _options.NodeWidth / 2f),
                (int)(node.MapY - _options.NodeHeight / 2f),
                (int)_options.NodeWidth, (int)_options.NodeHeight);
            return _camera.WorldToScreen(world);
        }

        private int FindInitialSelection()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (CampaignCatalog.IsUnlocked(_progress, _nodes[i]) && !_progress.IsCompleted(_nodes[i].Id)) return i;
            }
            return _nodes.Count > 0 ? 0 : -1;
        }

        private void EnsureViewport()
        {
            Rectangle desired = _options.GetMapViewport(_graphicsDevice.Viewport);
            if (desired != _camera.Viewport) _camera.SetViewport(desired, false);
        }

        private Vector2 ViewportCenter() => _camera.Viewport.Center.ToVector2();

        private bool Pressed(KeyboardState current, Keys key) =>
            current.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

        private bool Pressed(GamePadState current, Buttons button) =>
            current.IsButtonDown(button) && _previousGamePad.IsButtonUp(button);

        private static Rectangle MinusButton(Viewport viewport)
        {
#if FIDA_ANDROID
            return new Rectangle(28, viewport.Height - 78, 72, 56);
#else
            return new Rectangle(36, viewport.Height - 76, 54, 50);
#endif
        }

        private static Rectangle FitButton(Viewport viewport)
        {
#if FIDA_ANDROID
            return new Rectangle(112, viewport.Height - 78, 92, 56);
#else
            return new Rectangle(100, viewport.Height - 76, 82, 50);
#endif
        }

        private static Rectangle PlusButton(Viewport viewport)
        {
#if FIDA_ANDROID
            return new Rectangle(244, viewport.Height - 78, 72, 56);
#else
            return new Rectangle(192, viewport.Height - 76, 54, 50);
#endif
        }

        private static Rectangle BackButton(Viewport viewport)
        {
#if FIDA_ANDROID
            int left = Math.Min(340, Math.Max(28, viewport.Width / 3));
            return new Rectangle(left, viewport.Height - 78,
                Math.Max(180, viewport.Width - left - 28), 56);
#else
            return new Rectangle(viewport.Width - 286, viewport.Height - 76, 250, 50);
#endif
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length <= 0.01f) return;
            float angle = (float)Math.Atan2(delta.Y, delta.X);
            Vector2 origin = new(0f, _antialiasedLine.Height / 2f);
            Vector2 scale = new(length / _antialiasedLine.Width,
                (thickness + 2f) / _antialiasedLine.Height);
            spriteBatch.Draw(_antialiasedLine, start, null, color, angle, origin,
                scale, SpriteEffects.None, 0f);
        }

        private static bool ClipLine(Rectangle rectangle, ref Vector2 start, ref Vector2 end)
        {
            float t0 = 0f;
            float t1 = 1f;
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            if (!Clip(-dx, start.X - rectangle.Left, ref t0, ref t1) ||
                !Clip(dx, rectangle.Right - start.X, ref t0, ref t1) ||
                !Clip(-dy, start.Y - rectangle.Top, ref t0, ref t1) ||
                !Clip(dy, rectangle.Bottom - start.Y, ref t0, ref t1)) return false;

            Vector2 original = start;
            Vector2 delta = end - start;
            start = original + delta * t0;
            end = original + delta * t1;
            return true;
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Math.Abs(p) < 0.0001f) return q >= 0f;
            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }
    }
}
