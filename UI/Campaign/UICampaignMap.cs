#nullable enable
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
    public sealed partial class UICampaignMap : UIControl
    {
        private double _routeAge;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Texture2D _pixel;
        private readonly Texture2D _antialiasedLine;
        private readonly SpriteFont _font;
        private readonly SpriteFont _titleFont;
        private readonly IReadOnlyList<CampaignNode> _nodes;
        private readonly CampaignProgress _progress;
        private readonly CampaignMapOptions _options;
        private readonly UICampaignRouteView _routeView;
        private float _fitZoom = 1;
        private CampaignMapCamera _camera;
        private readonly CampaignMapHierarchy _hierarchy;
        private readonly List<string> _scope = new();
        private IReadOnlyList<string> _folders = Array.Empty<string>();
        private IReadOnlyList<int> _visibleNodes = Array.Empty<int>();
        private readonly Dictionary<string, Vector2> _routePositions = new();
        private int _selectedFolder;
        /// <summary>
        /// Set by the Android list layout so chapter cards can hold an 18dp
        /// equivalent title instead of inheriting the card-height ratio. Stays
        /// zero on desktop, where each card sizes its own text from its height.
        /// </summary>
        private float _mobileTextScale = 0f;

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
        private readonly Dictionary<Rectangle, (float Hover, float Press, bool Enabled)> _buttonMotion = new();

        private (float Hover, float Press) ButtonMotion(Rectangle bounds, bool enabled = true)
        {
            if (_buttonMotion.Count > 128) _buttonMotion.Clear();
            _buttonMotion.TryGetValue(bounds, out var state);
            _buttonMotion[bounds] = (state.Hover, state.Press, enabled);
            return enabled ? (state.Hover, state.Press) : (0, 0);
        }

        private void DrawMapButton(SpriteBatch batch, Rectangle bounds, Color color, bool enabled = true)
        {
            var motion = ButtonMotion(bounds, enabled);
            UIPaper.Button(batch, bounds, color, motion.Hover, motion.Press, enabled, shape: UIPaperShape.Tag);
        }

        private void DrawMapBack(SpriteBatch batch, Rectangle bounds)
        {
            var motion = ButtonMotion(bounds);
            UIIconButton.DrawBackButton(batch, _pixel, bounds, hoverAmount: motion.Hover, pressAmount: motion.Press);
        }
#if FIDA_ANDROID
        private readonly List<int> _mobileChoices = new();
        /// <summary>Rows printed by the phone list; a cleared mission keeps its row so it can be replayed.</summary>
        private IReadOnlyList<CampaignMissionEntry> _mobileEntries = Array.Empty<CampaignMissionEntry>();
        private readonly List<Rectangle> _mobileChoiceBounds = new();
        private Rectangle _mobileBackBounds;
        private CampaignMissionLayout? _mobileMissionLayout;
        private bool _mobileDetailsOpen;
        private float _mobileScroll;
        private float _mobileScrollMax;
        private bool _mobileDragging;
        private Rectangle _mobileListArea;
#endif

        public float Zoom => _camera.Zoom;
        public string SelectedNodeId => _selectedIndex >= 0 && _selectedIndex < _nodes.Count
            ? _nodes[_selectedIndex].Id
            : "";
        public string RemoteNodeId { get; set; } = "";
        public string RemoteStatusLabel { get; set; } = "";

#if FIDA_ANDROID
        /// <summary>Rows printed by the phone list, in screen order; previews tap these.</summary>
        public IReadOnlyList<Rectangle> MobileRowBounds => _mobileChoiceBounds;
        /// <summary>Mission rows with their printed state, in screen order.</summary>
        public IReadOnlyList<CampaignMissionEntry> MobileRows => _mobileEntries;
        public bool MobileDetailsOpen => _mobileDetailsOpen;
        /// <summary>The mission sheet geometry, shared by drawing and hit testing.</summary>
        public CampaignMissionLayout MissionSheet => _mobileMissionLayout ??=
            new CampaignMissionLayout(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
#endif

        public UICampaignMap(int id, GraphicsDevice graphicsDevice, Texture2D pixel,
            Texture2D antialiasedLine, SpriteFont font, SpriteFont titleFont,
            IReadOnlyList<CampaignNode> nodes, CampaignProgress progress,
            CampaignMapOptions? options = null) : base(id)
        {
            _graphicsDevice = graphicsDevice;
            _pixel = pixel;
            _antialiasedLine = antialiasedLine ?? pixel;
            _font = font;
            _titleFont = titleFont;
            _routeView = new UICampaignRouteView(font, titleFont, pixel, _antialiasedLine);
            _nodes = nodes ?? Array.Empty<CampaignNode>();
            _hierarchy = new CampaignMapHierarchy(_nodes);
            _progress = progress;
            _options = options ?? new CampaignMapOptions();
            _camera = new CampaignMapCamera(CalculateContentBounds(), _options.ContentPadding,
                _options.MinimumZoom, _options.MaximumZoom);
            _camera.SetViewport(_options.GetMapViewport(_graphicsDevice.Viewport));
            _fitZoom = _camera.Zoom;
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
        public void FitAll() { _camera.FitAll(); _fitZoom = _camera.Zoom; }

        public void SynchronizeInput()
        {
            _buttonMotion.Clear();
            _previousMouse = UIPointer.GetState();
            _previousKeyboard = Keyboard.GetState();
            _previousGamePad = GamePad.GetState(PlayerIndex.One);
            _inputInitialized = true; _pressedTarget = ""; _dragging = false;
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var bounds in _buttonMotion.Keys.ToArray())
            {
                var state = _buttonMotion[bounds];
                bool hover = state.Enabled && bounds.Contains(_previousMouse.Position);
                bool press = hover && !_dragging && _pressedTarget.Length > 0 &&
                    _previousMouse.LeftButton == ButtonState.Pressed && bounds.Contains(_pressPosition);
                _buttonMotion[bounds] = (UIMotion.Approach(state.Hover, hover ? 1 : 0, gameTime.ElapsedGameTime.TotalSeconds),
                    UIMotion.Approach(state.Press, press ? 1 : 0, gameTime.ElapsedGameTime.TotalSeconds, 24), state.Enabled);
            }
            _routeAge += gameTime.ElapsedGameTime.TotalSeconds;
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
                GoBack();
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
                    GoBack();
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
            if (_camera.Viewport.Contains(position))
            {
                int wheelDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
                if (wheelDelta != 0)
                    _camera.ZoomAt((float)Math.Pow(1.15, wheelDelta / 120f), position.ToVector2());

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
            _routeView.DrawHeader(spriteBatch, new CampaignMapLayout(_graphicsDevice.Viewport),
                _scope.Count > 0 ? _scope[^1] : null, ChapterStyle.ForPath(_scope));
            if (_folders.Count > 0) DrawFolders(spriteBatch);
            if (_visibleNodes.Count > 0)
            {
                // The graph is cut at the map rectangle instead of being dropped as soon as
                // a card touches it, so panned and zoomed cards stay on screen.
                using (UIBatchClip.Enter(spriteBatch, viewport))
                {
                    DrawConnections(spriteBatch);
                    foreach (int i in _visibleNodes) DrawNode(spriteBatch, i);
                }
            }
            DrawRemotePresence(spriteBatch, viewport);
            DrawEditorialDetails(spriteBatch);
            DrawFooter(spriteBatch);
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

        /// <summary>
        /// Portrait cards are sized from the viewport instead of being squeezed to
        /// fit one screen: a title of about 18dp keeps the chapter list readable,
        /// which no longer fits alongside every card, so the list scrolls.
        /// </summary>
        private void LayoutMobile()
        {
            Rectangle panel = MobilePanel();
            _mobileChoiceBounds.Clear();

            // Portrait cards size their type from the viewport instead of from the
            // card height, so a short card cannot push titles below a readable
            // size. A phone viewport is about 412dp wide, and the title face draws
            // roughly 20px of ink at card scale 1, which calibrates the dp target
            // against what actually renders; 16dp matches the main menu's labels.
            const float titleInkAtScaleOne = 20f;
            float pixelsPerDp = _graphicsDevice.Viewport.Width / 412f;
            float titlePixels = 16f * pixelsPerDp;
            _mobileTextScale = Math.Clamp(titlePixels / titleInkAtScaleOne, 0.9f, 2.6f);

            // The chapter cards and the mission cards share one header strip; only the
            // artwork band changes. Missions get a taller strip so their own scene plate
            // reads as artwork instead of a hairline.
            int header = (int)Math.Ceiling(33 * _mobileTextScale) + 14;
            bool chapters = _folders.Count > 0;
            int art = chapters ? Math.Clamp(panel.Width / 9, 84, 150)
                : Math.Clamp(panel.Width / 5, 120, 210);
            int cardHeight = header + art + 12;

            int cardTop = panel.Top + 102;
            int gap = 12;
            int available = Math.Max(1, panel.Bottom - cardTop - 12);
            int count = Math.Max(1, chapters ? _folders.Count : _mobileChoices.Count);
            int cardWidth = Math.Max(1, panel.Width - 40);

            float content = count * cardHeight + Math.Max(0, count - 1) * gap;
            _mobileScrollMax = Math.Max(0f, content - available);
            _mobileScroll = Math.Clamp(_mobileScroll, 0f, _mobileScrollMax);
            _mobileListArea = new Rectangle(panel.Left, cardTop, panel.Width, available);

            for (int i = 0; i < count && (chapters || i < _mobileChoices.Count); i++)
            {
                _mobileChoiceBounds.Add(new Rectangle(panel.Left + 20,
                    cardTop + i * (cardHeight + gap) - (int)_mobileScroll, cardWidth, cardHeight));
            }

            Rectangle safe = UIMobileSafeArea.Get(_graphicsDevice.Viewport);
            _mobileBackBounds = new Rectangle(safe.Left + 8, safe.Top + 4, 64, 64);
            _mobileMissionLayout = new CampaignMissionLayout(_graphicsDevice.Viewport.Width,
                _graphicsDevice.Viewport.Height);
        }

        private void ScrollMobile(float delta)
        {
            _mobileScroll = Math.Clamp(_mobileScroll + delta, 0f, _mobileScrollMax);
        }

        /// <summary>
        /// Every mission on the page keeps a row. Clearing a mission used to remove it from
        /// the list, which hid the only way back into that battle; a locked mission now prints
        /// as grey stock instead of disappearing, so the whole chapter stays legible.
        /// </summary>
        private void RebuildMobileChoices()
        {
            _mobileChoices.Clear();
            _mobileEntries = CampaignMissionList.Build(_nodes, _visibleNodes, _progress);
            foreach (var entry in _mobileEntries) _mobileChoices.Add(entry.Index);
            int focus = CampaignMissionList.InitialFocus(_mobileEntries, _selectedIndex);
            if (focus >= 0) _selectedIndex = focus;
        }

        /// <summary>Cleared and total missions of the page being browsed.</summary>
        private (int Cleared, int Total) MobilePageProgress()
        {
            if (_scope.Count == 0) return (_progress.CompletedNodes.Count, _nodes.Count);
            var descendants = CountedDescendants(_scope, _hierarchy.Descendants(_scope));
            return (descendants.Count(index => _progress.IsCompleted(_nodes[index].Id)), descendants.Count);
        }

        private void DrawMobile(SpriteBatch spriteBatch, GameTime gameTime)
        {
            LayoutMobile();
            Rectangle panel = MobilePanel();
            Viewport viewport = _graphicsDevice.Viewport;
            Rectangle safe = UIMobileSafeArea.Get(viewport);
            DrawCentered(spriteBatch, _titleFont, _scope.LastOrDefault() ?? "Campaign",
                new Vector2(safe.Center.X, safe.Top + 32f), safe.Width - 150f,
                0.60f, UITheme.Ink, UITextRole.Heading);
            DrawBackIcon(spriteBatch, _mobileBackBounds);
            DrawRemotePresence(spriteBatch, panel);

            // The list prints straight on the paper background, like the roster and loadout
            // pages: chapter and mission cards carry their own cut sheets, so a flat panel
            // behind them would read as a separate, non-paper table.
            bool chapters = _folders.Count > 0;
            var progress = MobilePageProgress();
            // The counter always reports missions of the page being browsed; only the root
            // page is the whole road, so it keeps the campaign-wide reading.
            DrawCentered(spriteBatch, _font, _scope.Count == 0 ? "ROUTES" : "MISSIONS",
                new Vector2(panel.Left + 86, panel.Top + 38), 160f,
                0.56f, UITheme.GoldBright, UITextRole.Caption);
            DrawCentered(spriteBatch, _font, $"{progress.Cleared} / {progress.Total}",
                new Vector2(panel.Right - 82, panel.Top + 38), 140f,
                0.56f, UITheme.InkMuted, UITextRole.Caption);
            Rectangle progressTrack = new(panel.Left + 30, panel.Top + 68,
                Math.Max(1, panel.Width - 60), 5);
            spriteBatch.Draw(_pixel, progressTrack, UITheme.PanelDark);
            float completion = progress.Total == 0 ? 0f :
                MathHelper.Clamp(progress.Cleared / (float)progress.Total, 0f, 1f);
            spriteBatch.Draw(_pixel, new Rectangle(progressTrack.X, progressTrack.Y,
                (int)(progressTrack.Width * completion), progressTrack.Height),
                UITheme.AccentGreen);
            spriteBatch.Draw(_pixel, new Rectangle(panel.Left + 30, panel.Top + 86,
                Math.Max(1, panel.Width - 60), 1), UITheme.Line * 0.45f);

            // The list can be taller than the panel once cards keep a readable
            // title, so clip it to its own area instead of painting over the
            // header and the mission sheet.
            using (UIBatchClip.Enter(spriteBatch, _mobileListArea))
            {
                for (int i = 0; i < _mobileChoiceBounds.Count; i++)
                {
                    if (chapters)
                    {
                        DrawMobileFolder(spriteBatch, i);
                        continue;
                    }
                    if (i >= _mobileEntries.Count) continue;
                    Rectangle bounds = _mobileChoiceBounds[i];
                    if (!bounds.Intersects(_mobileListArea)) continue;
                    CampaignMissionEntry entry = _mobileEntries[i];
                    CampaignNode node = _nodes[entry.Index];
                    var motion = ButtonMotion(bounds);
                    _routeView.DrawMissionCard(spriteBatch, bounds, node.Id, node.Name, node.Chapter,
                        entry.State, entry.Index == _selectedIndex, motion.Hover, motion.Press,
                        ChapterStyle.For(node), _mobileTextScale);
                }
            }

            CampaignNode? selectedNode = _selectedIndex >= 0 && _selectedIndex < _nodes.Count
                ? _nodes[_selectedIndex] : null;
            if (_mobileDetailsOpen && selectedNode != null)
                DrawMissionModal(spriteBatch, viewport, selectedNode);
        }

        private void DrawMobileFolder(SpriteBatch spriteBatch, int index)
        {
            Rectangle bounds = _mobileChoiceBounds[index];
            DrawChapterCard(spriteBatch, bounds, index, index == _selectedFolder);
        }

        private void DrawBackIcon(SpriteBatch spriteBatch, Rectangle bounds)
        {
            DrawMapBack(spriteBatch, bounds);
        }

        /// <summary>
        /// The phone mission sheet replaces the old flat modal: the mission's own artwork,
        /// vow stamp, story, cost and reward print on the same cut-paper stack as the
        /// chapter detail pane, and the ticket offers the replay that the list kept a row for.
        /// </summary>
        private void DrawMissionModal(SpriteBatch spriteBatch, Viewport viewport,
            CampaignNode node)
        {
            CampaignMissionLayout layout = MissionSheet;
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * 0.72f);
            CampaignMissionState state = CampaignMissionList.StateOf(_progress, node);
            var details = new CampaignRouteDetails(node.Id, node.Name, node.Story, node.Enemies.Count,
                node.SkillBudget, node.RewardText, node.Rewards.Count > 0 ? node.Rewards[0] : null,
                node.Chapter, state == CampaignMissionState.Locked, node.Tier ?? 0, node.Vows,
                CampaignChapters.IsSide(node), state == CampaignMissionState.Cleared);
            var actionMotion = ButtonMotion(layout.Action, state != CampaignMissionState.Locked);
            _routeView.DrawMissionSheet(spriteBatch, layout, details,
                actionMotion.Hover, actionMotion.Press, ChapterStyle.For(node));
            var closeMotion = ButtonMotion(layout.Close);
            UIIconButton.DrawCloseButton(spriteBatch, _pixel, layout.Close,
                closeMotion.Hover, closeMotion.Press);
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
            {
                _pressPosition = position;
                _pressedTarget = HitMobileTarget(position);
                _mobileDragging = false;
            }
            else if (mouse.LeftButton == ButtonState.Pressed && _pressedTarget.Length > 0)
            {
                // A drag scrubs the list; it must not also count as a tap when the
                // finger lifts, so the press target is dropped once travel passes
                // the slop distance. The sheet is modal, so it never scrubs the list
                // that sits behind it.
                if (!_mobileDetailsOpen && !_mobileDragging &&
                    Math.Abs(position.Y - _pressPosition.Y) > 12)
                {
                    _mobileDragging = true;
                    _pressedTarget = "";
                }
                if (_mobileDragging)
                    ScrollMobile(_previousMouse.Y - position.Y);
            }

            int wheel = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
            if (wheel != 0 && _mobileScrollMax > 0f)
                ScrollMobile(-wheel / 4f);

            bool released = mouse.LeftButton == ButtonState.Released &&
                _previousMouse.LeftButton == ButtonState.Pressed;
            if (released)
            {
                if (!_mobileDragging)
                {
                    string target = HitMobileTarget(position);
                    if (target == _pressedTarget) ActivateMobileTarget(target);
                }
                _pressedTarget = "";
                _mobileDragging = false;
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
                CampaignMissionLayout layout = MissionSheet;
                Rectangle closeTouch = layout.Close;
                closeTouch.Inflate(10, 10);
                if (closeTouch.Contains(point)) return "modal_close";
                if (layout.Action.Contains(point)) return "modal_enter";
                return layout.Sheet.Contains(point) ? "" : "modal_close";
            }

            Rectangle backTouch = _mobileBackBounds;
            backTouch.Inflate(10, 10);
            if (backTouch.Contains(point)) return "back";
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
                int.TryParse(target.AsSpan(5), out int choice))
            {
                if (_folders.Count > 0)
                {
                    if (choice >= 0 && choice < _folders.Count) EnterFolder(choice);
                }
                else if (choice >= 0 && choice < _mobileEntries.Count)
                {
                    _selectedIndex = _mobileEntries[choice].Index;
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
            var visible = _visibleNodes.Select(i => _nodes[i]).ToArray();
            IEnumerable<(string From, string To)> edges = CampaignRouteDiagram.IsReference(visible)
                ? CampaignRouteDiagram.ReferenceEdges
                : visible.SelectMany(node => node.Parents.Select(parent => (parent, node.Id)));
            var bounds = visible.ToDictionary(node => node.Id, NodeScreenBounds);
            float scale = new CampaignMapLayout(_graphicsDevice.Viewport).Scale;
            var links = edges.ToArray();
            foreach (var edge in links)
                if (bounds.TryGetValue(edge.From, out var from) && bounds.TryGetValue(edge.To, out var to))
                    _routeView.DrawConnection(spriteBatch, from, to, _camera.Viewport, scale,
                        links.Count(link => link.From == edge.From) > 1, links.Count(link => link.To == edge.To) > 1);
        }

        private void DrawNode(SpriteBatch spriteBatch, int index)
        {
            CampaignNode node = _nodes[index];
            Rectangle bounds = NodeScreenBounds(node);
            // Clipping trims a card that only partly overlaps the map; skip only the cards
            // with no visible part at all. The slack covers the tab and shadow, which are
            // printed outside the card's own rectangle.
            Rectangle reach = bounds;
            int slack = Math.Max(2, bounds.Height / 10);
            reach.Inflate(slack, slack);
            if (!_camera.Viewport.Intersects(reach)) return;
            _routeView.DrawNode(spriteBatch, bounds, node.Id, node.Name, node.Chapter,
                index == _selectedIndex, _progress.IsCompleted(node.Id),
                !CampaignCatalog.IsUnlocked(_progress, node),
                ChapterStyle.For(node), node.Tier ?? 0);
        }

        /// <summary>
        /// Keep the remote player's presence anchored to the screen instead of attaching
        /// it to a campaign node (which moves as the map is panned or zoomed).
        /// </summary>
        private void DrawRemotePresence(SpriteBatch spriteBatch, Rectangle anchorArea)
        {
            if (string.IsNullOrWhiteSpace(RemoteStatusLabel)) return;

            bool mobile =
#if FIDA_ANDROID
                true;
#else
                false;
#endif
            int width = Math.Clamp(anchorArea.Width / (mobile ? 2 : 4), mobile ? 196 : 260,
                mobile ? 300 : 380);
            width = Math.Min(width, Math.Max(1, anchorArea.Width - 24));
            int height = mobile ? 54 : 50;
            int inset = mobile ? 12 : 18;
            var badge = new Rectangle(anchorArea.Right - width - inset,
                anchorArea.Top + (mobile ? 12 : 18), width, height);

            spriteBatch.Draw(_pixel, badge, UITheme.PanelDark * 0.96f);
            UIUtils.DrawRectangleOutlines(spriteBatch, badge, UITheme.AccentGreen, 1, _pixel);
            spriteBatch.Draw(_pixel, new Rectangle(badge.Left, badge.Top, 4, badge.Height),
                UITheme.AccentGreen);

            string node = string.IsNullOrWhiteSpace(RemoteNodeId) ? "—" : RemoteNodeId;
            string label = $"ALLY  ·  {RemoteStatusLabel.Replace("ALLY · ", "", StringComparison.OrdinalIgnoreCase)}  ·  {node}";
            DrawCentered(spriteBatch, _font, label, badge.Center.ToVector2(), badge.Width - 18,
                mobile ? 0.42f : 0.37f, UITheme.AccentGreen, UITextRole.Small);
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
#if FIDA_ANDROID
                DrawSmallButton(spriteBatch, minus, "-");
                DrawFitIcon(spriteBatch, fit);
                DrawSmallButton(spriteBatch, plus, "+");
#else
                var minusMotion = ButtonMotion(minus);
                var plusMotion = ButtonMotion(plus);
                _routeView.DrawZoom(spriteBatch, new CampaignMapLayout(viewport),
                    (int)Math.Round(_camera.Zoom / Math.Max(.01f, _fitZoom) * 100),
                    minusMotion.Hover, minusMotion.Press, plusMotion.Hover, plusMotion.Press);
#endif
            }
            DrawBackIcon(spriteBatch, back);

#if FIDA_ANDROID
            if (_folders.Count > 0 || _scope.Count > 0 || _selectedIndex >= 0)
            {
                string detail = _folders.Count > 0
                    ? _folders[Math.Clamp(_selectedFolder, 0, _folders.Count - 1)]
                    : _selectedIndex >= 0 && _visibleNodes.Contains(_selectedIndex)
                        ? $"{_nodes[_selectedIndex].Id}  {_nodes[_selectedIndex].Name}"
                        : _scope.LastOrDefault() ?? "Campaign";
                DrawCentered(spriteBatch, _font, detail,
                    new Vector2(viewport.Width / 2f,
#if FIDA_ANDROID
                        viewport.Height - 122f
#else
                        viewport.Height - ScaleDesktop(46, DesktopControlScale(viewport))
#endif
                    ),
#if FIDA_ANDROID
                    viewport.Width - 56f, 0.46f, UITheme.PanelLight, UITextRole.Caption);
#else
                    viewport.Width - plus.Right - 80f, 0.40f, UITheme.Ink);
#endif
            }
#endif
        }

#if !FIDA_ANDROID
        private void DrawLeft(SpriteBatch spriteBatch, SpriteFont font, string value,
            Vector2 leftCenter, float maximumWidth, float preferredScale, Color color)
        {
            string safe = UIFontGuard.Sanitize(font, value);
            Vector2 measured = font.MeasureString(safe);
            if (measured.X <= 0f) return;
            float scale = Math.Min(preferredScale, maximumWidth / measured.X);
            UITextRenderer.DrawString(spriteBatch, font, safe,
                new Vector2(leftCenter.X, leftCenter.Y - measured.Y * scale / 2f),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawBackIcon(SpriteBatch spriteBatch, Rectangle bounds)
        {
            DrawMapBack(spriteBatch, bounds);
        }
#endif

        private void DrawFitIcon(SpriteBatch spriteBatch, Rectangle bounds)
        {
            DrawMapButton(spriteBatch, bounds, UITheme.PanelLight);
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
            DrawMapButton(spriteBatch, bounds, UITheme.PanelLight);
            DrawCentered(spriteBatch, _font, text, bounds.Center.ToVector2(), bounds.Width - 12f, 0.44f, UITheme.Ink, UITextRole.Caption);
        }

        private void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 center,
            float maximumWidth, float preferredScale, Color color, UITextRole role = UITextRole.Body)
        {
#if FIDA_ANDROID
            // The drawn size comes from the dp ramp; the authored sprite scale only
            // describes the desktop layout these values were tuned against.
            var viewport = _graphicsDevice.Viewport;
            float scale = UITypography.LineHeight(role, viewport.Width, viewport.Height)
                / Math.Max(1, font.LineSpacing);
            // Copy is deliberately shortened or wrapped; never trade legibility
            // away just to squeeze an exceptional string onto one line.
            float floor = scale;
#else
            float scale = preferredScale;
            const float floor = 0.16f;
#endif
            UIUtils.DrawCenteredFit(spriteBatch, font, text, center, maximumWidth,
                scale, color, floor);
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
            int localNext = CampaignMapNavigator.FindNext(visible, current, direction, RoutePosition);
            int next = localNext >= 0 && localNext < _visibleNodes.Count ? _visibleNodes[localNext] : -1;
            if (next < 0 || next == _selectedIndex) return;
            _selectedIndex = next;
            CampaignNode node = _nodes[_selectedIndex];
            _camera.EnsureVisible(RoutePosition(node));
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
            // A cleared node is replayable even if this lane never recorded its parents.
            if (_progress.IsCompleted(node.Id) || CampaignCatalog.IsUnlocked(_progress, node))
                _pendingNodeId = node.Id;
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
            if (new CampaignMapLayout(viewport).Action.Contains(point)) return "enter";
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
            if (target == "enter") ConfirmSelection();
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
            }
        }

        private Rectangle CalculateContentBounds()
        {
            if (_visibleNodes.Count == 0) return new Rectangle(0, 0, 1, 1);
            float halfWidth = _options.NodeWidth / 2f;
            float halfHeight = _options.NodeHeight / 2f;
            int left = (int)Math.Floor(_visibleNodes.Min(i => RoutePosition(_nodes[i]).X - halfWidth));
            int top = (int)Math.Floor(_visibleNodes.Min(i => RoutePosition(_nodes[i]).Y - halfHeight));
            int right = (int)Math.Ceiling(_visibleNodes.Max(i => RoutePosition(_nodes[i]).X + halfWidth));
            int bottom = (int)Math.Ceiling(_visibleNodes.Max(i => RoutePosition(_nodes[i]).Y + halfHeight));
            return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }

        private void RefreshScope()
        {
            _routeAge = 0;
            _folders = _hierarchy.Children(_scope);
            _visibleNodes = _hierarchy.Missions(_scope);
            LayoutRoute();
            _selectedFolder = Math.Clamp(_selectedFolder, 0, Math.Max(0, _folders.Count - 1));
            if (_visibleNodes.Count > 0 && !_visibleNodes.Contains(_selectedIndex))
                _selectedIndex = _visibleNodes.FirstOrDefault(i =>
                    CampaignCatalog.IsUnlocked(_progress, _nodes[i]) && !_progress.IsCompleted(_nodes[i].Id),
                    _visibleNodes[0]);
            _camera = new CampaignMapCamera(CalculateContentBounds(), _options.ContentPadding,
                _options.MinimumZoom, _options.MaximumZoom);
            _camera.SetViewport(_options.GetMapViewport(_graphicsDevice.Viewport));
            _fitZoom = _camera.Zoom;
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
#if FIDA_ANDROID
            // A new level starts at its own top and with no sheet left open.
            _mobileDetailsOpen = false;
            _mobileScroll = 0f;
            _mobileDragging = false;
#endif
            RefreshScope();
        }

        /// <summary>Opens a chapter by its displayed name; used by previews and tests.</summary>
        public bool TryEnterFolder(string title)
        {
            int index = -1;
            for (int i = 0; i < _folders.Count; i++)
                if (string.Equals(_folders[i], title, StringComparison.Ordinal)) { index = i; break; }
            if (index < 0) return false;
            _selectedFolder = index;
            EnterFolder(index);
            return true;
        }

        /// <summary>Pops one level of the chapter browser; used by previews and tests.</summary>
        public bool TryLeaveFolder()
        {
            if (_scope.Count == 0) return false;
            GoBack();
            return true;
        }

        private void GoBack()
        {
            if (_scope.Count == 0) { _backRequested = true; return; }
            string previous = _scope[^1];
            _scope.RemoveAt(_scope.Count - 1);
#if FIDA_ANDROID
            _mobileDetailsOpen = false;
            _mobileScroll = 0f;
            _mobileDragging = false;
#endif
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
            int height = Math.Clamp((viewport.Height - gap * (rows + 1)) / Math.Max(1, rows), 94, 210);
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
                DrawChapterCard(spriteBatch, FolderBounds(i), i, i == _selectedFolder);
        }

        private Rectangle NodeScreenBounds(CampaignNode node)
        {
            Vector2 position=RoutePosition(node);
            float width = _options.NodeWidth * CampaignRouteDiagram.WidthScale(node.Id);
            var world = new Rectangle((int)(position.X - width / 2f),
                (int)(position.Y - _options.NodeHeight / 2f),
                (int)width, (int)_options.NodeHeight);
            return _camera.WorldToScreen(world);
        }

        private Vector2 RoutePosition(CampaignNode node) => _routePositions.TryGetValue(node.Id,out var position)
            ? position : new Vector2(node.MapX,node.MapY);

        private void LayoutRoute()
        {
            _routePositions.Clear();
#if !FIDA_ANDROID
            if (CampaignRouteDiagram.TryArrange(_visibleNodes.Select(i => _nodes[i]).ToArray(), _routePositions)) return;
            var visible=_visibleNodes.Select(i=>_nodes[i]).ToDictionary(n=>n.Id);
            var depths=new Dictionary<string,int>();
            int Depth(string id,HashSet<string> path)
            {
                if(depths.TryGetValue(id,out int known)) return known;
                if(!path.Add(id)) return 0;
                int depth=0;
                foreach(string parent in visible[id].Parents)
                    if(visible.ContainsKey(parent)) depth=Math.Max(depth,Depth(parent,path)+1);
                path.Remove(id); depths[id]=depth; return depth;
            }
            foreach(var node in visible.Values) Depth(node.Id,new HashSet<string>());
            int rowOffset = 0;
            foreach(var row in visible.Values.GroupBy(n=>depths[n.Id]).OrderBy(group => group.Key))
            {
                var nodes=row.OrderBy(n=>n.MapX).ThenBy(n=>n.Id).ToArray();
                for(int i=0;i<nodes.Length;i++)
                {
                    int columns = Math.Min(2, nodes.Length - i / 2 * 2);
                    _routePositions[nodes[i].Id]=new Vector2((i % 2-(columns-1)/2f)*390,(rowOffset+i/2)*245);
                }
                rowOffset += (nodes.Length + 1) / 2;
            }
#endif
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
            return new CampaignMapLayout(viewport).Element("Minus");
#endif
        }

        private static Rectangle FitButton(Viewport viewport)
        {
#if FIDA_ANDROID
            return new Rectangle(112, viewport.Height - 78, 92, 56);
#else
            return new CampaignMapLayout(viewport).Element("Fit");
#endif
        }

        private static Rectangle PlusButton(Viewport viewport)
        {
#if FIDA_ANDROID
            return new Rectangle(244, viewport.Height - 78, 72, 56);
#else
            return new CampaignMapLayout(viewport).Element("Plus");
#endif
        }

        private Rectangle BackButton(Viewport viewport)
        {
#if !FIDA_ANDROID
            return new CampaignMapLayout(viewport).Back;
#else
            Rectangle frame = _camera.Viewport;
            Rectangle icon = UIIconButton.StandardBounds(viewport);
            return new Rectangle(frame.Left + 24, frame.Top + 24, icon.Width, icon.Height);
#endif
        }

#if !FIDA_ANDROID
        private static int DesktopControlScale(Viewport viewport) =>
            Math.Max(1, (int)Math.Round(UIDesktopLayout.Scale(viewport) * 1000));

        private static int ScaleDesktop(int value, int scaleThousandths) =>
            Math.Max(1, (int)Math.Round(value * scaleThousandths / 1000f));
#endif

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

    }
}
