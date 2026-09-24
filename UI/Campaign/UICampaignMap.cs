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
        private CampaignMapCamera _camera;
        private readonly CampaignMapHierarchy _hierarchy;
        private readonly List<string> _scope = new();
        private IReadOnlyList<string> _folders = Array.Empty<string>();
        private IReadOnlyList<int> _visibleNodes = Array.Empty<int>();
        private int _selectedFolder;

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
        private bool _desktopDetailsOpen;
#if FIDA_ANDROID
        private readonly List<int> _mobileChoices = new();
        private readonly List<Rectangle> _mobileChoiceBounds = new();
        private Rectangle _mobileBackBounds;
        private Rectangle _mobileModalBounds;
        private Rectangle _mobileModalEnterBounds;
        private Rectangle _mobileModalCloseBounds;
        private bool _mobileDetailsOpen;
#endif

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
            _hierarchy = new CampaignMapHierarchy(_nodes);
            _progress = progress;
            _options = options ?? new CampaignMapOptions();
            _camera = new CampaignMapCamera(CalculateContentBounds(), _options.ContentPadding,
                _options.MinimumZoom, _options.MaximumZoom);
            _camera.SetViewport(_options.GetMapViewport(_graphicsDevice.Viewport));
            _selectedIndex = FindInitialSelection();
            RefreshScope();
#if FIDA_ANDROID
            RebuildMobileChoices();
            LayoutMobile();
#endif
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
#if FIDA_ANDROID
            UpdateMobileKeyboard(gameTime);
            return;
