using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Fida.Logic;
using Fida.Campaign;

namespace Fida.UI
{
    public class UIPlayer : UIControl
    {
        public Player Player { get; private set; }
        public Vector2 Position { get; set; }

        public SpriteFont Font { get; private set; }
        public Texture2D Pixel { get; set; }

        public Texture2D PortraitTexture { get; set; }
        public Rectangle? PortraitSource { get; set; }
        public bool IsCurrentTurn { get; set; }
        public Func<bool> CurrentTurnChecker { get; set; }
        public bool IsAlly { get; }

        /// <summary>Marks the acting portrait while an automated controller is deciding.</summary>
        public bool IsPresentingAction { get; set; }
        public Vector2 VisualOffset { get; set; } = Vector2.Zero;
        public float VisualScale { get; set; } = 1f;
        public float SelectionStrength { get; set; }
        public float ImpactStrength { get; set; }

        public UILabel NameLabel { get; private set; }

        public UIAnimatedButton Number0Button { get; private set; }
        public UILabel HealthLabel { get; private set; }
        public UIAnimatedButton Number1Button { get; private set; }

        public Rectangle Number0Bounds { get; private set; }
        public Rectangle HealthBounds { get; private set; }
        public Rectangle Number1Bounds { get; private set; }

        public Rectangle CardBounds { get; private set; }
        public Rectangle InnerCardBounds { get; private set; }
        public Rectangle PortraitBounds { get; private set; }

        public Rectangle GetNumberBounds(int numberIndex) =>
            numberIndex == 0 ? Number0Bounds : Number1Bounds;

        // 环境检查器 — 卡片在 ChooseTarget 时启用交互
        public Func<bool> EnabledChecker { get; set; }

        // 卡片交互状态
        private bool IsEnabled { get; set; } = true;
        private bool IsHovered { get; set; } = false;
        private bool _isPressed { get; set; } = false;
        private bool _clickStartedOnThis = false;
        private bool _mouseWasDown = false;
        private Vector2 _nameCenter;
        private Vector2 _healthCenter;
        private Rectangle _skillStripBounds;
        private int? _deferredHealth;
        private int _visibleHealth;
        private float _displayHealth;
        private int _healthDelta;
        private float _healthChangeAge = 1f;
        private int[] _deferredNumbers;
        private float _deathReveal = 1f;
        private bool _visuallyDefeated;
        private float _arrivalProgress = 1f;
        public bool DeferNumberVisual { get; private set; }
        private bool _pointerOverSkills;
#if FIDA_ANDROID
        private bool _showSkillDetails;
        private bool _skillTouchStarted;
        private bool _skillLongPressed;
        private float _skillHoldSeconds;
#endif
        public bool DeferHealthVisual { get; set; }

        // ─── 手柄焦点支持 ──────────────────────────────────
        /// <summary>
        /// 玩家卡片可以被手柄聚焦（当启用时）
        /// </summary>
        public override bool CanFocus => IsEnabled;

        public override Rectangle? GetScreenBounds(Viewport viewport) =>
            IsVisible ? CardBounds : null;

        /// <summary>
        /// 获取焦点时触发确认事件（手柄 A 键按下时调用）
        /// </summary>
        public void OnCardConfirmed()
        {
            OnConfirmed();
        }

