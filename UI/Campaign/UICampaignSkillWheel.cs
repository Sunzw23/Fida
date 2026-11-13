#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI.Campaign
{
    /// <summary>
    /// Compatibility facade for campaign, cooperative loadout and duel draft states.
    /// The grid handles presentation and intent; the existing rules remain authoritative.
    /// </summary>
    public sealed class UICampaignSkillWheel : UIControl
    {
        /// <summary>New grid presentation behind the established state-machine API.</summary>
        public UICampaignSkillGrid GridBrowser { get; private set; }
        public bool SelectionEnabled { get; set; } = true;
        private void SyncGrid()
        {
            GridBrowser.HeaderText = HeaderText;
            GridBrowser.StatusText = StatusText;
            GridBrowser.AvailableActionText = AvailableActionText;
            GridBrowser.SelectedActionText = SelectedActionText;
            GridBrowser.StartActionText = StartActionText;
            GridBrowser.StartActionEnabled = StartActionEnabled;
            GridBrowser.StartActionTogglesSelection = StartActionTogglesSelection;
            GridBrowser.UseUnifiedActionButton = UseUnifiedActionButton;
            GridBrowser.ShowSkillPoints = ShowSkillPoints;
            GridBrowser.AvailabilityOverride = AvailabilityOverride;
            GridBrowser.AvailabilityLabelOverride = AvailabilityLabelOverride;
            GridBrowser.InputBlocked = InputBlocked;
            GridBrowser.SelectionEnabled = SelectionEnabled;
            GridBrowser.IsVisible = IsVisible;
            GridBrowser.SetSelection(_selected);
        }
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        private readonly SpriteFont _titleFont;
        private readonly CampaignNode _node;
        private readonly CampaignProgress _progress;
        private IReadOnlyCollection<SkillType> _selected;
        private readonly CampaignSkillWheelOptions _options;
        private readonly SkillWheelModel _wheel;

        private KeyboardState _previousKeyboard;
        private GamePadState _previousGamePad;
        private MouseState _previousMouse;
        private bool _inputInitialized;
        private string _pressedTarget = "";
        private SkillType? _pendingToggle;
        private bool _backRequested;
        private bool _startRequested;
        private Func<SkillType, CampaignSkillAvailability> _availabilityOverride = null!;
        private Func<SkillType, string> _availabilityLabelOverride = null!;
        private string _headerText;
        private string _statusText = "";
        private float _selectionReveal;
        private string _availableActionText = "EQUIP SKILL";
        private string _selectedActionText = "REMOVE SKILL";
        private string _backActionText = "<";
        private string _startActionText;
#if FIDA_ANDROID
        private bool _touchingWheel;
        private bool _touchMoved;
        private int _touchStartY;
        private float _touchWheelOffset;
#endif

        public SkillType FocusedSkill => GridBrowser?.FocusedSkill ?? _wheel.Focused;
        public int FocusedIndex => GridBrowser?.FocusedIndex ?? _wheel.FocusedIndex;
        public string HeaderText { get => _headerText; set => _headerText = value ?? ""; }
        public string StatusText { get => _statusText; set => _statusText = value ?? ""; }
        public string AvailableActionText { get => _availableActionText; set => _availableActionText = value ?? ""; }
        public string SelectedActionText { get => _selectedActionText; set => _selectedActionText = value ?? ""; }
        public string BackActionText { get => _backActionText; set => _backActionText = value ?? ""; }
        public string StartActionText { get => _startActionText; set => _startActionText = value ?? ""; }
        public bool StartActionEnabled { get; set; } = true;
        public bool InputBlocked { get; set; }
        public bool StartActionTogglesSelection { get; set; }
        public bool UseUnifiedActionButton { get; set; }
        public Func<SkillType, CampaignSkillAvailability> AvailabilityOverride
        {
            get => _availabilityOverride;
            set => _availabilityOverride = value;
        }
        public Func<SkillType, string> AvailabilityLabelOverride
        {
            get => _availabilityLabelOverride;
            set => _availabilityLabelOverride = value;
        }
        public bool ShowSkillPoints { get; set; } = true;

        public void SetSelection(IReadOnlyCollection<SkillType> selected)
        {
            _selected = selected;
            GridBrowser?.SetSelection(selected);
        }

        public UICampaignSkillWheel(int id, GraphicsDevice graphicsDevice, Texture2D pixel,
            SpriteFont font, SpriteFont titleFont, CampaignNode node, CampaignProgress progress,
            IReadOnlyCollection<SkillType> selected, IReadOnlyList<SkillType> skills,
            CampaignSkillWheelOptions? options = null, SpriteFont? captionFont = null, SpriteFont? labelFont = null) : base(id)
        {
            _graphicsDevice = graphicsDevice;
            _pixel = pixel;
            _font = font;
            _titleFont = titleFont;
            _node = node;
            _progress = progress;
            _selected = selected;
            _headerText = node.Name;
#if FIDA_ANDROID
            _startActionText = "START";
#else
            _startActionText = "START BATTLE  >";
#endif
            _options = options ?? new CampaignSkillWheelOptions();
            SkillType? initial = selected.Count > 0 ? selected.First() : FindFirstAvailable(skills);
            _wheel = new SkillWheelModel(skills, initial);
            GridBrowser = new UICampaignSkillGrid(id, graphicsDevice, pixel, font, titleFont, node, progress,
                selected, skills, captionFont, labelFont);
        }

        public bool TryConsumeToggle(out SkillType skill)
        {
            if (GridBrowser != null) return GridBrowser.TryConsumeToggle(out skill);
            if (_pendingToggle.HasValue)
            {
                skill = _pendingToggle.Value;
                _pendingToggle = null;
                return true;
            }
            skill = default;
            return false;
        }

        public bool ConsumeBackRequest()
        {
            if (GridBrowser != null) return GridBrowser.ConsumeBackRequest();
            bool result = _backRequested;
            _backRequested = false;
            return result;
        }

        public bool ConsumeStartRequest()
        {
            if (GridBrowser != null) return GridBrowser.ConsumeStartRequest();
            bool result = _startRequested;
            _startRequested = false;
            return result;
        }

        public void Move(int steps)
        {
            if (GridBrowser != null) GridBrowser.Move(steps); else _wheel.Move(steps);
        }

        public override void Update(GameTime gameTime)
        {
            if (GridBrowser != null) { SyncGrid(); GridBrowser.Update(gameTime); return; }
            if (InputBlocked)
            {
                _previousKeyboard = Keyboard.GetState();
                _previousGamePad = GamePad.GetState(PlayerIndex.One);
                return;
            }
            _selectionReveal = UIMotion.Approach(_selectionReveal, Availability(_wheel.Focused) == CampaignSkillAvailability.Selected ? 1f : 0f, gameTime.ElapsedGameTime.TotalSeconds);
#if FIDA_ANDROID
            if (!_touchingWheel && Math.Abs(_touchWheelOffset) > 0.1f)
            {
                float settle = 1f - (float)Math.Pow(0.002, gameTime.ElapsedGameTime.TotalSeconds);
                _touchWheelOffset = MathHelper.Lerp(_touchWheelOffset, 0f, settle);
                if (Math.Abs(_touchWheelOffset) < 0.5f) _touchWheelOffset = 0f;
            }
#endif
            KeyboardState keyboard = Keyboard.GetState();
            GamePadState gamePad = GamePad.GetState(PlayerIndex.One);
            if (!_inputInitialized)
            {
                _previousKeyboard = keyboard;
                _previousGamePad = gamePad;
                return;
            }

            if (Pressed(keyboard, Keys.Up) || Pressed(keyboard, Keys.Left)) Move(-1);
            if (Pressed(keyboard, Keys.Down) || Pressed(keyboard, Keys.Right)) Move(1);
            if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space)) ToggleFocused();
            if (Pressed(keyboard, Keys.Escape)) _backRequested = true;
            if (Pressed(keyboard, Keys.F5)) _startRequested = true;

            if (gamePad.IsConnected)
            {
                if (Pressed(gamePad, Buttons.DPadUp) || Pressed(gamePad, Buttons.DPadLeft)) Move(-1);
                if (Pressed(gamePad, Buttons.DPadDown) || Pressed(gamePad, Buttons.DPadRight)) Move(1);
                if (CrossedUp(gamePad.ThumbSticks.Left.Y, _previousGamePad.ThumbSticks.Left.Y)) Move(-1);
                if (CrossedDown(gamePad.ThumbSticks.Left.Y, _previousGamePad.ThumbSticks.Left.Y)) Move(1);
                if (Pressed(gamePad, Buttons.A)) ToggleFocused();
                if (Pressed(gamePad, Buttons.B) || Pressed(gamePad, Buttons.Back)) _backRequested = true;
                if (Pressed(gamePad, Buttons.Start)) _startRequested = true;
            }

            _previousKeyboard = keyboard;
            _previousGamePad = gamePad;
        }

        public override void Update(MouseState mouse)
        {
            if (GridBrowser != null) { SyncGrid(); GridBrowser.Update(mouse); return; }
            if (InputBlocked)
            {
                _previousMouse = mouse;
                return;
            }
            if (!_inputInitialized)
            {
                _previousMouse = mouse;
                _previousKeyboard = Keyboard.GetState();
                _previousGamePad = GamePad.GetState(PlayerIndex.One);
                _inputInitialized = true;
                return;
            }

            Point point = mouse.Position;
#if !FIDA_ANDROID
            int wheelDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
            if (wheelDelta != 0 && PanelBounds(_graphicsDevice.Viewport).Contains(point))
            {
                int steps = Math.Max(1, Math.Abs(wheelDelta) / 120);
                Move(wheelDelta > 0 ? -steps : steps);
            }
#endif

            bool pressed = mouse.LeftButton == ButtonState.Pressed &&
                           _previousMouse.LeftButton == ButtonState.Released;
            bool released = mouse.LeftButton == ButtonState.Released &&
                            _previousMouse.LeftButton == ButtonState.Pressed;
#if FIDA_ANDROID
            if (pressed)
            {
                _pressedTarget = HitTarget(point);
                _touchingWheel = WheelGestureBounds(_graphicsDevice.Viewport).Contains(point);
                _touchMoved = false;
                _touchStartY = point.Y;
                _touchWheelOffset = 0f;
            }

            if (_touchingWheel && !pressed && mouse.LeftButton == ButtonState.Pressed)
            {
                int deltaY = point.Y - _previousMouse.Y;
                int spacing = Math.Max(1, _options.NeighborSpacing);
                deltaY = Math.Clamp(deltaY, -spacing * 2, spacing * 2);
                _touchWheelOffset += deltaY;
                if (Math.Abs(point.Y - _touchStartY) >= 12)
                {
                    _touchMoved = true;
                    _pressedTarget = "";
                }

                float switchThreshold = spacing * 0.5f;
                while (_touchWheelOffset <= -switchThreshold)
                {
                    Move(1);
                    _touchWheelOffset += spacing;
                }
                while (_touchWheelOffset >= switchThreshold)
                {
                    Move(-1);
                    _touchWheelOffset -= spacing;
                }
            }
#else
            if (pressed) _pressedTarget = HitTarget(point);
#endif
            if (released)
            {
                string releasedTarget = HitTarget(point);
                if (
#if FIDA_ANDROID
                    !_touchMoved &&
#endif
                    releasedTarget == _pressedTarget) Activate(releasedTarget);
                _pressedTarget = "";
#if FIDA_ANDROID
                _touchingWheel = false;
                _touchMoved = false;
#endif
            }

            _previousMouse = mouse;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (GridBrowser != null) { SyncGrid(); GridBrowser.Draw(spriteBatch, gameTime); return; }
            Viewport viewport = _graphicsDevice.Viewport;
            Rectangle panel = PanelBounds(viewport);
            spriteBatch.Draw(_pixel, panel, UITheme.Panel);
            UIUtils.DrawRectangleOutlines(spriteBatch, panel, UITheme.Line, 1, _pixel);

            DrawHeader(spriteBatch, viewport, panel);
            DrawWheel(spriteBatch, viewport, panel, gameTime);
#if !FIDA_ANDROID
            DrawDesktopDetails(spriteBatch, panel);
#else
            DrawMobileDetails(spriteBatch, panel);
#endif
            DrawFooter(spriteBatch, viewport, panel, gameTime);
        }

        private void DrawHeader(SpriteBatch spriteBatch, Viewport viewport, Rectangle panel)
        {
            float centerX = viewport.Width / 2f;
#if FIDA_ANDROID
            DrawCentered(spriteBatch, _titleFont, _headerText,
                new Vector2(centerX, panel.Top + 62), panel.Width - 64, 0.60f, UITheme.Ink);
            if (!string.IsNullOrWhiteSpace(_statusText))
                DrawCentered(spriteBatch, _font, _statusText,
                    new Vector2(centerX, panel.Top + 105), panel.Width - 64, 0.46f, UITheme.InkMuted);
            if (ShowSkillPoints)
            {
                int used = CampaignSkillBalance.TotalCost(_selected);
                DrawSkillPoints(spriteBatch, new Vector2(centerX, panel.Top + 137),
                    panel.Width - 96, used);
            }
            DrawMobileLoadout(spriteBatch, panel);
#else
            DrawCentered(spriteBatch, _titleFont, _headerText,
                new Vector2(centerX, panel.Top + 58), panel.Width - 100, 0.48f, UITheme.Ink);
            if (!string.IsNullOrWhiteSpace(_statusText))
                DrawCentered(spriteBatch, _font, _statusText,
                    new Vector2(centerX, panel.Top + 101), panel.Width - 100, 0.42f, UITheme.InkMuted);
            if (ShowSkillPoints)
            {
                int used = CampaignSkillBalance.TotalCost(_selected);
                DrawSkillPoints(spriteBatch, new Vector2(centerX, panel.Top + 143),
                    panel.Width - 200, used);
            }
#endif
        }

        private void DrawSkillPoints(SpriteBatch spriteBatch, Vector2 center,
            int availableWidth, int used)
        {
            int budget = Math.Max(0, _node.SkillBudget);
            if (budget == 0) return;
            float spacing = Math.Min(34f, availableWidth / (float)budget);
            float size = Math.Min(18f, spacing * 0.55f);
            float startX = center.X - (budget - 1) * spacing / 2f;
            for (int i = 0; i < budget; i++)
            {
                Vector2 position = new(startX + i * spacing, center.Y);
                Color color = i < used ? UITheme.AccentGreen : UITheme.GoldBright;
                spriteBatch.Draw(_pixel, position, null, color, MathHelper.PiOver4,
                    new Vector2(0.5f, 0.5f), new Vector2(size, size), SpriteEffects.None, 0f);
                if (i >= used)
                    spriteBatch.Draw(_pixel, position, null, UITheme.PanelDark, MathHelper.PiOver4,
                        new Vector2(0.5f, 0.5f), new Vector2(size - 4f, size - 4f),
                        SpriteEffects.None, 0f);
            }
        }

        private void DrawWheel(SpriteBatch spriteBatch, Viewport viewport, Rectangle panel,
            GameTime gameTime)
        {
            float centerX = viewport.Width / 2f;
            int centerY = panel.Top + _options.WheelCenterOffsetFromPanelTop;
#if FIDA_ANDROID
            centerY = MobileWheelCenterY(panel);
#else
            centerX = DesktopWheelCenterX(panel);
            centerY = DesktopWheelCenterY(panel);
#endif
            int focusedWidth =
#if FIDA_ANDROID
                (int)(panel.Width * _options.FocusedWidthRatio);
#else
                DesktopWheelWidth(panel);
#endif
            Rectangle focusedBounds = new((int)centerX - focusedWidth / 2,
                centerY - _options.FocusedHeight / 2, focusedWidth, _options.FocusedHeight);
#if FIDA_ANDROID
            float wheelOffset = _touchWheelOffset;
#else
            const float wheelOffset = 0f;
#endif

            for (int offset = -_options.VisibleNeighborCount; offset <= _options.VisibleNeighborCount; offset++)
            {
                SkillType skill = _wheel.GetAtOffset(offset);
                if (offset == 0)
                {
                    DrawFocusedSkill(spriteBatch, skill, focusedBounds, gameTime, wheelOffset);
                    continue;
                }

                float y = centerY + offset * _options.NeighborSpacing + wheelOffset;
                bool near = Math.Abs(offset) == 1;
                float scale = near ? _options.NearTextScale : _options.FarTextScale;
                Color color = UITheme.InkMuted * (near ? 1f : 0.72f);
                string text =
#if FIDA_ANDROID
                    $"{(_selected.Contains(skill) ? "+ " : "")}{GameText.SkillName(skill)}";
#else
                    $"{(_selected.Contains(skill) ? "+ " : "")}{GameText.SkillName(skill)}";
#endif
                DrawCentered(spriteBatch, _font, text, new Vector2(centerX, y),
                    focusedWidth * (near ? 0.92f : 0.76f), scale,
                    _selected.Contains(skill) ? UITheme.AccentGreen : color);
            }

#if !FIDA_ANDROID
            DrawArrowButton(spriteBatch, PreviousButton(focusedBounds), true);
            DrawArrowButton(spriteBatch, NextButton(focusedBounds), false);
#endif
        }

        private void DrawFocusedSkill(SpriteBatch spriteBatch, SkillType skill,
            Rectangle bounds, GameTime gameTime, float contentOffsetY)
        {
            CampaignSkillAvailability availability = Availability(skill);
            bool selected = availability == CampaignSkillAvailability.Selected;
            Color background = selected ? UITheme.PanelPressed :
                availability == CampaignSkillAvailability.Available ? UITheme.PanelLight : UITheme.PanelDark;
            spriteBatch.Draw(_pixel, bounds, background);
            float pulse = .65f + .25f * UIMotion.Pulse(gameTime.TotalGameTime.TotalSeconds, 2.6f);
            Color accent = selected ? UITheme.AccentGreen : CampaignSkillPresentation.Accent(skill);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Bottom - 5, (int)(bounds.Width * _selectionReveal), 5), UITheme.AccentGreen);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds, selected ? UITheme.Gold : UITheme.InkMuted, 1, _pixel);

            DrawCentered(spriteBatch, _font,
                $"{UISkillIconCatalog.Glyph(skill)}   {GameText.SkillName(skill)}",
                new Vector2(bounds.Center.X, bounds.Center.Y - 22 + contentOffsetY), bounds.Width - 28,
                _options.FocusedTextScale, availability == CampaignSkillAvailability.Locked ? UITheme.InkMuted : UITheme.Ink);
