#nullable enable
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
        private readonly SpriteFont _captionFont;
        public Texture2D Pixel { get; set; }

        public Texture2D? PortraitTexture { get; set; }
        public Rectangle? PortraitSource { get; set; }
        public bool IsCurrentTurn { get; set; }
        public Func<bool>? CurrentTurnChecker { get; set; }
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
        public Func<bool>? EnabledChecker { get; set; }

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
        private int[]? _deferredNumbers;
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
        /// <summary>Strip holding name and skill icons: they share one row.</summary>
        private Rectangle _nameRowBounds;
        /// <summary>Side length the skill icons were laid out for.</summary>
        private int _skillIconSide;
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

        public UIPlayer(int id, Player player, Vector2 position, SpriteFont font, Texture2D pixel, Texture2D? portraitTexture = null, bool isAlly = false, Rectangle? layoutBounds = null, SpriteFont? labelFont = null, SpriteFont? captionFont = null) : base(id)
        {
            Position = position;
            Player = player;
            Font = font;
            _captionFont = captionFont ?? font;
            SpriteFont nameFont = labelFont ?? font;
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
            // Portrait layout: portrait, name/skills row and number tiles share a
            // single band. The thick health bar is pinned to the bottom of the
            // middle column only, so it never runs under the portrait or the
            // number tiles.
            int padding = Math.Clamp(CardBounds.Height / 12, 10, 16);
            int barHeight = Math.Clamp(CardBounds.Height / 8, 14, 22);
            int numberWidth = Math.Clamp(CardBounds.Width / 10, 56, 78);
            int numberGap = 8;
            int numberRight = CardBounds.Right - padding;

            int bandTop = CardBounds.Top + padding;
            int bandBottom = CardBounds.Bottom - padding;
            int bandHeight = Math.Max(44, bandBottom - bandTop);

            Number1Bounds = new Rectangle(numberRight - numberWidth, bandTop, numberWidth, bandHeight);
            Number0Bounds = new Rectangle(Number1Bounds.Left - numberGap - numberWidth,
                bandTop, numberWidth, bandHeight);

            PortraitBounds = new Rectangle(CardBounds.Left + padding, bandTop, bandHeight, bandHeight);

            int infoLeft = PortraitBounds.Right + 16;
            int infoRight = Number0Bounds.Left - 16;
            int infoWidth = Math.Max(80, infoRight - infoLeft);

            int shownSkills = Math.Min(4, player.Skills.Count);
            _skillIconSide = Math.Clamp(bandHeight / 4, 18, 30);
            int skillsWidth = shownSkills > 0 ? shownSkills * (_skillIconSide + 8) - 8 : 0;
            int nameRowHeight = Math.Clamp(bandHeight / 3, 30, 46);
            int nameWidth = Math.Max(60, infoWidth - skillsWidth - (shownSkills > 0 ? 12 : 0));

            _nameRowBounds = new Rectangle(infoLeft, bandTop, nameWidth, nameRowHeight);
            _nameCenter = new Vector2(infoLeft + nameWidth / 2f, bandTop + nameRowHeight / 2f);
            NameLabel = new UILabel(0, player.Name, _nameCenter, nameFont)
            {
                MaximumWidth = nameWidth,
                MaximumHeight = nameRowHeight,
                MinimumScale = 0.48f
            };
            _skillStripBounds = new Rectangle(infoLeft + nameWidth + 12, bandTop, skillsWidth, nameRowHeight);

            HealthBounds = new Rectangle(infoLeft, bandBottom - barHeight, infoWidth, barHeight);
            _healthCenter = new Vector2(infoLeft + infoWidth / 2f,
                (bandTop + nameRowHeight + (bandBottom - barHeight)) / 2f);

            Number0Button = new UIAnimatedButton(10, Number0Bounds,
                player.Numbers.Length > 0 ? player.Numbers[0].ToString() : "1",
                pixel, font) { ParentDrawOnly = true, DarkTileStyle = true };
            HealthLabel = new UILabel(11, $"HP  {player.Health} / {player.MaxHealth}", _healthCenter, _captionFont)
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
            var viewport = pixel.GraphicsDevice.Viewport;
            float nameLine = UITypography.LineHeight(UITextRole.Name, viewport.Width, viewport.Height);
            float healthLine = UITypography.LineHeight(UITextRole.Caption, viewport.Width, viewport.Height);
            int gap = Math.Max(3, UIConfig.ScaleDesktopMetric(5));
            int infoLeft = Number0Bounds.Left + gap * 2;
            int infoWidth = Number1Bounds.Right - Number0Bounds.Left - gap * 4;
            float nameHeight = nameLine;
            int upperHeight = Number0Bounds.Top - CardBounds.Top;
            int portraitHeight = Math.Max(24, upperHeight - (int)(nameHeight + healthLine + gap * 3));
            PortraitBounds = new Rectangle(CardBounds.Center.X - portraitHeight / 2, CardBounds.Top,
                portraitHeight, portraitHeight);
            _nameCenter = new Vector2(CardBounds.Center.X, PortraitBounds.Bottom + gap + nameHeight / 2);
            string displayName = player.Name;
            if (nameFont.MeasureString(displayName).X * UITypography.Scale(nameFont, UITextRole.Name, viewport) > infoWidth)
                displayName = displayName.Replace(" ", "\n");
            NameLabel = new UILabel(0, displayName, _nameCenter, nameFont)
            {
                MaximumWidth = infoWidth,
                MaximumHeight = nameHeight,
                TypographyRole = UITextRole.Name
            };
            int healthY = (int)(_nameCenter.Y + nameHeight / 2 + gap + healthLine / 2);
            HealthBounds = new Rectangle(infoLeft, healthY - 3, Math.Max(30, infoWidth * 62 / 100), Math.Max(5, UIConfig.ScaleDesktopMetric(8)));
            _healthCenter = new Vector2(HealthBounds.Right + gap + (infoWidth - HealthBounds.Width - gap) / 2, healthY);
            HealthLabel = new UILabel(11, $"{player.Health}/{Player.MaxHealth}", _healthCenter, _captionFont)
            {
                MaximumWidth = infoWidth - HealthBounds.Width - gap,
                MaximumHeight = healthLine,
                TypographyRole = UITextRole.Caption
            };
            _skillStripBounds = new Rectangle(CardBounds.Left, CardBounds.Top, CardBounds.Width, Number0Bounds.Top - CardBounds.Top);
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
            var infoSheet = new Rectangle(Number0Bounds.Left - 3, PortraitBounds.Bottom - 3,
                Number1Bounds.Right - Number0Bounds.Left + 6, Math.Max(1, Number0Bounds.Top - PortraitBounds.Bottom));
#if FIDA_ANDROID
            infoSheet = new Rectangle(CardBounds.Left, CardBounds.Top, CardBounds.Width, CardBounds.Height);
#endif
            UIPaper.Sheet(sb, infoSheet, UITheme.PanelDark, 2, 12);
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
            if (IsCurrentTurn || IsPresentingAction)
#if FIDA_ANDROID
                UIPaper.Sheet(sb, _nameRowBounds, UITheme.Gold, 1, 5);
            NameLabel.SetColor(IsCurrentTurn || IsPresentingAction ? UIPaper.Ink : UITheme.Ink);
#else
                sb.Draw(Pixel, new Rectangle(infoSheet.Left + 8, infoSheet.Top + 5,
                    3, Math.Max(1, infoSheet.Height - 10)), UITheme.Gold);
            NameLabel.SetColor(UITheme.Ink);
#endif
            NameLabel.DrawAt(sb, time, _nameCenter, scale);
            sb.Draw(Pixel, HealthBounds, UITheme.Line * 0.55f);
            float ratio = Player.MaxHealth <= 0 ? 0 : MathHelper.Clamp(_displayHealth / Player.MaxHealth, 0, 1);
            if (ratio > 0)
                sb.Draw(Pixel, new Rectangle(HealthBounds.X, HealthBounds.Y,
                    Math.Max(1, (int)(HealthBounds.Width * ratio)), HealthBounds.Height),
                    new Color(196, 113, 106));
            HealthLabel.SetColor(UITheme.InkMuted);
            HealthLabel.DrawAt(sb, time, _healthCenter, scale);
#if FIDA_ANDROID
            if (Player.Skills.Count > 0)
            {
                int side = _skillIconSide;
                int shown = Math.Min(4, Player.Skills.Count), total = shown * (side + 8) - 8;
                Vector2 center = _skillStripBounds.Center.ToVector2();
                for (int i = 0; i < shown; i++)
                    Campaign.UISkillArt.Draw(sb, Player.Skills[i].Type, new Rectangle((int)center.X - total / 2 + i * (side + 8),
                        (int)center.Y - side / 2, side, side), UITheme.Ink);
            }
#endif
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
                int line = (int)UITypography.LineHeight(UITextRole.Small, sb.GraphicsDevice.Viewport.Width, sb.GraphicsDevice.Viewport.Height) + 6;
                int height = Math.Min(sb.GraphicsDevice.Viewport.Height / 2, 48 + Player.Skills.Count * line);
                int y = Math.Max(8, CardBounds.Top - height - 8);
                if (y + height > CardBounds.Top) y = CardBounds.Bottom + 8;
                int width = Math.Min(sb.GraphicsDevice.Viewport.Width - 24, Math.Max(CardBounds.Width, 320));
                int x = Math.Clamp(CardBounds.Center.X - width / 2, 12, sb.GraphicsDevice.Viewport.Width - width - 12);
                y = Math.Clamp(y, 8, sb.GraphicsDevice.Viewport.Height - height - 8);
                DrawSkillDetails(sb, new Rectangle(x, y, width, height), scale);
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

            float rowHeight = UITypography.LineHeight(UITextRole.Small, sb.GraphicsDevice.Viewport.Width, sb.GraphicsDevice.Viewport.Height) + 6;
            float y = portraitBounds.Top + 38;
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
            UIUtils.DrawCenteredFit(sb, _captionFont, text, center, maximumWidth,
                Math.Max(preferredScale, UITypography.Scale(_captionFont, UITextRole.Small, sb.GraphicsDevice.Viewport)), color);
        }
    }
}