        public UIPlayer(int id, Player player, Vector2 position, SpriteFont font, Texture2D pixel, Texture2D portraitTexture = null, bool isAlly = false, Rectangle? layoutBounds = null) : base(id)
        {
            Position = position;
            Player = player;
            Font = font;
            Pixel = pixel;
            PortraitTexture = portraitTexture;
            IsAlly = isAlly;
            _visuallyDefeated = player.Health <= 0;
            _visibleHealth = player.Health;
            _displayHealth = player.Health;

            CardBounds = layoutBounds ?? new Rectangle(
                (int)position.X,
                (int)position.Y,
                UIConfig.Int("cardWidth"),
                UIConfig.Int("cardHeight")
            );

            InnerCardBounds = new Rectangle(
                CardBounds.X + UIConfig.Int("innerBorderMargin"),
                CardBounds.Y + UIConfig.Int("innerBorderMargin"),
                CardBounds.Width - 2 * UIConfig.Int("innerBorderMargin"),
                CardBounds.Height - 2 * UIConfig.Int("innerBorderMargin")
            );

#if FIDA_ANDROID
            int padding = Math.Clamp(CardBounds.Height / 12, 12, 18);
            int numberWidth = Math.Clamp(CardBounds.Width / 9, 64, 86);
            int numberHeight = Math.Clamp(CardBounds.Height - padding * 2, 68, 110);
            int numberGap = 8;
            int numberRight = CardBounds.Right - padding;
            Number1Bounds = new Rectangle(numberRight - numberWidth,
                CardBounds.Center.Y - numberHeight / 2, numberWidth, numberHeight);
            Number0Bounds = new Rectangle(Number1Bounds.Left - numberGap - numberWidth,
                Number1Bounds.Y, numberWidth, numberHeight);

            int badgeWidth = Math.Clamp(CardBounds.Width / 8, 72, 102);
            PortraitBounds = new Rectangle(CardBounds.Left + padding,
                CardBounds.Top + padding, badgeWidth, CardBounds.Height - padding * 2);
            int infoLeft = PortraitBounds.Right + 18;
            int infoRight = Number0Bounds.Left - 18;
            int infoWidth = Math.Max(80, infoRight - infoLeft);
            int nameAreaHeight = Math.Clamp(CardBounds.Height / 3, 44, 66);
            _nameCenter = new Vector2(infoLeft + infoWidth / 2f,
                CardBounds.Top + padding + nameAreaHeight / 2f);
            NameLabel = new UILabel(0, player.Name, _nameCenter, font)
            {
                MaximumWidth = infoWidth,
                MaximumHeight = nameAreaHeight,
                MinimumScale = 0.48f
            };

            _healthCenter = new Vector2(infoLeft + infoWidth / 2f,
                CardBounds.Top + padding + nameAreaHeight + 17);
            HealthBounds = new Rectangle(infoLeft, (int)_healthCenter.Y + 22,
                infoWidth, 10);
            _skillStripBounds = new Rectangle(infoLeft, HealthBounds.Bottom + 8,
                infoWidth, Math.Max(28, CardBounds.Bottom - padding - HealthBounds.Bottom - 8));

            Number0Button = new UIAnimatedButton(10, Number0Bounds,
                player.Numbers.Length > 0 ? player.Numbers[0].ToString() : "1",
                pixel, font) { ParentDrawOnly = true, DarkTileStyle = true };
            HealthLabel = new UILabel(11, $"HP  {player.Health} / {player.MaxHealth}", _healthCenter, font)
            {
                MaximumWidth = infoWidth,
                MaximumHeight = 34,
                MinimumScale = 0.48f
            };
            Number1Button = new UIAnimatedButton(12, Number1Bounds,
                player.Numbers.Length > 1 ? player.Numbers[1].ToString() : "1",
                pixel, font) { ParentDrawOnly = true, DarkTileStyle = true };
#else
            Number0Bounds = UIBattleLayout.Number(CardBounds, 0);
            Number1Bounds = UIBattleLayout.Number(CardBounds, 1);
            int portraitHeight = (int)(CardBounds.Height * 0.36f);
            int portraitWidth = portraitHeight;
            PortraitBounds = new Rectangle(CardBounds.Center.X - portraitWidth / 2,
                CardBounds.Top, portraitWidth, portraitHeight);
            _nameCenter = new Vector2(CardBounds.Center.X,
                CardBounds.Top + CardBounds.Height * 0.40f);
            NameLabel = new UILabel(0, player.Name, _nameCenter, font)
            {
                MaximumWidth = CardBounds.Width - 8,
                MaximumHeight = CardBounds.Height * 0.075f,
                MinimumScale = 0.5f
            };
            HealthBounds = new Rectangle(CardBounds.Left + CardBounds.Width / 5,
                CardBounds.Top + (int)(CardBounds.Height * 0.48f), CardBounds.Width * 2 / 5,
                Math.Max(3, CardBounds.Height / 50));
            _healthCenter = new Vector2(HealthBounds.Right + CardBounds.Width * 0.12f, HealthBounds.Center.Y);
            HealthLabel = new UILabel(11, $"{player.Health}/{player.MaxHealth}", _healthCenter, font)
            {
                MaximumWidth = CardBounds.Width * 0.23f,
                MaximumHeight = CardBounds.Height * 0.065f,
                MinimumScale = 0.5f
            };
            _skillStripBounds = new Rectangle(CardBounds.Left, CardBounds.Top,
                CardBounds.Width, Math.Max(1, Number0Bounds.Top - CardBounds.Top));
            Number0Button = new UIAnimatedButton(10, Number0Bounds,
                player.Numbers.Length > 0 ? player.Numbers[0].ToString() : "1",
                pixel, font) { ParentDrawOnly = true, DarkTileStyle = true };
            Number1Button = new UIAnimatedButton(12, Number1Bounds,
                player.Numbers.Length > 1 ? player.Numbers[1].ToString() : "1",
               pixel, font) { ParentDrawOnly = true, DarkTileStyle = true };
#endif

            // 直接监听 Player 的数字变化，UI 自动刷新
            player.NumberChanged += (p, idx, oldVal, newVal) => UpdateNumbers();
            player.HealthChanged += (p) => UpdateHealth();
        }