#if FIDA_ANDROID
            const float statusScale = 0.54f;
#else
            const float statusScale = 0.46f;
#endif
            DrawCentered(spriteBatch, _font,
#if FIDA_ANDROID
                CostLabel(skill, availability),
#else
                CostLabel(skill, availability),
#endif
                new Vector2(bounds.Center.X, bounds.Center.Y + 31 + contentOffsetY), bounds.Width - 28,
                statusScale,
                availability == CampaignSkillAvailability.Available || selected ? UITheme.Ink : UITheme.InkMuted);
        }

        private void DrawFooter(SpriteBatch spriteBatch, Viewport viewport, Rectangle panel, GameTime gameTime)
        {
#if FIDA_ANDROID
            if (!UseUnifiedActionButton)
            {
                CampaignSkillAvailability availability = Availability(_wheel.Focused);
                string selectText = availability switch
                {
                    CampaignSkillAvailability.Selected => _selectedActionText,
                    CampaignSkillAvailability.Available => _availableActionText,
                    _ => AvailabilityLabel(_wheel.Focused, availability)
                };
                DrawSmallButton(spriteBatch, SelectButton(viewport, panel), selectText,
                    availability is CampaignSkillAvailability.Selected or CampaignSkillAvailability.Available);
            }
#endif
            UIIconButton.DrawBackButton(spriteBatch, _pixel, BackButton(viewport, panel));
            DrawSmallButton(spriteBatch, StartButton(viewport, panel),
                _startActionText, StartActionEnabled);
        }

        private string CostLabel(SkillType skill, CampaignSkillAvailability availability)
        {
            string label = AvailabilityLabel(skill, availability);
            return label is "LOCKED" or "AVAILABLE" or "EQUIPPED"
                ? $"{CampaignSkillBalance.Cost(skill)} SP"
                : $"{CampaignSkillBalance.Cost(skill)} SP  ·  {label}";
        }

        private void DrawSmallButton(SpriteBatch spriteBatch, Rectangle bounds, string text,
            bool enabled = true)
        {
            spriteBatch.Draw(_pixel, bounds, enabled ? UITheme.PanelLight : UITheme.PanelDark);
            UIUtils.DrawGlowFrame(spriteBatch, bounds, _pixel,
                enabled ? UITheme.Gold : UITheme.InkMuted, 2, enabled ? 0.48f : 0.25f);
            if (text is "LOCKED" or "UNAVAILABLE")
            {
                UISkillSymbols.Status(spriteBatch, _pixel,
                    new Rectangle(bounds.Center.X - 12, bounds.Center.Y - 13, 24, 26),
                    CampaignSkillAvailability.Locked, UITheme.InkMuted);
                return;
            }
            if (text is "START" or "START BATTLE  >" or "PLAY  >")
            {
                // Use the established navigation chevron, preserving the original button bounds.
                Vector2 center = bounds.Center.ToVector2();
                UISkillSymbols.Play(spriteBatch, _pixel, center, enabled ? UITheme.Ink : UITheme.InkMuted);
                return;
            }
            DrawCentered(spriteBatch, _font, text, bounds.Center.ToVector2(), bounds.Width - 12,
#if FIDA_ANDROID
                0.66f,
#else
                0.56f,
#endif
                enabled ? UITheme.Ink : UITheme.InkMuted);
        }