#else
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
            if (Pressed(keyboard, Keys.Escape))
            {
                if (_desktopDetailsOpen) _desktopDetailsOpen = false;
                else GoBack();
            }
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
                if (Pressed(gamePad, Buttons.B) || Pressed(gamePad, Buttons.Back))
                {
                    if (_desktopDetailsOpen) _desktopDetailsOpen = false;
                    else GoBack();
                }
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
#endif
        }

        public override void Update(MouseState mouse)
        {
#if FIDA_ANDROID
            UpdateMobileMouse(mouse);
            return;
#else
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
            if (!_desktopDetailsOpen && _camera.Viewport.Contains(position))
            {
                int wheelDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
                if (wheelDelta != 0)
                    _camera.ZoomAt((float)Math.Pow(1.15, wheelDelta / 120f), position.ToVector2());

                int hoveredNode = HitNode(position);
                if (hoveredNode >= 0) _selectedIndex = hoveredNode;
                int hoveredFolder = HitFolder(position);
                if (hoveredFolder >= 0) _selectedFolder = hoveredFolder;
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
#endif
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
#if FIDA_ANDROID
            DrawMobile(spriteBatch, gameTime);
            return;
#else
            EnsureViewport();
            Rectangle viewport = _camera.Viewport;

            spriteBatch.Draw(_pixel, viewport, UITheme.Panel);
            UIUtils.DrawRectangleOutlines(spriteBatch, viewport, UITheme.Gold, 3, _pixel);

            if (_folders.Count > 0) DrawFolders(spriteBatch);
            if (_visibleNodes.Count > 0)
            {
                DrawConnections(spriteBatch);
                foreach (int i in _visibleNodes) DrawNode(spriteBatch, i);
            }
            DrawFooter(spriteBatch);
            if (_desktopDetailsOpen && _selectedIndex >= 0)
                DrawDesktopMissionModal(spriteBatch, _nodes[_selectedIndex]);
#endif
        }

#if FIDA_ANDROID
        private Rectangle MobilePanel()
        {
            Viewport viewport = _graphicsDevice.Viewport;
            Rectangle safe = UIMobileSafeArea.Get(viewport);
            int top = safe.Top + 72;
            return new Rectangle(safe.Left + 4, top,
                Math.Max(1, safe.Width - 8),
                Math.Max(1, safe.Bottom - top - 8));
        }

        private void LayoutMobile()
        {
            Rectangle panel = MobilePanel();
            _mobileChoiceBounds.Clear();

            int cardTop = panel.Top + 102;
            int footerHeight = 28;
            int gap = 12;
            int available = Math.Max(1, panel.Bottom - cardTop - footerHeight);
            int count = Math.Max(1, _folders.Count > 0 ? _folders.Count : _mobileChoices.Count);
            int cardHeight = Math.Clamp(
                (available - Math.Max(0, count - 1) * gap) / count, 94, 116);
            int cardWidth = Math.Max(1, panel.Width - 40);

            for (int i = 0; i < count && (_folders.Count > 0 || i < _mobileChoices.Count); i++)
            {
                _mobileChoiceBounds.Add(new Rectangle(panel.Left + 20,
                    cardTop + i * (cardHeight + gap), cardWidth, cardHeight));
            }

            Rectangle safe = UIMobileSafeArea.Get(_graphicsDevice.Viewport);
            _mobileBackBounds = new Rectangle(safe.Left + 8, safe.Top + 8, 54, 54);

            int modalWidth = Math.Max(1, panel.Width - 48);
            int modalHeight = Math.Min(620, Math.Max(1, panel.Height - 120));
            _mobileModalBounds = new Rectangle(panel.Center.X - modalWidth / 2,
                panel.Center.Y - modalHeight / 2, modalWidth, modalHeight);
            _mobileModalCloseBounds = new Rectangle(_mobileModalBounds.Right - 62,
                _mobileModalBounds.Top + 12, 48, 48);
            _mobileModalEnterBounds = new Rectangle(_mobileModalBounds.Left + 28,
                _mobileModalBounds.Bottom - 92, _mobileModalBounds.Width - 56, 64);
        }

        private void RebuildMobileChoices()
        {
            _mobileChoices.Clear();
            foreach (int i in _visibleNodes)
            {
                CampaignNode node = _nodes[i];
                if (! _progress.IsCompleted(node.Id) && CampaignCatalog.IsUnlocked(_progress, node))
                    _mobileChoices.Add(i);
            }

            // Once every open node is cleared, keep the browser useful with a small
            // review list instead of showing the entire completed graph again.
            if (_mobileChoices.Count == 0)
            {
                for (int j = _visibleNodes.Count - 1; j >= 0 && _mobileChoices.Count < 4; j--)
                {
                    int i = _visibleNodes[j];
                    if (CampaignCatalog.IsUnlocked(_progress, _nodes[i]))
                        _mobileChoices.Insert(0, i);
                }
            }

            if (_mobileChoices.Count == 0 && _visibleNodes.Count > 0)
                _mobileChoices.Add(_visibleNodes[0]);

            if (_mobileChoices.Count > 0 && !_mobileChoices.Contains(_selectedIndex))
                _selectedIndex = _mobileChoices[0];
        }

        private void DrawMobile(SpriteBatch spriteBatch, GameTime gameTime)
        {
            LayoutMobile();
            Rectangle panel = MobilePanel();
            Viewport viewport = _graphicsDevice.Viewport;
            Rectangle safe = UIMobileSafeArea.Get(viewport);
            DrawCentered(spriteBatch, _titleFont, _scope.LastOrDefault() ?? "Campaign",
                new Vector2(safe.Center.X, safe.Top + 32f), safe.Width - 150f,
                0.60f, UITheme.Ink);
            DrawBackIcon(spriteBatch, _mobileBackBounds);

            spriteBatch.Draw(_pixel, panel, UITheme.Panel);
            UIUtils.DrawGlowFrame(spriteBatch, panel, _pixel, UITheme.Gold, 2, 0.72f);
            UIUtils.DrawRectangleOutlines(spriteBatch,
                new Rectangle(panel.X + 8, panel.Y + 8, panel.Width - 16, panel.Height - 16),
                UITheme.AccentBlue * 0.52f, 1, _pixel);
            DrawMobileStructure(spriteBatch, panel);

            int completed = _progress.CompletedNodes.Count;
            DrawCentered(spriteBatch, _font, "ROUTES",
                new Vector2(panel.Left + 86, panel.Top + 38), 120f,
                0.56f, UITheme.GoldBright);
            DrawCentered(spriteBatch, _font, $"{completed} / {_nodes.Count}",
                new Vector2(panel.Right - 82, panel.Top + 38), 120f,
                0.56f, UITheme.InkMuted);
            Rectangle progressTrack = new(panel.Left + 30, panel.Top + 68,
                Math.Max(1, panel.Width - 60), 5);
            spriteBatch.Draw(_pixel, progressTrack, UITheme.PanelDark);
            float completion = _nodes.Count == 0 ? 0f :
                MathHelper.Clamp(completed / (float)_nodes.Count, 0f, 1f);
            spriteBatch.Draw(_pixel, new Rectangle(progressTrack.X, progressTrack.Y,
                (int)(progressTrack.Width * completion), progressTrack.Height),
                UITheme.AccentGreen);

            for (int i = 0; i < _mobileChoiceBounds.Count; i++)
            {
                if (_folders.Count > 0)
                {
                    DrawMobileFolder(spriteBatch, i);
                    continue;
                }
                CampaignNode node = _nodes[_mobileChoices[i]];
                Rectangle bounds = _mobileChoiceBounds[i];
                bool selected = _mobileChoices[i] == _selectedIndex;
                bool completedNode = _progress.IsCompleted(node.Id);
                Color background = selected ? UITheme.PanelPressed :
                    completedNode ? UITheme.PanelDark : UITheme.PanelLight;
                Color accent = completedNode ? UITheme.InkMuted : UITheme.RouteAccent(i);
                Color border = selected ? UITheme.GoldBright : accent;
                Color foreground = completedNode ? UITheme.InkMuted : UITheme.Ink;

                spriteBatch.Draw(_pixel, bounds, background);
                if (selected)
                {
                    float pulse = 0.72f + 0.22f * (float)Math.Sin(
                        gameTime.TotalGameTime.TotalSeconds * 5.5);
                    UIUtils.DrawGlowFrame(spriteBatch, bounds, _pixel, accent, 3, pulse);
                }
                UIUtils.DrawRectangleOutlines(spriteBatch, bounds, border,
                    selected ? 3 : 1, _pixel);
                spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top, 6,
                    bounds.Height), accent);
                int chipWidth = Math.Max(24, bounds.Width / 9);
                spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - chipWidth,
                    bounds.Top, chipWidth, 3), UITheme.AccentGreen * 0.85f);
                DrawCentered(spriteBatch, _font,
                    $"{node.Id}  {node.Name}",
                    new Vector2(bounds.Center.X - 8, bounds.Top + bounds.Height * 0.35f),
                    bounds.Width - 92f, 0.66f, foreground);
                DrawCentered(spriteBatch, _font,
                    $"{node.SkillBudget} SP   //   {node.RewardText}",
                    new Vector2(bounds.Center.X - 8, bounds.Top + bounds.Height * 0.72f),
                    bounds.Width - 92f, 0.52f,
                    completedNode ? UITheme.InkMuted : accent);
                DrawChevron(spriteBatch, new Vector2(bounds.Right - 34, bounds.Center.Y), accent);
            }

            CampaignNode selectedNode = _selectedIndex >= 0 && _selectedIndex < _nodes.Count
                ? _nodes[_selectedIndex] : null;
            if (_mobileDetailsOpen && selectedNode != null)
                DrawMissionModal(spriteBatch, viewport, selectedNode);
        }

        private void DrawMobileFolder(SpriteBatch spriteBatch, int index)
        {
            Rectangle bounds = _mobileChoiceBounds[index];
            var path = _scope.Concat(new[] { _folders[index] }).ToArray();
            IReadOnlyList<int> descendants = _hierarchy.Descendants(path);
            int cleared = descendants.Count(i => _progress.IsCompleted(_nodes[i].Id));
            Color accent = UITheme.RouteAccent(index);
            spriteBatch.Draw(_pixel, bounds, index == _selectedFolder ? UITheme.PanelPressed : UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds,
                index == _selectedFolder ? UITheme.GoldBright : accent, 2, _pixel);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top, 5, bounds.Height), accent);
            DrawCentered(spriteBatch, _font, _folders[index],
                new Vector2(bounds.Center.X - 8, bounds.Top + bounds.Height * 0.39f),
                bounds.Width - 90, 0.68f, UITheme.Ink);
            DrawCentered(spriteBatch, _font, $"{cleared} / {descendants.Count}",
                new Vector2(bounds.Center.X - 8, bounds.Top + bounds.Height * 0.73f),
                bounds.Width - 90, 0.48f, UITheme.GoldBright);
            DrawChevron(spriteBatch, new Vector2(bounds.Right - 34, bounds.Center.Y), accent);
        }

        private void DrawMobileButton(SpriteBatch spriteBatch, Rectangle bounds,
            string text, bool enabled, Color accent)
        {
            spriteBatch.Draw(_pixel, bounds, enabled ? UITheme.PanelLight : UITheme.PanelDark);
            if (enabled)
                UIUtils.DrawGlowFrame(spriteBatch, bounds, _pixel, accent, 2, 0.55f);
            else
                UIUtils.DrawRectangleOutlines(spriteBatch, bounds, UITheme.InkMuted, 1, _pixel);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top,
                Math.Max(18, bounds.Width / 6), 3), accent);
            DrawCentered(spriteBatch, _font, text, bounds.Center.ToVector2(),
                bounds.Width - 18f, 0.58f, enabled ? UITheme.Ink : UITheme.InkMuted);
        }

        private void DrawBackIcon(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.Draw(_pixel, bounds, UITheme.PanelLight);
            UIUtils.DrawGlowFrame(spriteBatch, bounds, _pixel, UITheme.AccentBlue, 2, 0.48f);
            Vector2 tip = new(bounds.Left + 15, bounds.Center.Y);
            Vector2 tail = new(bounds.Right - 13, bounds.Center.Y);
            DrawLine(spriteBatch, tip, tail, UITheme.Ink, 3);
            DrawLine(spriteBatch, tip, new Vector2(bounds.Left + 27, bounds.Top + 14),
                UITheme.Ink, 3);
            DrawLine(spriteBatch, tip, new Vector2(bounds.Left + 27, bounds.Bottom - 14),
                UITheme.Ink, 3);
        }

        private void DrawChevron(SpriteBatch spriteBatch, Vector2 center, Color color)
        {
            DrawLine(spriteBatch, new Vector2(center.X - 6, center.Y - 11), center, color, 3);
            DrawLine(spriteBatch, center, new Vector2(center.X - 6, center.Y + 11), color, 3);
        }

        private void DrawCloseIcon(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.Draw(_pixel, bounds, UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds, UITheme.AccentBlue, 1, _pixel);
            DrawLine(spriteBatch, new Vector2(bounds.Left + 14, bounds.Top + 14),
                new Vector2(bounds.Right - 14, bounds.Bottom - 14), UITheme.Ink, 3);
            DrawLine(spriteBatch, new Vector2(bounds.Right - 14, bounds.Top + 14),
                new Vector2(bounds.Left + 14, bounds.Bottom - 14), UITheme.Ink, 3);
        }

        private void DrawMobileStructure(SpriteBatch spriteBatch, Rectangle panel)
        {
            int left = panel.Left + 16;
            int right = panel.Right - 17;
            int top = panel.Top + 124;
            int bottom = panel.Bottom - 92;
            spriteBatch.Draw(_pixel, new Rectangle(left, top, 1,
                Math.Max(1, bottom - top)), UITheme.AccentBlue * 0.28f);
            spriteBatch.Draw(_pixel, new Rectangle(right, top, 1,
                Math.Max(1, bottom - top)), UITheme.AccentViolet * 0.28f);
            for (int y = top + 36; y < bottom; y += 150)
            {
                spriteBatch.Draw(_pixel, new Rectangle(left - 3, y, 7, 7),
                    UITheme.AccentBlue * 0.52f);
                spriteBatch.Draw(_pixel, new Rectangle(right - 3, y + 48, 7, 7),
                    UITheme.AccentGreen * 0.46f);
            }
        }

        private void DrawMissionModal(SpriteBatch spriteBatch, Viewport viewport,
            CampaignNode node)
        {
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * 0.72f);
            Rectangle modal = _mobileModalBounds;
            spriteBatch.Draw(_pixel, modal, UITheme.PanelDark);
            UIUtils.DrawGlowFrame(spriteBatch, modal, _pixel,
                UITheme.Gold, 3, 0.78f);
            UIUtils.DrawRectangleOutlines(spriteBatch,
                new Rectangle(modal.Left + 8, modal.Top + 8, modal.Width - 16, modal.Height - 16),
                UITheme.AccentBlue * 0.62f, 1, _pixel);
            spriteBatch.Draw(_pixel, new Rectangle(modal.Left, modal.Top,
                Math.Max(48, modal.Width / 3), 4), UITheme.AccentViolet);
            spriteBatch.Draw(_pixel, new Rectangle(modal.Right - Math.Max(48, modal.Width / 3),
                modal.Bottom - 4, Math.Max(48, modal.Width / 3), 4),
                UITheme.AccentGreen);
            DrawCloseIcon(spriteBatch, _mobileModalCloseBounds);

            DrawCentered(spriteBatch, _titleFont, $"{node.Id}  {node.Name}",
                new Vector2(modal.Center.X, modal.Top + 68), modal.Width - 150,
                0.58f, UITheme.Ink);
            DrawCentered(spriteBatch, _font,
                $"{node.Chapter}   //   {node.SkillBudget} SP",
                new Vector2(modal.Center.X, modal.Top + 122), modal.Width - 70,
                0.56f, UITheme.GoldBright);

            string story = WrapText(node.Story, modal.Width - 72, 0.56f, 4);
            DrawCentered(spriteBatch, _font, story,
                new Vector2(modal.Center.X, modal.Top + 210), modal.Width - 72,
                0.56f, UITheme.Ink);
            DrawCentered(spriteBatch, _font, node.RewardText,
                new Vector2(modal.Center.X, modal.Top + 314), modal.Width - 72,
                0.60f, UITheme.AccentGreen);

            int chipGap = 8;
            int chipWidth = Math.Max(1, (modal.Width - 48 - chipGap * 2) / 3);
            int chipY = modal.Bottom - 174;
            DrawDossierChip(spriteBatch, new Rectangle(modal.Left + 24, chipY,
                chipWidth, 58), "ENEMIES", node.Enemies.Count.ToString(), UITheme.Gold);
            DrawDossierChip(spriteBatch, new Rectangle(modal.Left + 24 + chipWidth + chipGap,
                chipY, chipWidth, 58), "BACKUP", node.Reinforcements.Count.ToString(),
                UITheme.AccentViolet);
            DrawDossierChip(spriteBatch, new Rectangle(modal.Left + 24 +
                (chipWidth + chipGap) * 2, chipY, chipWidth, 58), "CLASS",
                node.IsBoss ? "BOSS" : "STANDARD", UITheme.AccentGreen);

            bool enabled = CampaignCatalog.IsUnlocked(_progress, node);
            DrawMobileButton(spriteBatch, _mobileModalEnterBounds,
                enabled ? "Enter Mission" : "Unavailable", enabled, UITheme.AccentGreen);
            if (enabled)
                DrawChevron(spriteBatch,
                    new Vector2(_mobileModalEnterBounds.Right - 28,
                        _mobileModalEnterBounds.Center.Y), UITheme.AccentGreen);
        }

        private void DrawDossierChip(SpriteBatch spriteBatch, Rectangle bounds,
            string label, string value, Color accent)
        {
            spriteBatch.Draw(_pixel, bounds, UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds, accent, 1, _pixel);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top, 4,
                bounds.Height), accent);
            DrawCentered(spriteBatch, _font, label,
                new Vector2(bounds.Center.X, bounds.Top + 17), bounds.Width - 14,
                0.34f, UITheme.InkMuted);
            DrawCentered(spriteBatch, _font, value,
                new Vector2(bounds.Center.X, bounds.Top + 42), bounds.Width - 14,
                0.48f, UITheme.Ink);
        }

        private string WrapText(string text, float maximumWidth, float scale, int maximumLines)
        {
            if (string.IsNullOrWhiteSpace(text)) return "No mission briefing available.";
            var lines = new List<string>();
            string current = "";
            foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
                if (_font.MeasureString(UIFontGuard.Sanitize(_font, candidate)).X * scale <= maximumWidth)
                {
                    current = candidate;
                    continue;
                }
                if (!string.IsNullOrEmpty(current)) lines.Add(current);
                current = word;
                if (lines.Count == maximumLines - 1) break;
            }
            if (!string.IsNullOrEmpty(current) && lines.Count < maximumLines) lines.Add(current);
            return string.Join("\n", lines);
        }

        private void UpdateMobileKeyboard(GameTime gameTime)
        {
            LayoutMobile();
            KeyboardState keyboard = Keyboard.GetState();
            GamePadState gamePad = GamePad.GetState(PlayerIndex.One);
            if (!_inputInitialized)
            {
                _previousKeyboard = keyboard;
                _previousGamePad = gamePad;
                return;
            }

            if (!_mobileDetailsOpen && (Pressed(keyboard, Keys.Up) || Pressed(keyboard, Keys.Left) ||
                Pressed(gamePad, Buttons.DPadUp) || Pressed(gamePad, Buttons.DPadLeft)))
                MoveMobileSelection(-1);
            if (!_mobileDetailsOpen && (Pressed(keyboard, Keys.Down) || Pressed(keyboard, Keys.Right) ||
                Pressed(gamePad, Buttons.DPadDown) || Pressed(gamePad, Buttons.DPadRight)))
                MoveMobileSelection(1);
            if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space) ||
                Pressed(gamePad, Buttons.A))
            {
                if (_mobileDetailsOpen) ConfirmSelection();
                else if (_folders.Count > 0) EnterFolder(_selectedFolder);
                else _mobileDetailsOpen = _selectedIndex >= 0;
            }
            if (Pressed(keyboard, Keys.Escape) || Pressed(gamePad, Buttons.B) ||
                Pressed(gamePad, Buttons.Back))
            {
                if (_mobileDetailsOpen) _mobileDetailsOpen = false;
                else GoBack();
            }

            _previousKeyboard = keyboard;
            _previousGamePad = gamePad;
        }

        private void UpdateMobileMouse(MouseState mouse)
        {
            LayoutMobile();
            if (!_inputInitialized)
            {
                _previousMouse = mouse;
                _previousKeyboard = Keyboard.GetState();
                _previousGamePad = GamePad.GetState(PlayerIndex.One);
                _inputInitialized = true;
                return;
            }

            Point position = new(mouse.X, mouse.Y);

            bool pressed = mouse.LeftButton == ButtonState.Pressed &&
                _previousMouse.LeftButton == ButtonState.Released;
            if (pressed)
                _pressedTarget = HitMobileTarget(position);

            bool released = mouse.LeftButton == ButtonState.Released &&
                _previousMouse.LeftButton == ButtonState.Pressed;
            if (released)
            {
                string target = HitMobileTarget(position);
                if (target == _pressedTarget) ActivateMobileTarget(target);
                _pressedTarget = "";
            }

            _previousMouse = mouse;
        }

        private int HitMobileCard(Point point)
        {
            for (int i = _mobileChoiceBounds.Count - 1; i >= 0; i--)
            {
                if (_mobileChoiceBounds[i].Contains(point)) return i;
            }
            return -1;
        }

        private string HitMobileTarget(Point point)
        {
            if (_mobileDetailsOpen)
            {
                if (_mobileModalCloseBounds.Contains(point)) return "modal_close";
                if (_mobileModalEnterBounds.Contains(point)) return "modal_enter";
                return _mobileModalBounds.Contains(point) ? "" : "modal_close";
            }

            if (_mobileBackBounds.Contains(point)) return "back";
            int card = HitMobileCard(point);
            if (card >= 0) return "node:" + card;
            return "";
        }

        private void ActivateMobileTarget(string target)
        {
            if (target == "modal_enter") ConfirmSelection();
            else if (target == "modal_close") _mobileDetailsOpen = false;
            else if (target == "back") GoBack();
            else if (target.StartsWith("node:", StringComparison.Ordinal) &&
                int.TryParse(target.AsSpan(5), out int choice) &&
                choice >= 0 && choice < _mobileChoiceBounds.Count)
            {
                if (_folders.Count > 0) EnterFolder(choice);
                else
                {
                    _selectedIndex = _mobileChoices[choice];
                    _mobileDetailsOpen = true;
                }
            }
        }

        private void MoveMobileSelection(int offset)
        {
            if (_folders.Count > 0)
            {
                _selectedFolder = Math.Clamp(_selectedFolder + Math.Sign(offset), 0, _folders.Count - 1);
                return;
            }
            if (_mobileChoices.Count == 0) return;
            int current = _mobileChoices.IndexOf(_selectedIndex);
            if (current < 0) current = 0;
            current = Math.Clamp(current + Math.Sign(offset), 0, _mobileChoices.Count - 1);
            _selectedIndex = _mobileChoices[current];
        }