        // ---- 鼠标交互 ----

        public override void Update(MouseState mouseState)
        {
            bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
            var mousePoint = new Point(mouseState.X, mouseState.Y);
            _pointerOverSkills = _skillStripBounds.Contains(mousePoint);
#if FIDA_ANDROID
            if (mouseDown && !_mouseWasDown)
            {
                _showSkillDetails = false;
                _skillTouchStarted = _pointerOverSkills && Player.Skills.Count > 0;
                _skillLongPressed = false;
                _skillHoldSeconds = 0f;
            }
            if (mouseDown && !_pointerOverSkills)
                _skillTouchStarted = false;
            if (!mouseDown && _mouseWasDown)
            {
                if (_skillLongPressed)
                    _clickStartedOnThis = false;
                _skillTouchStarted = false;
                _skillHoldSeconds = 0f;
            }
#endif

            bool shouldBeEnabled = EnabledChecker?.Invoke() ?? false;

            if (!shouldBeEnabled)
            {
                IsEnabled = false;
                IsHovered = false;
                _isPressed = false;
                _clickStartedOnThis = false;
                _mouseWasDown = mouseDown;
            }
            else
            {
                IsEnabled = true;
                IsHovered = CardBounds.Contains(mousePoint) || IsFocused;

                if (IsHovered && mouseDown && !_mouseWasDown)
                {
                    _isPressed = true;
                    _clickStartedOnThis = true;
                }

                if (!mouseDown && _mouseWasDown)
                {
                    _isPressed = false;
                }

                if (!IsHovered && mouseDown)
                {
                    _isPressed = false;
                    _clickStartedOnThis = false;
                }

                _mouseWasDown = mouseDown;
            }

        }

        public bool IsClicked()
        {
            if (!IsEnabled) return false;
            bool clicked = _clickStartedOnThis && !_isPressed && IsHovered;
            if (clicked)
            {
                _clickStartedOnThis = false;
                return true;
            }
            return false;
        }

        public bool IsPressed()
        {
            return _isPressed;
        }