#if !FIDA_ANDROID
        private static int DesktopWheelCenterX(Rectangle panel) => panel.Left + panel.Width * 29 / 100;
        private static int DesktopWheelCenterY(Rectangle panel) => panel.Top + panel.Height / 2;
        private static int DesktopWheelWidth(Rectangle panel) => Math.Min(490, panel.Width * 38 / 100);

        private void DrawArrowButton(SpriteBatch spriteBatch, Rectangle bounds, bool up)
        {
            spriteBatch.Draw(_pixel, bounds, UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, bounds, UITheme.AccentBlue, 2, _pixel);
            int centerX = bounds.Center.X;
            int centerY = bounds.Center.Y;
            int direction = up ? -1 : 1;
            DrawArrowLine(spriteBatch, new Vector2(centerX - 12, centerY - direction * 5),
                new Vector2(centerX, centerY + direction * 7));
            DrawArrowLine(spriteBatch, new Vector2(centerX, centerY + direction * 7),
                new Vector2(centerX + 12, centerY - direction * 5));
        }

        private void DrawArrowLine(SpriteBatch spriteBatch, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            spriteBatch.Draw(_pixel, from, null, UITheme.Ink, (float)Math.Atan2(delta.Y, delta.X),
                Vector2.Zero, new Vector2(delta.Length(), 3), SpriteEffects.None, 0f);
        }

        private void DrawDesktopDetails(SpriteBatch spriteBatch, Rectangle panel)
        {
            int left = panel.Left + panel.Width * 56 / 100;
            Rectangle card = new(left, panel.Top + panel.Height * 24 / 100,
                panel.Right - left - 55, panel.Height * 58 / 100);
            SkillType skill = _wheel.Focused;
            CampaignSkillAvailability availability = Availability(skill);
            Color accent = availability == CampaignSkillAvailability.Locked
                ? UITheme.InkMuted : CampaignSkillPresentation.Accent(skill);
            var (family, detail) = CampaignSkillPresentation.Describe(skill);
            spriteBatch.Draw(_pixel, card, UITheme.PanelDark);
            UIUtils.DrawRectangleOutlines(spriteBatch, card, accent, 2, _pixel);
            spriteBatch.Draw(_pixel, new Rectangle(card.Left, card.Top, 6, card.Height), accent);
            Rectangle icon = new(card.Left + 30, card.Top + 28, 74, 74);
            spriteBatch.Draw(_pixel, icon, UITheme.PanelLight);
            UIUtils.DrawRectangleOutlines(spriteBatch, icon, accent, 2, _pixel);
            DrawCentered(spriteBatch, _titleFont, UISkillIconCatalog.Glyph(skill),
                icon.Center.ToVector2(), 80, 0.50f, accent);
            DrawCentered(spriteBatch, _font, family,
                new Vector2(card.Left + card.Width * 0.64f, card.Top + 65), card.Width - 150,
                0.42f, accent);
            DrawCentered(spriteBatch, _titleFont, GameText.SkillName(skill),
                new Vector2(card.Center.X, card.Top + Math.Min(133, card.Height * .35f)), card.Width - 64, 0.40f, UITheme.Ink);
            int descriptionTop = card.Top + (int)Math.Min(173, card.Height * 0.43f);
            UITextRenderer.DrawParagraph(spriteBatch, _font, detail,
                new Rectangle(card.Left + 32, descriptionTop, card.Width - 64,
                    Math.Max(1, card.Bottom - 112 - descriptionTop)), UITheme.Ink);
            DrawCentered(spriteBatch, _font,
                CostLabel(skill, availability),
                new Vector2(card.Center.X, card.Bottom - (UseUnifiedActionButton ? 54 : 80)),
                card.Width - 64, 0.39f,
                availability == CampaignSkillAvailability.Selected ? UITheme.AccentGreen : accent);
            if (!UseUnifiedActionButton)
            {
                Rectangle action = DesktopActionBounds(card);
                string actionText = availability == CampaignSkillAvailability.Selected
                    ? _selectedActionText
                    : availability == CampaignSkillAvailability.Available ? _availableActionText : "UNAVAILABLE";
                DrawSmallButton(spriteBatch, action, actionText,
                    availability is CampaignSkillAvailability.Selected or CampaignSkillAvailability.Available);
            }
        }