#endif

        private void DrawConnections(SpriteBatch spriteBatch)
        {
            var positions = _visibleNodes.ToDictionary(i => _nodes[i].Id,
                i => _camera.WorldToScreen(new Vector2(_nodes[i].MapX, _nodes[i].MapY)));
            foreach (int index in _visibleNodes)
            {
                CampaignNode node = _nodes[index];
                if (!positions.TryGetValue(node.Id, out Vector2 nodeEnd)) continue;
                foreach (string parentId in node.Parents)
                {
                    if (!positions.TryGetValue(parentId, out Vector2 start)) continue;
                    Vector2 end = nodeEnd;
                    if (!ClipLine(_camera.Viewport, ref start, ref end)) continue;
                    bool active = _progress.IsCompleted(parentId) || _progress.IsCompleted(node.Id);
                    DrawLine(spriteBatch, start, end,
                        active ? UITheme.Gold : UITheme.InkMuted * 0.58f,
                        active ? 3 : 2);
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
            Color background = completed
                ? Color.Lerp(UITheme.PanelLight, UITheme.AccentGreen, 0.30f)
                : unlocked ? UITheme.PanelLight : UITheme.PanelDark;
            Color foreground = unlocked ? UITheme.Ink : UITheme.InkMuted;

            spriteBatch.Draw(_pixel, bounds, background);
            Color accent = completed ? UITheme.AccentGreen :
                unlocked ? UITheme.Gold : UITheme.AccentViolet;
            if (selected)
                UIUtils.DrawGlowFrame(spriteBatch, bounds, _pixel, UITheme.GoldBright, 3, 0.72f);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds,
                selected ? UITheme.GoldBright : accent * (unlocked ? 0.82f : 0.52f),
                selected ? 3 : 2, _pixel);

            int signalWidth = Math.Max(18, bounds.Width / 5);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top,
                signalWidth, Math.Max(2, bounds.Height / 18)), accent);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - signalWidth,
                bounds.Bottom - Math.Max(2, bounds.Height / 18), signalWidth,
                Math.Max(2, bounds.Height / 18)), accent * 0.72f);
            DrawNodeStateMark(spriteBatch, bounds, completed, unlocked, accent);

            string label = selected ? $"{node.Id}  {node.Name}" : node.Id;
            DrawCentered(spriteBatch, _font, label,
                new Vector2(bounds.Center.X - (selected ? 0f : 2f), bounds.Center.Y),
                bounds.Width - (selected ? 44f : 70f),
                MathHelper.Clamp((selected ? 0.42f : 0.48f) * _camera.Zoom, 0.30f, 0.64f),
                foreground);
        }

        private void DrawNodeStateMark(SpriteBatch spriteBatch, Rectangle bounds,
            bool completed, bool unlocked, Color accent)
        {
            Vector2 center = new(bounds.Right - Math.Max(13, bounds.Height / 4), bounds.Center.Y);
            float size = Math.Max(5f, bounds.Height / 9f);
            if (completed)
            {
                DrawLine(spriteBatch, new Vector2(center.X - size, center.Y),
                    new Vector2(center.X - size * 0.2f, center.Y + size * 0.8f),
                    UITheme.Ink, 2);
                DrawLine(spriteBatch, new Vector2(center.X - size * 0.2f, center.Y + size * 0.8f),
                    new Vector2(center.X + size, center.Y - size), UITheme.Ink, 2);
            }
            else if (unlocked)
            {
                int dot = Math.Max(5, bounds.Height / 8);
                spriteBatch.Draw(_pixel, new Rectangle((int)center.X - dot / 2,
                    (int)center.Y - dot / 2, dot, dot), UITheme.GoldBright);
            }
            else
            {
                DrawLine(spriteBatch, new Vector2(center.X, center.Y - size),
                    new Vector2(center.X + size, center.Y), accent, 2);
                DrawLine(spriteBatch, new Vector2(center.X + size, center.Y),
                    new Vector2(center.X, center.Y + size), accent, 2);
                DrawLine(spriteBatch, new Vector2(center.X, center.Y + size),
                    new Vector2(center.X - size, center.Y), accent, 2);
                DrawLine(spriteBatch, new Vector2(center.X - size, center.Y),
                    new Vector2(center.X, center.Y - size), accent, 2);
            }
        }

        private void DrawFooter(SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDevice.Viewport;
            Rectangle minus = MinusButton(viewport);
            Rectangle fit = FitButton(viewport);
            Rectangle plus = PlusButton(viewport);
            Rectangle back = BackButton(viewport);
            if (_visibleNodes.Count > 0)
            {
                DrawSmallButton(spriteBatch, minus, "-");
                DrawFitIcon(spriteBatch, fit);
                DrawSmallButton(spriteBatch, plus, "+");
            }
            DrawBackIcon(spriteBatch, back);

            if (_folders.Count > 0 || _scope.Count > 0 || _selectedIndex >= 0)
            {
                string detail = _folders.Count > 0
                    ? _folders[Math.Clamp(_selectedFolder, 0, _folders.Count - 1)]
                    : _selectedIndex >= 0 && _visibleNodes.Contains(_selectedIndex)
                        ? $"{_nodes[_selectedIndex].Id}  {_nodes[_selectedIndex].Name}"
                        : _scope.LastOrDefault() ?? "Campaign";
#if FIDA_ANDROID
                DrawCentered(spriteBatch, _font, detail,
                    new Vector2(viewport.Width / 2f, viewport.Height - 122f),
                    viewport.Width - 56f, 0.46f, UITheme.PanelLight);
#else
                float centerX = (_visibleNodes.Count > 0 ? plus.Right + back.Left : viewport.Width / 2f * 2f) / 2f;
                float width = _visibleNodes.Count > 0 ? Math.Max(120f, back.Left - plus.Right - 35f) : viewport.Width - 180f;
                DrawCentered(spriteBatch, _font, detail,
                    new Vector2(centerX, back.Center.Y), width, 0.40f, UITheme.Ink);
#endif
            }
        }