        public override void Update(GameTime gameTime)
        {
            IsCurrentTurn = CurrentTurnChecker?.Invoke() ?? IsCurrentTurn;
            float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _displayHealth = UIMotion.Approach(_displayHealth, _visibleHealth, seconds, 9f);
            _healthChangeAge += seconds;
            if (UIMotion.Reduced) { _arrivalProgress = 1; _deathReveal = 1; }
#if FIDA_ANDROID
            if (_skillTouchStarted && _mouseWasDown && !_skillLongPressed)
            {
                _skillHoldSeconds += seconds;
                if (_skillHoldSeconds >= 0.55f)
                {
                    _skillLongPressed = true;
                    _showSkillDetails = true;
                    _clickStartedOnThis = false;
                    _isPressed = false;
                }
            }
#endif
            if (_visuallyDefeated && _deathReveal < 1f)
                _deathReveal = Math.Min(1f, _deathReveal + seconds / 0.55f);
            if (_arrivalProgress < 1f)
                _arrivalProgress = Math.Min(1f, _arrivalProgress + seconds / 0.48f);
        }

        public void BeginReinforcementEntrance() => _arrivalProgress = 0f;

        private void SetVisibleHealth(int health)
        {
            if (health != _visibleHealth)
            {
                _healthDelta = health - _visibleHealth;
                _healthChangeAge = 0;
            }
#if FIDA_ANDROID
            HealthLabel.Update($"HP  {health} / {Player.MaxHealth}");
#else
            HealthLabel.Update($"{health}/{Player.MaxHealth}");
#endif
            _visibleHealth = health;
            bool defeated = health <= 0;
            if (defeated && !_visuallyDefeated) _deathReveal = 0f;
            _visuallyDefeated = defeated;
        }

        public void UpdateNumbers()
        {
            if (DeferNumberVisual)
            {
                _deferredNumbers = (int[])Player.Numbers.Clone();
                return;
            }
            if (Player.Numbers.Length > 0)
                Number0Button.Update(Player.Numbers[0].ToString());
            if (Player.Numbers.Length > 1)
                Number1Button.Update(Player.Numbers[1].ToString());
        }

        public void UpdateHealth()
        {
            if (DeferHealthVisual)
            {
                _deferredHealth = Player.Health;
                return;
            }
            SetVisibleHealth(Player.Health);
        }

        public void BeginDeferredNumberVisual()
        {
            DeferNumberVisual = true;
            _deferredNumbers = null;
        }

        public void CommitDeferredNumberVisual()
        {
            DeferNumberVisual = false;
            int[] numbers = _deferredNumbers ?? Player.Numbers;
            if (numbers.Length > 0) Number0Button.Update(numbers[0].ToString());
            if (numbers.Length > 1) Number1Button.Update(numbers[1].ToString());
            _deferredNumbers = null;
        }

        public void CommitDeferredHealthVisual()
        {
            DeferHealthVisual = false;
            SetVisibleHealth(_deferredHealth ?? Player.Health);
            _deferredHealth = null;
        }

        public void RefreshButtons()
        {
            Number0Button.RefreshState();
            Number1Button.RefreshState();
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime) => DrawColdUnit(sb, gameTime);