#endif
        private static string ShortAvailability(CampaignSkillAvailability availability) => availability switch
        {
            CampaignSkillAvailability.Selected => "EQUIPPED",
            CampaignSkillAvailability.Available => "AVAILABLE",
            CampaignSkillAvailability.Locked => "LOCKED",
            CampaignSkillAvailability.OverBudget => "NO SP",
            CampaignSkillAvailability.Conflicting => "CONFLICT",
            _ => "UNAVAILABLE"
        };

#if !FIDA_ANDROID
        private static Rectangle DesktopDetailBounds(Rectangle panel)
        {
            int left = panel.Left + panel.Width * 56 / 100;
            return new Rectangle(left, panel.Top + 220,
                panel.Right - left - 55, Math.Max(260, Math.Min(500, panel.Height - 360)));
        }

        private static Rectangle DesktopActionBounds(Rectangle detail) =>
            new(detail.Left + 38, detail.Bottom - 58, detail.Width - 76, 40);
#endif

#if FIDA_ANDROID
        private void DrawMobileLoadout(SpriteBatch spriteBatch, Rectangle panel)
        {
            int count = Math.Min(7, _selected.Count);
            if (count == 0) return;
            int size = 40;
            int gap = 9;
            int left = panel.Center.X - (count * size + (count - 1) * gap) / 2;
            int index = 0;
            foreach (SkillType skill in _selected)
            {
                if (index >= count) break;
                Rectangle chip = new(left + index * (size + gap), panel.Top + 181, size, size);
                spriteBatch.Draw(_pixel, chip, CampaignSkillPresentation.Accent(skill));
                DrawCentered(spriteBatch, _font, UISkillIconCatalog.Glyph(skill),
                    chip.Center.ToVector2(), size - 6, 0.54f, UITheme.PanelLight);
                index++;
            }
        }

        private void DrawMobileDetails(SpriteBatch spriteBatch, Rectangle panel)
        {
            SkillType skill = _wheel.Focused;
            var (family, detail) = CampaignSkillPresentation.Describe(skill);
            Color accent = Availability(skill) == CampaignSkillAvailability.Locked
                ? UITheme.InkMuted : CampaignSkillPresentation.Accent(skill);
            int top = MobileWheelCenterY(panel) + _options.NeighborSpacing + 78;
            int bottom = (UseUnifiedActionButton
                ? StartButton(_graphicsDevice.Viewport, panel)
                : SelectButton(_graphicsDevice.Viewport, panel)).Top - 24;
            if (bottom - top < 116) return;
            Rectangle card = new(panel.Left + 46, top, panel.Width - 92,
                Math.Min(208, bottom - top));
            spriteBatch.Draw(_pixel, card, UITheme.PanelDark);
            spriteBatch.Draw(_pixel, new Rectangle(card.Left, card.Top, 6, card.Height), accent);
            DrawCentered(spriteBatch, _font, family,
                new Vector2(card.Center.X, card.Top + 42), card.Width - 48, 0.45f, accent);
            DrawWrappedCentered(spriteBatch, _font, detail,
                new Vector2(card.Center.X, card.Center.Y + 19), card.Width - 42,
                0.50f, 39f, 2, UITheme.Ink);
        }

        private static int MobileWheelCenterY(Rectangle panel)
        {
            int desired = panel.Top + (int)(panel.Height * 0.38f);
            int min = panel.Top + 330;
            int max = Math.Max(min, panel.Bottom - 400);
            return Math.Clamp(desired, min, max);
        }
