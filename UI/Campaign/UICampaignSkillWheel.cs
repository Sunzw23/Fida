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
    /// A single-focus, circular skill browser. It reports user intent but never mutates
    /// campaign state, keeping loadout rules and state transitions outside the UI control.
    /// </summary>
    public sealed class UICampaignSkillWheel : UIControl
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        private readonly SpriteFont _titleFont;
        private readonly CampaignNode _node;
        private readonly CampaignProgress _progress;
        private readonly IReadOnlyCollection<SkillType> _selected;
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

        public SkillType FocusedSkill => _wheel.Focused;
        public int FocusedIndex => _wheel.FocusedIndex;

        public UICampaignSkillWheel(int id, GraphicsDevice graphicsDevice, Texture2D pixel,
            SpriteFont font, SpriteFont titleFont, CampaignNode node, CampaignProgress progress,
            IReadOnlyCollection<SkillType> selected, IReadOnlyList<SkillType> skills,
            CampaignSkillWheelOptions options = null) : base(id)
        {
            _graphicsDevice = graphicsDevice;
            _pixel = pixel;
            _font = font;
            _titleFont = titleFont;
            _node = node;
            _progress = progress;
            _selected = selected;
            _options = options ?? new CampaignSkillWheelOptions();
            SkillType? initial = selected.Count > 0 ? selected.First() : FindFirstAvailable(skills);
            _wheel = new SkillWheelModel(skills, initial);
        }

        public bool TryConsumeToggle(out SkillType skill)
        {
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
            bool result = _backRequested;
            _backRequested = false;
            return result;
        }

        public bool ConsumeStartRequest()
        {
            bool result = _startRequested;
            _startRequested = false;
            return result;
        }

        public void Move(int steps) => _wheel.Move(steps);

        public override void Update(GameTime gameTime)
        {
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
            if (!_inputInitialized)
            {
                _previousMouse = mouse;
                _previousKeyboard = Keyboard.GetState();
                _previousGamePad = GamePad.GetState(PlayerIndex.One);
                _inputInitialized = true;
                return;
            }

            Point point = mouse.Position;
            int wheelDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
            if (wheelDelta != 0 && PanelBounds(_graphicsDevice.Viewport).Contains(point))
            {
                int steps = Math.Max(1, Math.Abs(wheelDelta) / 120);
                Move(wheelDelta > 0 ? -steps : steps);
            }

            bool pressed = mouse.LeftButton == ButtonState.Pressed &&
                           _previousMouse.LeftButton == ButtonState.Released;
            bool released = mouse.LeftButton == ButtonState.Released &&
                            _previousMouse.LeftButton == ButtonState.Pressed;
            if (pressed) _pressedTarget = HitTarget(point);
            if (released)
            {
                string releasedTarget = HitTarget(point);
                if (releasedTarget == _pressedTarget) Activate(releasedTarget);
                _pressedTarget = "";
            }

            _previousMouse = mouse;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            Viewport viewport = _graphicsDevice.Viewport;
            Rectangle panel = PanelBounds(viewport);
            spriteBatch.Draw(_pixel, panel, UITheme.Panel);
            UIUtils.DrawGlowFrame(spriteBatch, panel, _pixel, UITheme.Gold, 3, 0.72f);

            DrawHeader(spriteBatch, viewport, panel);
            DrawWheel(spriteBatch, viewport, panel, gameTime);
            DrawFooter(spriteBatch, viewport, panel);
        }

        private void DrawHeader(SpriteBatch spriteBatch, Viewport viewport, Rectangle panel)
        {
            float centerX = viewport.Width / 2f;
            DrawCentered(spriteBatch, _titleFont, $"{_node.Chapter} - {_node.Name}",
                new Vector2(centerX, panel.Top + 48), panel.Width - 80, 0.54f, UITheme.Ink);
            DrawCentered(spriteBatch, _font, _node.Story,
                new Vector2(centerX, panel.Top + 112), panel.Width - 100, 0.50f, UITheme.InkMuted);

            int used = CampaignSkillBalance.TotalCost(_selected);
            DrawCentered(spriteBatch, _font,
                $"Skill Points: {used} / {_node.SkillBudget}    Reward: {_node.RewardText}",
                new Vector2(centerX, panel.Top + 174), panel.Width - 100, 0.46f, UITheme.Ink);

            string loadout = _selected.Count == 0
                ? "Loadout: None"
                : "Loadout: " + string.Join(", ", _selected.Select(GameText.SkillName));
            DrawCentered(spriteBatch, _font, loadout,
                new Vector2(centerX, panel.Top + 222), panel.Width - 100, 0.42f, UITheme.Ink);
            DrawCentered(spriteBatch, _font,
                "Mouse wheel / arrows / D-pad: browse    Enter / A / click: equip or remove",
                new Vector2(centerX, panel.Top + 268), panel.Width - 100, 0.46f, UITheme.InkMuted);
        }

        private void DrawWheel(SpriteBatch spriteBatch, Viewport viewport, Rectangle panel,
            GameTime gameTime)
        {
            float centerX = viewport.Width / 2f;
            int centerY = panel.Top + _options.WheelCenterOffsetFromPanelTop;
#if FIDA_ANDROID
            centerY = Math.Min(centerY, panel.Bottom - 500);
#endif
            int focusedWidth = (int)(panel.Width * _options.FocusedWidthRatio);
            Rectangle focusedBounds = new((int)centerX - focusedWidth / 2,
                centerY - _options.FocusedHeight / 2, focusedWidth, _options.FocusedHeight);

            spriteBatch.Draw(_pixel, new Rectangle((int)centerX - 1, centerY - 250, 2, 500), UITheme.Gold);
            spriteBatch.Draw(_pixel, new Rectangle((int)centerX + 3, centerY - 250, 1, 500),
                UITheme.AccentViolet * 0.62f);

            for (int offset = -_options.VisibleNeighborCount; offset <= _options.VisibleNeighborCount; offset++)
            {
                SkillType skill = _wheel.GetAtOffset(offset);
                if (offset == 0)
                {
                    DrawFocusedSkill(spriteBatch, skill, focusedBounds, gameTime);
                    continue;
                }

                int y = centerY + offset * _options.NeighborSpacing;
                bool near = Math.Abs(offset) == 1;
                float scale = near ? _options.NearTextScale : _options.FarTextScale;
                Color color = near ? UITheme.InkMuted : UITheme.AccentBlue * 0.72f;
                string selectedMarker = _selected.Contains(skill) ? "[EQUIPPED] " : "";
                string text = $"{selectedMarker}{GameText.SkillName(skill)} [{CampaignSkillBalance.Cost(skill)} SP]";
                DrawCentered(spriteBatch, _font, text, new Vector2(centerX, y),
                    focusedWidth * (near ? 0.92f : 0.76f), scale, color);
            }

            DrawSmallButton(spriteBatch, PreviousButton(focusedBounds), "UP");
            DrawSmallButton(spriteBatch, NextButton(focusedBounds), "DOWN");
        }

        private void DrawFocusedSkill(SpriteBatch spriteBatch, SkillType skill,
            Rectangle bounds, GameTime gameTime)
        {
            CampaignSkillAvailability availability = Availability(skill);
            bool selected = availability == CampaignSkillAvailability.Selected;
            Color background = selected ? UITheme.PanelPressed :
                availability == CampaignSkillAvailability.Available ? UITheme.PanelLight : UITheme.PanelDark;
            spriteBatch.Draw(_pixel, bounds, background);
            float pulse = 0.70f + 0.24f * (float)Math.Sin(
                gameTime.TotalGameTime.TotalSeconds * 5.2);
            Color accent = selected ? UITheme.AccentGreen : UITheme.GoldBright;
            UIUtils.DrawGlowFrame(spriteBatch, bounds, _pixel, accent, 4, pulse);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top, 7, bounds.Height),
                UITheme.AccentViolet);

            DrawCentered(spriteBatch, _font,
                $"{GameText.SkillName(skill)}   {CampaignSkillBalance.Cost(skill)} SP",
                new Vector2(bounds.Center.X, bounds.Center.Y - 22), bounds.Width - 28,
                _options.FocusedTextScale, availability == CampaignSkillAvailability.Locked ? UITheme.InkMuted : UITheme.Ink);
            DrawCentered(spriteBatch, _font, AvailabilityText(availability),
                new Vector2(bounds.Center.X, bounds.Center.Y + 31), bounds.Width - 28,
                0.46f, availability == CampaignSkillAvailability.Available || selected ? UITheme.Ink : UITheme.InkMuted);
        }

        private void DrawFooter(SpriteBatch spriteBatch, Viewport viewport, Rectangle panel)
        {
            DrawSmallButton(spriteBatch, BackButton(viewport, panel), "Back to Map");
            DrawSmallButton(spriteBatch, StartButton(viewport, panel), "Start Battle");
#if !FIDA_ANDROID
            DrawCentered(spriteBatch, _font, "F5 / Controller Start also begins the battle",
                new Vector2(viewport.Width / 2f, panel.Bottom - 34), panel.Width - 80, 0.28f, UITheme.InkMuted);
#endif
        }

        private void DrawSmallButton(SpriteBatch spriteBatch, Rectangle bounds, string text)
        {
            spriteBatch.Draw(_pixel, bounds, UITheme.PanelLight);
            UIUtils.DrawGlowFrame(spriteBatch, bounds, _pixel, UITheme.Gold, 2, 0.48f);
            DrawCentered(spriteBatch, _font, text, bounds.Center.ToVector2(), bounds.Width - 12, 0.56f, UITheme.Ink);
        }

        private CampaignSkillAvailability Availability(SkillType skill) =>
            CampaignLoadout.Evaluate(_progress, _node, _selected, skill);

        private string AvailabilityText(CampaignSkillAvailability availability) => availability switch
        {
            CampaignSkillAvailability.Selected => "EQUIPPED - CONFIRM TO REMOVE",
            CampaignSkillAvailability.Available => "AVAILABLE - CONFIRM TO EQUIP",
            CampaignSkillAvailability.Locked => "LOCKED - COMPLETE EARLIER NODES",
            CampaignSkillAvailability.OverBudget => "NOT ENOUGH SKILL POINTS",
            CampaignSkillAvailability.Conflicting => "CONFLICTS WITH THE CURRENT LOADOUT",
            _ => "UNAVAILABLE"
        };

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
            if (focused.Contains(point)) return "toggle";
            if (PreviousButton(focused).Contains(point)) return "previous";
            if (NextButton(focused).Contains(point)) return "next";
            if (BackButton(viewport, panel).Contains(point)) return "back";
            if (StartButton(viewport, panel).Contains(point)) return "start";

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

        private void Activate(string target)
        {
            if (target == "toggle") ToggleFocused();
            else if (target == "previous") Move(-1);
            else if (target == "next") Move(1);
            else if (target == "back") _backRequested = true;
            else if (target == "start") _startRequested = true;
            else if (target.StartsWith("offset:", StringComparison.Ordinal) &&
                     int.TryParse(target.AsSpan(7), out int offset)) Move(offset);
        }

        private void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 center,
            float maximumWidth, float preferredScale, Color color)
        {
            text = UIFontGuard.Sanitize(font, text);
            Vector2 measured = font.MeasureString(text);
            if (measured.X <= 0f) return;
            float scale = Math.Max(0.30f, Math.Min(preferredScale, maximumWidth / measured.X));
            spriteBatch.DrawString(font, text, center - measured * scale / 2f, color, 0f,
                Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

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
#if FIDA_ANDROID
            centerY = Math.Min(centerY, panel.Bottom - 500);
#endif
            return new Rectangle(viewport.Width / 2 - width / 2,
                centerY - _options.FocusedHeight / 2, width, _options.FocusedHeight);
        }

        private static Rectangle PreviousButton(Rectangle focused) =>
            new(focused.Right + 24, focused.Center.Y - 57, 96, 46);

        private static Rectangle NextButton(Rectangle focused) =>
            new(focused.Right + 24, focused.Center.Y + 11, 96, 46);

        private static Rectangle BackButton(Viewport viewport, Rectangle panel) =>
#if FIDA_ANDROID
            new(panel.Left + 48, panel.Bottom - 178, panel.Width - 96, 64);
#else
            new(panel.Left + 42, panel.Bottom - 108, 280, 60);
#endif

        private static Rectangle StartButton(Viewport viewport, Rectangle panel) =>
#if FIDA_ANDROID
            new(panel.Left + 48, panel.Bottom - 98, panel.Width - 96, 64);
#else
            new(panel.Right - 322, panel.Bottom - 108, 280, 60);
#endif
    }
}