#if !FIDA_ANDROID
        private static Rectangle DesktopModalBounds(Viewport viewport)
        {
            int width = Math.Min(760, viewport.Width - 100);
            int height = Math.Min(610, viewport.Height - 100);
            return new Rectangle((viewport.Width - width) / 2, (viewport.Height - height) / 2,
                width, height);
        }

        private static Rectangle DesktopModalClose(Rectangle modal) =>
            new(modal.Right - 64, modal.Top + 16, 44, 44);

        private static Rectangle DesktopModalEnter(Rectangle modal) =>
            new(modal.Left + 44, modal.Bottom - 104, modal.Width - 88, 64);

        private void DrawDesktopMissionModal(SpriteBatch spriteBatch, CampaignNode node)
        {
            Viewport viewport = _graphicsDevice.Viewport;
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.78f);
            Rectangle modal = DesktopModalBounds(viewport);
            spriteBatch.Draw(_pixel, modal, UITheme.PanelDark);
            UIUtils.DrawGlowFrame(spriteBatch, modal, _pixel, UITheme.Gold, 3, 0.76f);
            DrawCentered(spriteBatch, _titleFont, $"{node.Id}  {node.Name}",
                new Vector2(modal.Center.X, modal.Top + 78), modal.Width - 140, 0.48f, UITheme.Ink);
            DrawCentered(spriteBatch, _font, node.Story,
                new Vector2(modal.Center.X, modal.Top + 160), modal.Width - 100, 0.36f, UITheme.InkMuted);
            DrawCentered(spriteBatch, _font, $"{node.SkillBudget} SP",
                new Vector2(modal.Center.X, modal.Top + 262), modal.Width - 100, 0.60f, UITheme.GoldBright);
            DrawCentered(spriteBatch, _font, node.RewardText,
                new Vector2(modal.Center.X, modal.Top + 327), modal.Width - 100, 0.38f, UITheme.AccentGreen);
            DrawCentered(spriteBatch, _font, $"{node.Enemies.Count}  ENEMIES",
                new Vector2(modal.Center.X, modal.Top + 396), modal.Width - 100, 0.36f, UITheme.Ink);
            Rectangle enter = DesktopModalEnter(modal);
            bool unlocked = CampaignCatalog.IsUnlocked(_progress, node);
            spriteBatch.Draw(_pixel, enter, unlocked ? UITheme.PanelLight : UITheme.Panel);
            UIUtils.DrawRectangleOutlines(spriteBatch, enter,
                unlocked ? UITheme.AccentGreen : UITheme.InkMuted, 2, _pixel);
            DrawCentered(spriteBatch, _font, unlocked ? "SELECT SKILLS" : "LOCKED",
                enter.Center.ToVector2(), enter.Width - 30, 0.42f,
                unlocked ? UITheme.Ink : UITheme.InkMuted);
            Rectangle close = DesktopModalClose(modal);
            spriteBatch.Draw(_pixel, close, UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, close, UITheme.AccentBlue, 1, _pixel);
            DrawLine(spriteBatch, new Vector2(close.Left + 12, close.Top + 12),
                new Vector2(close.Right - 12, close.Bottom - 12), UITheme.Ink, 2);
            DrawLine(spriteBatch, new Vector2(close.Right - 12, close.Top + 12),
                new Vector2(close.Left + 12, close.Bottom - 12), UITheme.Ink, 2);
        }

        private void DrawBackIcon(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.Draw(_pixel, bounds, UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds, UITheme.Gold, 2, _pixel);
            Vector2 tip = new(bounds.Left + 13, bounds.Center.Y);
            DrawLine(spriteBatch, tip, new Vector2(bounds.Right - 11, bounds.Center.Y), UITheme.Ink, 2);
            DrawLine(spriteBatch, tip, new Vector2(bounds.Left + 25, bounds.Top + 12), UITheme.Ink, 2);
            DrawLine(spriteBatch, tip, new Vector2(bounds.Left + 25, bounds.Bottom - 12), UITheme.Ink, 2);
        }