#endif

        private string AvailabilityLabel(SkillType skill, CampaignSkillAvailability availability) =>
            _availabilityLabelOverride?.Invoke(skill) ?? ShortAvailability(availability);

        private CampaignSkillAvailability Availability(SkillType skill) =>
            _availabilityOverride?.Invoke(skill) ??
            CampaignLoadout.Evaluate(_progress, _node, _selected, skill);

        private void ToggleFocused()
        {
            CampaignSkillAvailability availability = Availability(_wheel.Focused);
            if (availability is CampaignSkillAvailability.Available or CampaignSkillAvailability.Selected)
                _pendingToggle = _wheel.Focused;
        }

        private SkillType? FindFirstAvailable(IReadOnlyList<SkillType> skills)
        {
            foreach (SkillType skill in skills)
            {
                if (Availability(skill) == CampaignSkillAvailability.Available) return skill;
            }
            return skills.Count > 0 ? skills[0] : null;
        }

        private string HitTarget(Point point)
        {
            Viewport viewport = _graphicsDevice.Viewport;
            Rectangle panel = PanelBounds(viewport);
            Rectangle focused = FocusedBounds(viewport, panel);
#if FIDA_ANDROID
            if (!UseUnifiedActionButton && SelectButton(viewport, panel).Contains(point))
                return "toggle";
            if (StartButton(viewport, panel).Contains(point)) return "start";
            if (UseUnifiedActionButton && !StartActionTogglesSelection && focused.Contains(point))
                return "toggle";
#else
            if (!UseUnifiedActionButton &&
                DesktopActionBounds(DesktopDetailBounds(panel)).Contains(point)) return "toggle";
            if (!StartActionTogglesSelection && focused.Contains(point)) return "toggle";
            if (PreviousButton(focused).Contains(point)) return "previous";
            if (NextButton(focused).Contains(point)) return "next";
#endif
            if (BackButton(viewport, panel).Contains(point)) return "back";
#if !FIDA_ANDROID
            if (StartButton(viewport, panel).Contains(point)) return "start";
#endif

            int centerY = focused.Center.Y;
            for (int offset = -_options.VisibleNeighborCount; offset <= _options.VisibleNeighborCount; offset++)
            {
                if (offset == 0) continue;
                int y = centerY + offset * _options.NeighborSpacing;
                var row = new Rectangle(focused.X, y - 38, focused.Width, 76);
                if (row.Contains(point)) return "offset:" + offset;
            }
            return "";
        }