        private void DrawColdUnit(SpriteBatch sb, GameTime time)
        {
            float scale = UIDesktopLayout.LayoutScale(sb.GraphicsDevice.Viewport);
            double clock = time.TotalGameTime.TotalSeconds;
            float phase = Player.PlayerId * 1.71f;
            bool moving = !UIMotion.Reduced && !_visuallyDefeated;
            float breath = moving ? MathF.Sin((float)clock * 1.5f + phase) : 0;
            float sway = moving ? MathF.Sin((float)clock * 0.8f + phase) : 0;
            // Only the artwork moves. Buttons, labels and their hit rectangles stay fixed.
            Vector2 offset = UIMotion.Reduced ? Vector2.Zero : VisualOffset * scale;
            float portraitScale = UIMotion.Reduced ? 1 : VisualScale;
            var art = PortraitBounds;
            float enter = UIMotion.Reduced ? 1 : MathHelper.SmoothStep(0, 1, _arrivalProgress);
            Color tint = (_visuallyDefeated ? Color.Lerp(Color.White, UITheme.InkMuted * 0.35f, _deathReveal) : Color.White) * enter;
            if (PortraitTexture != null)
            {
                Rectangle source = PortraitSource ?? PortraitTexture.Bounds;
                float fit = Math.Min(art.Width / (float)source.Width, art.Height / (float)source.Height);
                sb.Draw(PortraitTexture, new Vector2(art.Center.X, art.Bottom) + offset +
                    new Vector2(sway * 1.5f * scale, -breath * 1.5f * scale), source, tint,
                    sway * 0.008f, new Vector2(source.Width / 2f, source.Height),
                    new Vector2(fit * portraitScale, fit * portraitScale * (1 + breath * 0.009f)),
                    SpriteEffects.None, 0);
            }
            NameLabel.SetColor(IsCurrentTurn || IsPresentingAction ? UITheme.Gold : UITheme.Ink);
            NameLabel.DrawAt(sb, time, _nameCenter, scale);
            sb.Draw(Pixel, HealthBounds, UITheme.Line * 0.55f);
            float ratio = Player.MaxHealth <= 0 ? 0 : MathHelper.Clamp(_displayHealth / Player.MaxHealth, 0, 1);
            if (ratio > 0)
                sb.Draw(Pixel, new Rectangle(HealthBounds.X, HealthBounds.Y,
                    Math.Max(1, (int)(HealthBounds.Width * ratio)), HealthBounds.Height),
                    IsAlly ? UITheme.Gold : UITheme.Ink);
            HealthLabel.SetColor(UITheme.InkMuted);
            HealthLabel.DrawAt(sb, time, _healthCenter, scale);
            if (Player.Skills.Count > 0)
            {
                string skills = string.Join("  /  ", Player.Skills.ConvertAll(skill => UISkillIconCatalog.Glyph(skill.Type)));
#if FIDA_ANDROID
                DrawCompactText(sb, skills, _skillStripBounds.Center.ToVector2(), UITheme.InkMuted,
                    0.50f, _skillStripBounds.Width);
#else
                DrawCompactText(sb, skills, new Vector2(CardBounds.Center.X,
                    (HealthBounds.Bottom + Number0Bounds.Top) / 2f), UITheme.InkMuted,
                    0.23f * scale, CardBounds.Width - 12);
#endif
            }
            if (IsCurrentTurn || IsPresentingAction)
                sb.Draw(Pixel, new Rectangle(CardBounds.Center.X - Math.Max(4, (int)(8 * scale)),
                    PortraitBounds.Top - Math.Max(5, (int)(10 * scale)), Math.Max(8, (int)(16 * scale)), Math.Max(2, (int)(2 * scale))), UITheme.Gold);
            if (IsHovered || IsFocused || SelectionStrength > 0)
                UIUtils.DrawRectangleOutlines(sb, CardBounds, UITheme.InkMuted, 1, Pixel);
            Number0Button.DrawAt(sb, time, Number0Bounds);
            Number1Button.DrawAt(sb, time, Number1Bounds);
            if (ImpactStrength > 0 && !UIMotion.Reduced)
                sb.Draw(Pixel, new Rectangle(HealthBounds.X, HealthBounds.Y - 2,
                    HealthBounds.Width, Math.Max(2, HealthBounds.Height + 4)), UITheme.Ink * (ImpactStrength * 0.5f));
            if (_visuallyDefeated)
            {
                sb.Draw(Pixel, Number0Bounds, UITheme.BgTop * (0.65f * _deathReveal));
                sb.Draw(Pixel, Number1Bounds, UITheme.BgTop * (0.65f * _deathReveal));
                DrawCompactText(sb, "DOWN", new Vector2(CardBounds.Center.X, Number0Bounds.Center.Y),
                    UITheme.InkMuted, 0.35f * scale, CardBounds.Width);
            }
            if (!UIMotion.Reduced && _healthChangeAge < 0.7f && _healthDelta != 0)
                DrawCompactText(sb, _healthDelta > 0 ? $"+{_healthDelta}" : _healthDelta.ToString(),
                    new Vector2(PortraitBounds.Right + 10 * scale, PortraitBounds.Top - _healthChangeAge * 18 * scale),
                    UITheme.Ink * (1 - _healthChangeAge / 0.7f), 0.5f * scale, CardBounds.Width / 3);
            // Skill information is secondary and available over the portrait/name, away from number clicks.
#if FIDA_ANDROID
            if (_showSkillDetails && Player.Skills.Count > 0)
#else
            if (_pointerOverSkills && Player.Skills.Count > 0)
#endif
            {
                int height = Math.Min(UIConfig.ScaleDesktopMetric(220),
                    UIConfig.ScaleDesktopMetric(66 + Player.Skills.Count * 28));
                int y = Math.Max(8, CardBounds.Top - height - 8);
                if (y + height > CardBounds.Top) y = CardBounds.Bottom + 8;
                DrawSkillDetails(sb, new Rectangle(CardBounds.Left, y, CardBounds.Width, height), scale);
            }
        }