#endif

        private void DrawFitIcon(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.Draw(_pixel, bounds, UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds, UITheme.Gold, 2, _pixel);
            float inset = Math.Min(bounds.Width, bounds.Height) * 0.28f;
            float arm = Math.Min(bounds.Width, bounds.Height) * 0.18f;
            Color color = UITheme.Ink;
            DrawLine(spriteBatch, new Vector2(bounds.Left + inset, bounds.Top + inset + arm),
                new Vector2(bounds.Left + inset, bounds.Top + inset), color, 2);
            DrawLine(spriteBatch, new Vector2(bounds.Left + inset, bounds.Top + inset),
                new Vector2(bounds.Left + inset + arm, bounds.Top + inset), color, 2);
            DrawLine(spriteBatch, new Vector2(bounds.Right - inset - arm, bounds.Top + inset),
                new Vector2(bounds.Right - inset, bounds.Top + inset), color, 2);
            DrawLine(spriteBatch, new Vector2(bounds.Right - inset, bounds.Top + inset),
                new Vector2(bounds.Right - inset, bounds.Top + inset + arm), color, 2);
            DrawLine(spriteBatch, new Vector2(bounds.Left + inset, bounds.Bottom - inset - arm),
                new Vector2(bounds.Left + inset, bounds.Bottom - inset), color, 2);
            DrawLine(spriteBatch, new Vector2(bounds.Left + inset, bounds.Bottom - inset),
                new Vector2(bounds.Left + inset + arm, bounds.Bottom - inset), color, 2);
            DrawLine(spriteBatch, new Vector2(bounds.Right - inset - arm, bounds.Bottom - inset),
                new Vector2(bounds.Right - inset, bounds.Bottom - inset), color, 2);
            DrawLine(spriteBatch, new Vector2(bounds.Right - inset, bounds.Bottom - inset),
                new Vector2(bounds.Right - inset, bounds.Bottom - inset - arm), color, 2);
        }

        private void DrawSmallButton(SpriteBatch spriteBatch, Rectangle bounds, string text)
        {
            spriteBatch.Draw(_pixel, bounds, UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds, UITheme.Gold, 2, _pixel);
            DrawCentered(spriteBatch, _font, text, bounds.Center.ToVector2(), bounds.Width - 12f, 0.44f, UITheme.Ink);
        }

        private void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 center,
            float maximumWidth, float preferredScale, Color color)
        {
            text = UIFontGuard.Sanitize(font, text);
            Vector2 measured = font.MeasureString(text);
            if (measured.X <= 0f) return;
            float scale = Math.Min(preferredScale, maximumWidth / measured.X);
#if FIDA_ANDROID
            // Mobile copy is deliberately shortened or wrapped; never trade
            // legibility away just to squeeze an exceptional string onto one line.
            scale = Math.Max(0.30f, scale);
#else
            scale = Math.Max(0.16f, scale);
#endif
            spriteBatch.DrawString(font, text, center - measured * scale / 2f, color, 0f,
                Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void MoveSelection(Vector2 direction)
        {
            if (_folders.Count > 0)
            {
                int columns = _folders.Count <= 2 ? 1 : 2;
                int delta = Math.Abs(direction.X) > Math.Abs(direction.Y)
                    ? Math.Sign(direction.X) : Math.Sign(direction.Y) * columns;
                _selectedFolder = Math.Clamp(_selectedFolder + delta, 0, _folders.Count - 1);
                return;
            }
            var visible = _visibleNodes.Select(i => _nodes[i]).ToArray();
            int current = _visibleNodes.ToList().IndexOf(_selectedIndex);
            int localNext = CampaignMapNavigator.FindNext(visible, current, direction);
            int next = localNext >= 0 && localNext < _visibleNodes.Count ? _visibleNodes[localNext] : -1;
            if (next < 0 || next == _selectedIndex) return;
            _selectedIndex = next;
            CampaignNode node = _nodes[_selectedIndex];
            _camera.EnsureVisible(new Vector2(node.MapX, node.MapY));
        }

        private void ConfirmSelection()
        {
            if (_folders.Count > 0)
            {
                EnterFolder(_selectedFolder);
                return;
            }
            if (_selectedIndex < 0 || _selectedIndex >= _nodes.Count) return;
            CampaignNode node = _nodes[_selectedIndex];
#if FIDA_ANDROID
            if (CampaignCatalog.IsUnlocked(_progress, node)) _pendingNodeId = node.Id;
#else
            if (!_desktopDetailsOpen) _desktopDetailsOpen = true;
            else if (CampaignCatalog.IsUnlocked(_progress, node)) _pendingNodeId = node.Id;
#endif
        }

        private int HitNode(Point screenPoint)
        {
            if (!_camera.Viewport.Contains(screenPoint)) return -1;
            for (int j = _visibleNodes.Count - 1; j >= 0; j--)
            {
                int i = _visibleNodes[j];
                if (NodeScreenBounds(_nodes[i]).Contains(screenPoint)) return i;
            }
            return -1;
        }

        private string HitTarget(Point point)
        {
            var viewport = _graphicsDevice.Viewport;
#if !FIDA_ANDROID
            if (_desktopDetailsOpen)
            {
                Rectangle modal = DesktopModalBounds(viewport);
                if (DesktopModalEnter(modal).Contains(point)) return "modal_enter";
                if (DesktopModalClose(modal).Contains(point)) return "modal_close";
                return "";
            }
#endif
            if (_visibleNodes.Count > 0 && MinusButton(viewport).Contains(point)) return "zoom_out";
            if (_visibleNodes.Count > 0 && FitButton(viewport).Contains(point)) return "fit";
            if (_visibleNodes.Count > 0 && PlusButton(viewport).Contains(point)) return "zoom_in";
            if (BackButton(viewport).Contains(point)) return "back";
            int folder = HitFolder(point);
            if (folder >= 0) return "folder:" + folder;
            int node = HitNode(point);
            if (node >= 0) return "node:" + node;
            return _camera.Viewport.Contains(point) ? "map" : "";
        }

        private void ActivateTarget(string target)
        {
            if (target == "modal_close") _desktopDetailsOpen = false;
            else if (target == "modal_enter") ConfirmSelection();
            else if (target == "zoom_out") ZoomOut();
            else if (target == "fit") FitAll();
            else if (target == "zoom_in") ZoomIn();
            else if (target == "back") GoBack();
            else if (target.StartsWith("folder:", StringComparison.Ordinal) &&
                     int.TryParse(target.AsSpan(7), out int folderIndex)) EnterFolder(folderIndex);
            else if (target.StartsWith("node:", StringComparison.Ordinal) &&
                     int.TryParse(target.AsSpan(5), out int nodeIndex))
            {
                _selectedIndex = nodeIndex;
                ConfirmSelection();
            }
        }

        private Rectangle CalculateContentBounds()
        {
            if (_visibleNodes.Count == 0) return new Rectangle(0, 0, 1, 1);
            float halfWidth = _options.NodeWidth / 2f;
            float halfHeight = _options.NodeHeight / 2f;
            int left = (int)Math.Floor(_visibleNodes.Min(i => _nodes[i].MapX - halfWidth));
            int top = (int)Math.Floor(_visibleNodes.Min(i => _nodes[i].MapY - halfHeight));
            int right = (int)Math.Ceiling(_visibleNodes.Max(i => _nodes[i].MapX + halfWidth));
            int bottom = (int)Math.Ceiling(_visibleNodes.Max(i => _nodes[i].MapY + halfHeight));
            return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }

        private void RefreshScope()
        {
            _folders = _hierarchy.Children(_scope);
            _visibleNodes = _hierarchy.Missions(_scope);
            _selectedFolder = Math.Clamp(_selectedFolder, 0, Math.Max(0, _folders.Count - 1));
            if (_visibleNodes.Count > 0 && !_visibleNodes.Contains(_selectedIndex))
                _selectedIndex = _visibleNodes.FirstOrDefault(i =>
                    CampaignCatalog.IsUnlocked(_progress, _nodes[i]) && !_progress.IsCompleted(_nodes[i].Id),
                    _visibleNodes[0]);
            _camera = new CampaignMapCamera(CalculateContentBounds(), _options.ContentPadding,
                _options.MinimumZoom, _options.MaximumZoom);
            _camera.SetViewport(_options.GetMapViewport(_graphicsDevice.Viewport));
#if FIDA_ANDROID
            RebuildMobileChoices();
            LayoutMobile();
#endif
        }

        private void EnterFolder(int index)
        {
            if (index < 0 || index >= _folders.Count) return;
            _scope.Add(_folders[index]);
            _selectedFolder = 0;
            RefreshScope();
        }

        private void GoBack()
        {
            if (_scope.Count == 0) { _backRequested = true; return; }
            string previous = _scope[^1];
            _scope.RemoveAt(_scope.Count - 1);
            RefreshScope();
            int restored = _folders.ToList().IndexOf(previous);
            if (restored >= 0) _selectedFolder = restored;
        }

        private Rectangle FolderBounds(int index)
        {
            Rectangle viewport = _camera.Viewport;
            int columns = _folders.Count <= 2 ? 1 : 2;
            int rows = (_folders.Count + columns - 1) / columns;
            int gap = Math.Clamp(viewport.Width / 40, 16, 36);
            int width = Math.Min(460, (viewport.Width - gap * (columns + 1)) / columns);
            int height = Math.Clamp((viewport.Height - gap * (rows + 1)) / Math.Max(1, rows), 94, 156);
            int totalWidth = columns * width + (columns - 1) * gap;
            int totalHeight = rows * height + (rows - 1) * gap;
            int left = viewport.Center.X - totalWidth / 2;
            int top = viewport.Center.Y - totalHeight / 2;
            return new Rectangle(left + index % columns * (width + gap),
                top + index / columns * (height + gap), width, height);
        }

        private int HitFolder(Point point)
        {
            for (int i = 0; i < _folders.Count; i++)
                if (FolderBounds(i).Contains(point)) return i;
            return -1;
        }

        private void DrawFolders(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < _folders.Count; i++)
            {
                Rectangle bounds = FolderBounds(i);
                var path = _scope.Concat(new[] { _folders[i] }).ToArray();
                IReadOnlyList<int> descendants = _hierarchy.Descendants(path);
                int cleared = descendants.Count(index => _progress.IsCompleted(_nodes[index].Id));
                bool selected = i == _selectedFolder;
                Color accent = UITheme.RouteAccent(i);
                spriteBatch.Draw(_pixel, bounds, selected ? UITheme.PanelPressed : UITheme.PanelLight);
                UIUtils.DrawRectangleOutlines(spriteBatch, bounds,
                    selected ? UITheme.GoldBright : accent, selected ? 3 : 2, _pixel);
                spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top, 5, bounds.Height), accent);
                DrawCentered(spriteBatch, _titleFont, _folders[i],
                    new Vector2(bounds.Center.X, bounds.Top + bounds.Height * 0.40f),
                    bounds.Width - 56, 0.54f, UITheme.Ink);
                DrawCentered(spriteBatch, _font, $"{cleared} / {descendants.Count}",
                    new Vector2(bounds.Center.X, bounds.Top + bounds.Height * 0.73f),
                    bounds.Width - 56, 0.38f, UITheme.GoldBright);
            }
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
            return new Rectangle(viewport.Width - 90, viewport.Height - 76, 54, 50);
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