#if FIDA_ANDROID
        private Rectangle WheelGestureBounds(Viewport viewport)
        {
            Rectangle panel = PanelBounds(viewport);
            int top = panel.Top + 235;
            int bottom = MobileWheelCenterY(panel) + _options.NeighborSpacing + 45;
            return new Rectangle(panel.Left + 18, top,
                Math.Max(1, panel.Width - 36), Math.Max(1, bottom - top));
        }
#endif

        private void Activate(string target)
        {
            if (target == "toggle") ToggleFocused();
            else if (target == "select") ToggleFocused();
            else if (target == "previous") Move(-1);
            else if (target == "next") Move(1);
            else if (target == "back") _backRequested = true;
            else if (target == "start")
            {
                if (StartActionTogglesSelection) ToggleFocused();
                else _startRequested = true;
            }
            else if (target.StartsWith("offset:", StringComparison.Ordinal) &&
                     int.TryParse(target.AsSpan(7), out int offset)) Move(offset);
        }

        private void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 center,
            float maximumWidth, float preferredScale, Color color)
        {
            UIUtils.DrawCenteredFit(spriteBatch, font, text, center, maximumWidth,
                preferredScale, color, 0.30f);
        }

#if FIDA_ANDROID
        private void DrawWrappedCentered(SpriteBatch spriteBatch, SpriteFont font, string text,
            Vector2 center, float maximumWidth, float scale, float lineSpacing,
            int maximumLines, Color color)
        {
            var lines = new List<string>();
            string current = "";
            foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
                string safeCandidate = UIFontGuard.Sanitize(font, candidate);
                if (font.MeasureString(safeCandidate).X * scale <= maximumWidth ||
                    string.IsNullOrEmpty(current))
                {
                    current = candidate;
                    continue;
                }

                lines.Add(current);
                current = word;
                if (lines.Count == maximumLines - 1) break;
            }
            if (!string.IsNullOrEmpty(current) && lines.Count < maximumLines) lines.Add(current);
            if (lines.Count == 0) return;

            float startY = center.Y - (lines.Count - 1) * lineSpacing / 2f;
            for (int i = 0; i < lines.Count; i++)
                DrawCentered(spriteBatch, font, lines[i],
                    new Vector2(center.X, startY + i * lineSpacing), maximumWidth, scale, color);
        }
