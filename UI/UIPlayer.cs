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
        public bool IsAlly { get; }

        /// <summary>Displays a pulsing frame while an automated controller is deciding.</summary>
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
        private int[] _deferredNumbers;
        private float _deathReveal = 1f;
        private bool _visuallyDefeated;
        private float _arrivalProgress = 1f;
        public bool DeferNumberVisual { get; private set; }
        private bool _pointerOverCard;
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

        private UIButtonState CardState
        {
            get
            {
                if (!IsEnabled) return UIButtonState.Disabled;
                if (_isPressed) return UIButtonState.Pressed;
                if (IsHovered) return UIButtonState.Hovered;
                return UIButtonState.Normal;
            }
        }

        // 配色统一取自 UITheme（暖墨 + 羊皮纸主题）

        public UIPlayer(int id, Player player, Vector2 position, SpriteFont font, Texture2D pixel, Texture2D portraitTexture = null, bool isAlly = false) : base(id)
        {
            Position = position;
            Player = player;
            Font = font;
            Pixel = pixel;
            PortraitTexture = portraitTexture;
            IsAlly = isAlly;
            _visuallyDefeated = player.Health <= 0;
            _visibleHealth = player.Health;

            float nameHeight = font.MeasureString(UIFontGuard.Sanitize(font, player.Name)).Y;

            CardBounds = new Rectangle(
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

            int verticalSpacing = (InnerCardBounds.Height - UIConfig.Int("portraitSize") - UIConfig.Int("nameHeight") - UIConfig.Int("bottomElementHeight")) / 4;

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
            int pad = UIConfig.ScaleDesktopMetric(18);
            int badgeSize = UIConfig.Int("portraitSize");
            PortraitBounds = new Rectangle(CardBounds.Left + pad,
                CardBounds.Top + UIConfig.ScaleDesktopMetric(82),
                badgeSize, badgeSize);
            _nameCenter = new Vector2(CardBounds.Center.X,
                CardBounds.Top + UIConfig.ScaleDesktopMetric(34));
            NameLabel = new UILabel(0, player.Name, _nameCenter, font)
            {
                MaximumWidth = CardBounds.Width - pad * 2,
                MaximumHeight = UIConfig.ScaleDesktopMetric(34),
                MinimumScale = 0.5f
            };

            int infoLeft = PortraitBounds.Right + UIConfig.ScaleDesktopMetric(16);
            int infoWidth = CardBounds.Right - pad - infoLeft;
            HealthBounds = new Rectangle(infoLeft, CardBounds.Top + UIConfig.ScaleDesktopMetric(105),
                infoWidth, UIConfig.ScaleDesktopMetric(38));
            _healthCenter = HealthBounds.Center.ToVector2();
            HealthLabel = new UILabel(11, player.Health.ToString(), _healthCenter, font)
            {
                MaximumWidth = infoWidth - 16,
                MaximumHeight = UIConfig.ScaleDesktopMetric(29),
                MinimumScale = 0.5f
            };
            _skillStripBounds = new Rectangle(infoLeft, CardBounds.Top + UIConfig.ScaleDesktopMetric(150),
                infoWidth, UIConfig.ScaleDesktopMetric(34));

            int numberWidth = UIConfig.ScaleDesktopMetric(56);
            int numberHeight = UIConfig.ScaleDesktopMetric(54);
            int bottomY = CardBounds.Bottom - numberHeight - UIConfig.ScaleDesktopMetric(16);
            Number0Bounds = new Rectangle(CardBounds.Left + pad, bottomY,
                numberWidth, numberHeight);
            Number0Button = new UIAnimatedButton(10, Number0Bounds,
                player.Numbers.Length > 0 ? player.Numbers[0].ToString() : "1",
                pixel, font) { ParentDrawOnly = true };
            Number1Bounds = new Rectangle(CardBounds.Right - pad - numberWidth,
                bottomY, numberWidth, numberHeight);
            Number1Button = new UIAnimatedButton(12, Number1Bounds,
                player.Numbers.Length > 1 ? player.Numbers[1].ToString() : "1",
               pixel, font) { ParentDrawOnly = true };
#endif

            // 直接监听 Player 的数字变化，UI 自动刷新
            player.NumberChanged += (p, idx, oldVal, newVal) => UpdateNumbers();
            player.HealthChanged += (p) => UpdateHealth();
        }

        // ---- 状态驱动渲染 ----

        private Color GetCardBackColor()
        {
            return CardState switch
            {
                UIButtonState.Pressed => UITheme.PanelDark,
                _ => UITheme.PanelLight
            };
        }

        private Color GetCardForeColor()
        {
            // 文字/内框/头像用深墨，在浅底与按压深底上都清晰
            return UITheme.Ink;
        }

        private Color GetCardBorderColor()
        {
            return CardState switch
            {
                UIButtonState.Hovered => UITheme.GoldBright,
                _ => UITheme.Gold
            };
        }

        private int GetOuterBorderThickness()
        {
            return CardState switch
            {
                UIButtonState.Hovered => 4,
                UIButtonState.Pressed => 2,
                _ => UIConfig.Int("cardBorderThickness")
            };
        }

        private bool HasDoubleBorder()
        {
            return CardState == UIButtonState.Pressed;
        }

        // ---- 鼠标交互 ----

        public override void Update(MouseState mouseState)
        {
            bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
            var mousePoint = new Point(mouseState.X, mouseState.Y);
            _pointerOverCard = CardBounds.Contains(mousePoint);
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
            float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
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
#if FIDA_ANDROID
            HealthLabel.Update($"HP  {health} / {Player.MaxHealth}");
#else
            HealthLabel.Update(health.ToString());
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

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            Color backColor = GetCardBackColor();
            Color foreColor = GetCardForeColor();
            int outerThickness = GetOuterBorderThickness();

            float interactionScale = CardState switch
            {
                UIButtonState.Hovered => 1.035f,
                UIButtonState.Pressed => 0.985f,
                _ => 1f
            };
            float layoutScale = UIDesktopLayout.LayoutScale(Pixel.GraphicsDevice.Viewport);
            Vector2 interactionOffset = CardState == UIButtonState.Hovered
                ? new Vector2(0f, -9f * layoutScale)
                : Vector2.Zero;
            float drawScale = VisualScale * interactionScale;
            Vector2 drawOffset = VisualOffset + interactionOffset;
            if (_arrivalProgress < 1f)
            {
                float entrance = MathHelper.SmoothStep(0f, 1f, _arrivalProgress);
                drawOffset += new Vector2(IsAlly ? -44f : 44f, 0f) * layoutScale * (1f - entrance);
                drawScale *= 0.94f + 0.06f * entrance;
            }
            Vector2 pivot = CardBounds.Center.ToVector2();

            Rectangle cardBounds = TransformRect(CardBounds, pivot, drawScale, drawOffset);
            Rectangle portraitBounds = TransformRect(PortraitBounds, pivot, drawScale, drawOffset);
            Rectangle number0Bounds = TransformRect(Number0Bounds, pivot, drawScale, drawOffset);
            Rectangle number1Bounds = TransformRect(Number1Bounds, pivot, drawScale, drawOffset);
            Rectangle healthBounds = TransformRect(HealthBounds, pivot, drawScale, drawOffset);
            Vector2 nameCenter = TransformPoint(_nameCenter, pivot, drawScale, drawOffset);
            Vector2 healthCenter = TransformPoint(_healthCenter, pivot, drawScale, drawOffset);

            if (IsPresentingAction)
            {
                float pulse = 0.5f + 0.5f * (float)Math.Sin(
                    gameTime.TotalGameTime.TotalSeconds * 7.0);
                int expansion = UIConfig.ScaleDesktopMetric(5) +
                    (int)(pulse * UIConfig.ScaleDesktopMetric(5));
                var cueBounds = new Rectangle(
                    cardBounds.X - expansion,
                    cardBounds.Y - expansion,
                    cardBounds.Width + expansion * 2,
                    cardBounds.Height + expansion * 2);
                UIUtils.DrawRectangleOutlines(sb, cueBounds,
                    Color.Lerp(UITheme.Gold, UITheme.GoldBright, pulse),
                    UIConfig.ScaleDesktopMetric(2) + (int)(pulse * UIConfig.ScaleDesktopMetric(2)), Pixel);
            }

            if (SelectionStrength > 0f)
            {
                int expansion = UIConfig.ScaleDesktopMetric(8) +
                    (int)(SelectionStrength * UIConfig.ScaleDesktopMetric(5));
                var selectedBounds = new Rectangle(cardBounds.X - expansion, cardBounds.Y - expansion,
                    cardBounds.Width + expansion * 2, cardBounds.Height + expansion * 2);
                UIUtils.DrawGlowFrame(sb, selectedBounds, Pixel,
                    UITheme.AccentGreen, 4, SelectionStrength);
            }

            // 绘制外框背景
            sb.Draw(Pixel, cardBounds, backColor);

            // 绘制外框边框
#if FIDA_ANDROID
            Color identityAccent = IsAlly ? UITheme.AccentGreen : UITheme.Gold;
            UIUtils.DrawRectangleOutlines(sb, cardBounds,
                CardState == UIButtonState.Hovered ? identityAccent : UITheme.Ink,
                CardState == UIButtonState.Hovered ? 2 : 1, Pixel);
            sb.Draw(Pixel, portraitBounds, UITheme.PanelDark);
            DrawCompactText(sb, Player.PlayerId.ToString("00"),
                portraitBounds.Center.ToVector2(), identityAccent,
                0.78f * drawScale, portraitBounds.Width - 8);
            int dividerWidth = Math.Max(1, (int)(2 * drawScale));
            sb.Draw(Pixel, new Rectangle(portraitBounds.Right + (int)(8 * drawScale),
                cardBounds.Top + (int)(16 * drawScale), dividerWidth,
                cardBounds.Height - (int)(32 * drawScale)), UITheme.Ink * 0.25f);
            sb.Draw(Pixel, new Rectangle(number0Bounds.Left - (int)(10 * drawScale),
                cardBounds.Top + (int)(16 * drawScale), dividerWidth,
                cardBounds.Height - (int)(32 * drawScale)), UITheme.Ink * 0.25f);
            DrawCompactHealthBar(sb, healthBounds);
#else
            Color border = CardState == UIButtonState.Hovered || IsPresentingAction
                ? UITheme.Gold : UITheme.Ink;
            UIUtils.DrawRectangleOutlines(sb, cardBounds, border,
                CardState == UIButtonState.Hovered ? 2 : 1, Pixel);
            Color identityAccent = IsAlly ? UITheme.AccentGreen : UITheme.Gold;
            sb.Draw(Pixel, new Rectangle(cardBounds.Left, cardBounds.Top,
                UIConfig.ScaleDesktopMetric(7), cardBounds.Height),
                identityAccent);
            sb.Draw(Pixel, new Rectangle(cardBounds.Left + UIConfig.ScaleDesktopMetric(18),
                cardBounds.Top + UIConfig.ScaleDesktopMetric(66),
                cardBounds.Width - UIConfig.ScaleDesktopMetric(36),
                UIConfig.ScaleDesktopMetric(1)), UITheme.Ink * 0.24f);
            sb.Draw(Pixel, portraitBounds, UITheme.PanelDark);
            DrawCompactText(sb, Player.PlayerId.ToString("00"),
                portraitBounds.Center.ToVector2(), identityAccent,
                0.78f * drawScale, portraitBounds.Width - 10);
            DrawCompactText(sb, "HP", new Vector2(healthBounds.Left + UIConfig.ScaleDesktopMetric(12),
                healthBounds.Top - UIConfig.ScaleDesktopMetric(14)), UITheme.InkMuted, 0.36f * layoutScale * drawScale,
                healthBounds.Width);
            DrawCompactHealthBar(sb, healthBounds);
#endif

            DrawSkillMedallions(sb, cardBounds, portraitBounds, nameCenter, number0Bounds, drawScale);

#if FIDA_ANDROID
            if (_showSkillDetails && Player.Skills.Count > 0)
            {
                int tooltipHeight = Math.Min(UIConfig.ScaleDesktopMetric(250),
                    UIConfig.ScaleDesktopMetric(88 + Player.Skills.Count * 42));
                int tooltipY = (sb.GraphicsDevice.Viewport.Height - tooltipHeight) / 2;
                DrawSkillDetails(sb, new Rectangle(cardBounds.Left + 20,
                    tooltipY, cardBounds.Width - 40, tooltipHeight), 1f);
            }
#else
            if (_pointerOverSkills && Player.Skills.Count > 0)
            {
                int tooltipY = cardBounds.Bottom + 8;
                int tooltipNominalHeight = UIConfig.ScaleDesktopMetric(168);
                if (tooltipY + tooltipNominalHeight > sb.GraphicsDevice.Viewport.Height - UIConfig.ScaleDesktopMetric(12))
                    tooltipY = cardBounds.Top - UIConfig.ScaleDesktopMetric(176);
                DrawSkillDetails(sb, new Rectangle(cardBounds.Left,
                    Math.Max(UIConfig.ScaleDesktopMetric(8), tooltipY), cardBounds.Width,
                    tooltipNominalHeight), drawScale * layoutScale);
            }
#endif

            // 绘制底部元素
            Number0Button.DrawAt(sb, gameTime, number0Bounds);
            HealthLabel.SetColor(foreColor);
            HealthLabel.DrawAt(sb, gameTime, healthCenter, drawScale * layoutScale);
            Number1Button.DrawAt(sb, gameTime, number1Bounds);

            // 绘制名字（Pressed 时反色）
            NameLabel.SetColor(foreColor);
            NameLabel.DrawAt(sb, gameTime, nameCenter, drawScale * layoutScale);

            if (ImpactStrength > 0f)
            {
                sb.Draw(Pixel, cardBounds, UITheme.GoldBright * (0.26f * ImpactStrength));
                UIUtils.DrawRectangleOutlines(sb, cardBounds, UITheme.GoldBright,
                    3 + (int)(ImpactStrength * 4f), Pixel);
            }

            if (_visuallyDefeated)
                DrawDefeatedState(sb, cardBounds, drawScale);
            else if (_arrivalProgress < 1f)
                DrawArrivalState(sb, cardBounds, drawScale);
        }

        private void DrawDefeatedState(SpriteBatch sb, Rectangle bounds, float scale)
        {
            float reveal = MathHelper.SmoothStep(0f, 1f, _deathReveal);
            int height = Math.Max(1, (int)(bounds.Height * reveal));
            sb.Draw(Pixel, new Rectangle(bounds.Left, bounds.Bottom - height,
                bounds.Width, height), UITheme.PanelLight * 0.90f);
            if (reveal < 0.3f) return;
            Color accent = IsAlly ? UITheme.AccentGreen : UITheme.Gold;
            sb.Draw(Pixel, new Rectangle(bounds.Left, bounds.Top,
                Math.Max(5, (int)(7f * scale)), bounds.Height), accent);
            int ruleY = bounds.Center.Y + Math.Max(18, (int)(22f * scale));
            sb.Draw(Pixel, new Rectangle(bounds.Left + 16, ruleY,
                Math.Max(1, bounds.Width - 32), Math.Max(2, (int)(3f * scale))), accent);
            DrawCompactText(sb, IsAlly ? "DOWN" : "DEFEATED",
                new Vector2(bounds.Center.X, bounds.Center.Y - 12f * scale),
                UITheme.Ink, 0.70f * scale, bounds.Width - 30f);
        }

        private void DrawArrivalState(SpriteBatch sb, Rectangle bounds, float scale)
        {
            float reveal = MathHelper.SmoothStep(0f, 1f, _arrivalProgress);
            int top = bounds.Top + Math.Max(5, (int)(8f * scale));
            sb.Draw(Pixel, new Rectangle(bounds.Left, top,
                Math.Max(1, (int)(bounds.Width * reveal)), Math.Max(3, (int)(4f * scale))),
                IsAlly ? UITheme.AccentGreen : UITheme.Gold);
            if (_arrivalProgress < 0.78f)
                DrawCompactText(sb, "INCOMING",
                    new Vector2(bounds.Center.X, bounds.Bottom - 22f * scale),
                    UITheme.Ink, 0.46f * scale, bounds.Width - 24f);
        }

        private void DrawCompactHealthBar(SpriteBatch sb, Rectangle bounds)
        {
            sb.Draw(Pixel, bounds, UITheme.PanelDark);
            float ratio = Player.MaxHealth <= 0 ? 0f :
                MathHelper.Clamp(_visibleHealth / (float)Player.MaxHealth, 0f, 1f);
            int fill = Math.Max(0, (int)(bounds.Width * ratio));
            if (fill > 0)
#if FIDA_ANDROID
                sb.Draw(Pixel, new Rectangle(bounds.Left, bounds.Top, fill, bounds.Height),
                    UITheme.AccentGreen);
#else
                sb.Draw(Pixel, new Rectangle(bounds.Left, bounds.Bottom - 6, fill, 6),
                    UITheme.AccentGreen);
#endif
        }

        private static Rectangle TransformRect(Rectangle source, Vector2 pivot, float scale, Vector2 offset)
        {
            Vector2 center = TransformPoint(source.Center.ToVector2(), pivot, scale, offset);
            int width = System.Math.Max(1, (int)(source.Width * scale));
            int height = System.Math.Max(1, (int)(source.Height * scale));
            return new Rectangle((int)(center.X - width / 2f), (int)(center.Y - height / 2f), width, height);
        }

        private static Vector2 TransformPoint(Vector2 point, Vector2 pivot, float scale, Vector2 offset) =>
            pivot + (point - pivot) * scale + offset;

        private void DrawSkillMedallions(SpriteBatch sb, Rectangle cardBounds,
            Rectangle portraitBounds, Vector2 nameCenter, Rectangle numberBounds, float scale)
        {
#if FIDA_ANDROID
            Rectangle strip = TransformRect(_skillStripBounds, CardBounds.Center.ToVector2(), scale,
                VisualOffset + (CardState == UIButtonState.Hovered ? new Vector2(0f, -9f) : Vector2.Zero));
            if (Player.Skills.Count == 0) return;

            const int maximumVisibleMobile = 5;
            int visibleMobile = Math.Min(maximumVisibleMobile, Player.Skills.Count);
            int sizeMobile = Math.Min(strip.Height, Math.Max(28, (int)(38 * scale)));
            int gapMobile = Math.Max(6, (int)(8 * scale));
            int startMobile = strip.Left;
            for (int i = 0; i < visibleMobile; i++)
            {
                Skill skill = Player.Skills[i];
                Color accent = UISkillIconCatalog.Fill(skill);
                var bounds = new Rectangle(startMobile + i * (sizeMobile + gapMobile),
                    strip.Center.Y - sizeMobile / 2, sizeMobile, sizeMobile);
                sb.Draw(Pixel, bounds, UITheme.PanelLight);
                sb.Draw(Pixel, new Rectangle(bounds.Left, bounds.Top, 5, bounds.Height), accent);
                UIUtils.DrawRectangleOutlines(sb, bounds, accent, 2, Pixel);
                DrawCompactText(sb, UISkillIconCatalog.Glyph(skill.Type), bounds.Center.ToVector2(),
                    UITheme.Ink, 0.60f * scale, sizeMobile - 5f);
            }
            return;
#else
            if (Player.Skills.Count == 0) return;

            Rectangle strip = TransformRect(_skillStripBounds, CardBounds.Center.ToVector2(), scale,
                VisualOffset + (CardState == UIButtonState.Hovered ? new Vector2(0f, -9f) : Vector2.Zero));

            const int maximumVisible = 5;
            int visible = System.Math.Min(maximumVisible, Player.Skills.Count);
            int size = System.Math.Min(strip.Height, System.Math.Max(
                UIConfig.ScaleDesktopMetric(23), (int)(UIConfig.ScaleDesktopMetric(30) * scale)));
            int gap = System.Math.Max(UIConfig.ScaleDesktopMetric(3),
                (int)(UIConfig.ScaleDesktopMetric(4) * scale));
            int totalWidth = visible * size + (visible - 1) * gap;
            int startX = strip.Left + Math.Max(0, (strip.Width - totalWidth) / 2);
            int centerY = strip.Center.Y;

            for (int i = 0; i < visible; i++)
            {
                Skill skill = Player.Skills[i];
                Color accent = UISkillIconCatalog.Fill(skill);
                var bounds = new Rectangle(startX + i * (size + gap), centerY - size / 2, size, size);
                sb.Draw(Pixel, bounds, UITheme.PanelLight);
                sb.Draw(Pixel, new Rectangle(bounds.Left, bounds.Top, Math.Max(4, (int)(5 * scale)), bounds.Height), accent);
                UIUtils.DrawRectangleOutlines(sb, bounds, accent,
                    System.Math.Max(2, (int)(2 * scale)), Pixel);
                DrawCompactText(sb, UISkillIconCatalog.Glyph(skill.Type), bounds.Center.ToVector2(),
                    UITheme.Ink, 0.58f * scale, size - 4f);
            }

            if (Player.Skills.Count > maximumVisible)
            {
                var last = new Vector2(startX + (visible - 1) * (size + gap) + size / 2f, centerY);
                DrawCompactText(sb, $"+{Player.Skills.Count - maximumVisible}", last,
                    UITheme.Ink, 0.40f * scale, size - 3f);
            }
#endif
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