        private void DrawSkillDetails(SpriteBatch sb, Rectangle portraitBounds, float scale)
        {
            sb.Draw(Pixel, portraitBounds, UITheme.PanelDark * 0.96f);
#if FIDA_ANDROID
            UIUtils.DrawRectangleOutlines(sb, portraitBounds, UITheme.Gold, 2, Pixel);
            sb.Draw(Pixel, new Rectangle(portraitBounds.Left, portraitBounds.Top, 7,
                portraitBounds.Height), IsAlly ? UITheme.AccentGreen : UITheme.Gold);
#else
            UIUtils.DrawEngravedFrame(sb, portraitBounds, Pixel, UITheme.GoldBright,
                UITheme.PanelDark, 2, 6, 14, false);
#endif

            DrawCompactText(sb, "SKILLS", new Vector2(portraitBounds.Center.X,
                portraitBounds.Top + 28 * scale), UITheme.GoldBright,
#if FIDA_ANDROID
                0.62f * scale,
#else
                0.42f * scale,
#endif
                portraitBounds.Width - 20f);

            float rowHeight = System.Math.Max(
#if FIDA_ANDROID
                36f, 42f * scale);
            float y = portraitBounds.Top + 60f * scale;
#else
                20f, 27f * scale);
            float y = portraitBounds.Top + 54f * scale;
#endif
            int maximumRows = System.Math.Max(1,
                (int)((portraitBounds.Bottom - y - 8f) / rowHeight));
            int shown = System.Math.Min(maximumRows, Player.Skills.Count);
            for (int i = 0; i < shown; i++)
            {
                Skill skill = Player.Skills[i];
                string line = $"{UISkillIconCatalog.Glyph(skill.Type)}  {GameText.SkillName(skill.Type)}  " +
                    $"[{CampaignSkillBalance.Cost(skill.Type)} SP]";
                DrawCompactText(sb, line, new Vector2(portraitBounds.Center.X, y + rowHeight / 2f),
                    UITheme.Ink,
#if FIDA_ANDROID
                    0.62f * scale,
#else
                    0.44f * scale,
#endif
                    portraitBounds.Width - 28f);
                y += rowHeight;
            }
            if (shown < Player.Skills.Count)
                DrawCompactText(sb, $"+{Player.Skills.Count - shown} MORE",
                    new Vector2(portraitBounds.Center.X, portraitBounds.Bottom - 13f * scale),
                    UITheme.GoldBright, 0.40f * scale, portraitBounds.Width - 18f);
        }

        private void DrawCompactText(SpriteBatch sb, string text, Vector2 center,
            Color color, float preferredScale, float maximumWidth)
        {
            UIUtils.DrawCenteredFit(sb, Font, text, center, maximumWidth,
                preferredScale, color);
        }
    }
}