#endif

        private bool Pressed(KeyboardState current, Keys key) =>
            current.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

        private bool Pressed(GamePadState current, Buttons button) =>
            current.IsButtonDown(button) && _previousGamePad.IsButtonUp(button);

        private static bool CrossedUp(float current, float previous) => current > 0.55f && previous <= 0.55f;
        private static bool CrossedDown(float current, float previous) => current < -0.55f && previous >= -0.55f;

        private static Rectangle PanelBounds(Viewport viewport)
        {
#if FIDA_ANDROID
            Rectangle safe = UIMobileSafeArea.Get(viewport);
            return new Rectangle(safe.Left + 8, safe.Top + 8,
                Math.Max(1, safe.Width - 16), Math.Max(1, safe.Height - 16));
#else
            int width = Math.Min(viewport.Width - 72, 1400);
            return new Rectangle(viewport.Width / 2 - width / 2, 28, width, viewport.Height - 56);
#endif
        }

        private Rectangle FocusedBounds(Viewport viewport, Rectangle panel)
        {
            int width = (int)(panel.Width * _options.FocusedWidthRatio);
            int centerY = panel.Top + _options.WheelCenterOffsetFromPanelTop;
            int centerX = viewport.Width / 2;
#if FIDA_ANDROID
            centerY = MobileWheelCenterY(panel);
#else
            centerY = DesktopWheelCenterY(panel);
            centerX = DesktopWheelCenterX(panel);
            width = DesktopWheelWidth(panel);
#endif
            return new Rectangle(centerX - width / 2,
                centerY - _options.FocusedHeight / 2, width, _options.FocusedHeight);
        }

        private static Rectangle PreviousButton(Rectangle focused) =>
            new(focused.Right + 18, focused.Center.Y - 53, 54, 44);

        private static Rectangle NextButton(Rectangle focused) =>
            new(focused.Right + 18, focused.Center.Y + 9, 54, 44);

#if FIDA_ANDROID
        private static Rectangle SelectButton(Viewport viewport, Rectangle panel) =>
            new(panel.Left + 48, panel.Bottom - 178, panel.Width - 96, 64);
#endif

        private static Rectangle BackButton(Viewport viewport, Rectangle panel)
        {
            Rectangle icon = UIIconButton.StandardBounds(viewport);
            return new Rectangle(panel.Left + 12, panel.Top + 12, icon.Width, icon.Height);
        }

        private Rectangle StartButton(Viewport viewport, Rectangle panel) =>
#if FIDA_ANDROID
            UseUnifiedActionButton
                ? new Rectangle(panel.Left + 48, panel.Bottom - 106, panel.Width - 96, 72)
                : new Rectangle(panel.Left + 48, panel.Bottom - 106, panel.Width - 96, 72);
#else
            new(panel.Right - 322, panel.Bottom - 108, 280, 60);
#endif
    }
}
