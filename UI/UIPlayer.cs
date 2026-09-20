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
        private int[] _deferredNumbers;
        public bool DeferNumberVisual { get; private set; }
        private bool _pointerOverCard;
        public bool DeferHealthVisual { get; set; }

        // ─── 手柄焦点支持 ──────────────────────────────────
        /// <summary>
        /// 玩家卡片可以被手柄聚焦（当启用时）
        /// </summary>
        public override bool CanFocus => IsEnabled;

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

        public UIPlayer(int id, Player player, Vector2 position, SpriteFont font, Texture2D pixel, Texture2D portraitTexture = null) : base(id)
        {
            Position = position;
            Player = player;
            Font = font;
            Pixel = pixel;
            PortraitTexture = portraitTexture;

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
            int padding = Math.Clamp(CardBounds.Height / 14, 8, 14);
            int portraitSize = Math.Min(CardBounds.Height - padding * 2, UIConfig.Int("portraitSize"));
            PortraitBounds = new Rectangle(InnerCardBounds.Left + padding,
                CardBounds.Center.Y - portraitSize / 2, portraitSize, portraitSize);

            int numberWidth = UIConfig.Int("bottomElementWidth");
            int numberHeight = Math.Min(CardBounds.Height - padding * 2,
                UIConfig.Int("bottomElementHeight"));
            int numberGap = Math.Clamp(CardBounds.Width / 90, 6, 12);
            int numberRight = InnerCardBounds.Right - padding;
            Number1Bounds = new Rectangle(numberRight - numberWidth,
                CardBounds.Center.Y - numberHeight / 2, numberWidth, numberHeight);
            Number0Bounds = new Rectangle(Number1Bounds.Left - numberGap - numberWidth,
                Number1Bounds.Y, numberWidth, numberHeight);

            int infoLeft = PortraitBounds.Right + Math.Clamp(CardBounds.Width / 45, 12, 24);
            int infoRight = Number0Bounds.Left - Math.Clamp(CardBounds.Width / 55, 10, 18);
            int infoWidth = Math.Max(80, infoRight - infoLeft);
            _nameCenter = new Vector2(infoLeft + infoWidth / 2f, CardBounds.Top + padding + 18);
            NameLabel = new UILabel(0, player.Name, _nameCenter, font)
            {
                MaximumWidth = infoWidth,
                MinimumScale = 0.55f
            };

            HealthBounds = new Rectangle(infoLeft, CardBounds.Top + padding + 40,
                infoWidth, Math.Clamp(CardBounds.Height / 7, 16, 24));
            _healthCenter = new Vector2(HealthBounds.Center.X, HealthBounds.Center.Y);
            _skillStripBounds = new Rectangle(infoLeft, HealthBounds.Bottom + 8,
                infoWidth, Math.Max(24, CardBounds.Bottom - padding - HealthBounds.Bottom - 8));

            Number0Button = new UIAnimatedButton(10, Number0Bounds,
                player.Numbers.Length > 0 ? player.Numbers[0].ToString() : "1",
                pixel, font) { ParentDrawOnly = true };
            HealthLabel = new UILabel(11, player.Health.ToString(), _healthCenter, font);
            Number1Button = new UIAnimatedButton(12, Number1Bounds,
                player.Numbers.Length > 1 ? player.Numbers[1].ToString() : "1",
                pixel, font) { ParentDrawOnly = true };
#else
            int portraitX = InnerCardBounds.Center.X - UIConfig.Int("portraitSize") / 2;
            int portraitY = InnerCardBounds.Y + verticalSpacing;
            PortraitBounds = new Rectangle(portraitX, portraitY, UIConfig.Int("portraitSize"), UIConfig.Int("portraitSize"));

            int nameY = portraitY + UIConfig.Int("portraitSize") + verticalSpacing;
            NameLabel = new UILabel(0, player.Name,
                new Vector2(InnerCardBounds.Center.X, nameY + nameHeight / 2),
                font)
            {
                MaximumWidth = InnerCardBounds.Width - 24,
                MinimumScale = 0.5f
            };
            _nameCenter = new Vector2(InnerCardBounds.Center.X, nameY + nameHeight / 2);

            int nameBottomY = nameY + UIConfig.Int("nameHeight");
            int bottomY = nameBottomY + verticalSpacing;
            int spacing = (InnerCardBounds.Width - 3 * UIConfig.Int("bottomElementWidth")) / 4;
            int startX = InnerCardBounds.X + spacing;

            Number0Bounds = new Rectangle(startX, bottomY, UIConfig.Int("bottomElementWidth"), UIConfig.Int("bottomElementHeight"));
            Number0Button = new UIAnimatedButton(10, Number0Bounds,
                player.Numbers.Length > 0 ? player.Numbers[0].ToString() : "1",
                pixel, font) { ParentDrawOnly = true };

            HealthBounds = new Rectangle(
                startX + UIConfig.Int("bottomElementWidth") + spacing,
                bottomY,
                UIConfig.Int("bottomElementWidth"),
                UIConfig.Int("bottomElementHeight"));
            HealthLabel = new UILabel(11, player.Health.ToString(),
                new Vector2(HealthBounds.Center.X, HealthBounds.Center.Y),
                font);
            _healthCenter = new Vector2(HealthBounds.Center.X, HealthBounds.Center.Y);

            Number1Bounds = new Rectangle(
                startX + 2 * (UIConfig.Int("bottomElementWidth") + spacing),
                bottomY,
                UIConfig.Int("bottomElementWidth"),
                UIConfig.Int("bottomElementHeight"));
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
            // Child number buttons are registered as controls and update themselves.
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
            HealthLabel.Update(Player.Health.ToString());
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
            HealthLabel.Update((_deferredHealth ?? Player.Health).ToString());
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
            Vector2 interactionOffset = CardState == UIButtonState.Hovered
                ? new Vector2(0f, -9f)
                : Vector2.Zero;
            float drawScale = VisualScale * interactionScale;
            Vector2 drawOffset = VisualOffset + interactionOffset;
            Vector2 pivot = CardBounds.Center.ToVector2();

            Rectangle cardBounds = TransformRect(CardBounds, pivot, drawScale, drawOffset);
            Rectangle innerBounds = TransformRect(InnerCardBounds, pivot, drawScale, drawOffset);
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
                int expansion = 5 + (int)(pulse * 5);
                var cueBounds = new Rectangle(
                    cardBounds.X - expansion,
                    cardBounds.Y - expansion,
                    cardBounds.Width + expansion * 2,
                    cardBounds.Height + expansion * 2);
                UIUtils.DrawRectangleOutlines(sb, cueBounds,
                    Color.Lerp(UITheme.Gold, UITheme.GoldBright, pulse),
                    2 + (int)(pulse * 2), Pixel);
            }

            if (SelectionStrength > 0f)
            {
                int expansion = 8 + (int)(SelectionStrength * 5f);
                var selectedBounds = new Rectangle(cardBounds.X - expansion, cardBounds.Y - expansion,
                    cardBounds.Width + expansion * 2, cardBounds.Height + expansion * 2);
                UIUtils.DrawGlowFrame(sb, selectedBounds, Pixel,
                    UITheme.AccentGreen, 4, SelectionStrength);
            }

            // 绘制外框背景
            sb.Draw(Pixel, cardBounds, backColor);

            // 绘制外框边框
            if (HasDoubleBorder())
            {
                UIUtils.DrawRectangleOutlines(sb, cardBounds, UITheme.Gold, outerThickness, Pixel);
                var pressedInnerBounds = new Rectangle(
                    cardBounds.X + outerThickness,
                    cardBounds.Y + outerThickness,
                    cardBounds.Width - outerThickness * 2,
                    cardBounds.Height - outerThickness * 2
                );
                UIUtils.DrawRectangleOutlines(sb, pressedInnerBounds,
                    UITheme.AccentViolet, 2, Pixel);
            }
            else
            {
                UIUtils.DrawEngravedFrame(sb, cardBounds, Pixel, GetCardBorderColor(),
                    foreColor, outerThickness, System.Math.Max(6, outerThickness + 4),
                    18, false);
            }

            // 绘制内框边框（Pressed 时反色）
            UIUtils.DrawRectangleOutlines(sb, innerBounds, foreColor, UIConfig.Int("innerBorderThickness"), Pixel);

            // 绘制头像
            if (PortraitTexture != null)
            {
                sb.Draw(PortraitTexture, portraitBounds, Color.White);
                UIUtils.DrawRectangleOutlines(sb, portraitBounds, UITheme.Ink, 1, Pixel);
            }

            DrawSkillMedallions(sb, cardBounds, portraitBounds, nameCenter, number0Bounds, drawScale);

            if ((_pointerOverCard || IsFocused) && Player.Skills.Count > 0)
                DrawSkillDetails(sb, portraitBounds, drawScale);

            // 绘制底部元素
            Number0Button.DrawAt(sb, gameTime, number0Bounds);
#if FIDA_ANDROID
            DrawHealthBar(sb, healthBounds, drawScale);
#endif
            HealthLabel.SetColor(foreColor);
            HealthLabel.DrawAt(sb, gameTime, healthCenter, drawScale);
            Number1Button.DrawAt(sb, gameTime, number1Bounds);

            // 绘制名字（Pressed 时反色）
            NameLabel.SetColor(foreColor);
            NameLabel.DrawAt(sb, gameTime, nameCenter, drawScale);

            if (ImpactStrength > 0f)
            {
                sb.Draw(Pixel, cardBounds, UITheme.GoldBright * (0.26f * ImpactStrength));
                UIUtils.DrawRectangleOutlines(sb, cardBounds, UITheme.GoldBright,
                    3 + (int)(ImpactStrength * 4f), Pixel);
            }
        }

        private void DrawHealthBar(SpriteBatch sb, Rectangle bounds, float scale)
        {
            sb.Draw(Pixel, bounds, UITheme.PanelDark);
            float ratio = Player.MaxHealth <= 0 ? 0f : MathHelper.Clamp(Player.Health / (float)Player.MaxHealth, 0f, 1f);
            int inset = Math.Max(2, (int)(2f * scale));
            int fillWidth = Math.Max(0, (int)((bounds.Width - inset * 2) * ratio));
            if (fillWidth > 0)
                sb.Draw(Pixel, new Rectangle(bounds.Left + inset, bounds.Top + inset,
                    fillWidth, Math.Max(1, bounds.Height - inset * 2)), UITheme.AccentGreen * 0.82f);
            UIUtils.DrawRectangleOutlines(sb, bounds, UITheme.Gold,
                Math.Max(1, (int)(scale)), Pixel);
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
            if (Player.Skills.Count == 0)
            {
                DrawCompactText(sb, "NO MODULES", strip.Center.ToVector2(), UITheme.InkMuted,
                    0.34f * scale, strip.Width);
                return;
            }

            const int maximumVisibleMobile = 5;
            int visibleMobile = Math.Min(maximumVisibleMobile, Player.Skills.Count);
            int sizeMobile = Math.Min(strip.Height, Math.Max(22, (int)(32 * scale)));
            int gapMobile = Math.Max(4, (int)(6 * scale));
            int totalMobile = visibleMobile * sizeMobile + (visibleMobile - 1) * gapMobile;
            int startMobile = strip.Center.X - totalMobile / 2;
            for (int i = 0; i < visibleMobile; i++)
            {
                Skill skill = Player.Skills[i];
                var bounds = new Rectangle(startMobile + i * (sizeMobile + gapMobile),
                    strip.Center.Y - sizeMobile / 2, sizeMobile, sizeMobile);
                sb.Draw(Pixel, bounds, UISkillIconCatalog.Fill(skill));
                UIUtils.DrawRectangleOutlines(sb, bounds, UITheme.GoldBright, 1, Pixel);
                DrawCompactText(sb, UISkillIconCatalog.Glyph(skill.Type), bounds.Center.ToVector2(),
                    UITheme.Ink, 0.44f * scale, sizeMobile - 5f);
            }
            return;
#else
            if (Player.Skills.Count == 0)
            {
                DrawCompactText(sb, "NO SKILLS", new Vector2(cardBounds.Center.X,
                    (nameCenter.Y + numberBounds.Top) / 2f), UITheme.InkMuted, 0.30f * scale, 120f);
                return;
            }

            const int maximumVisible = 6;
            int visible = System.Math.Min(maximumVisible, Player.Skills.Count);
            int size = System.Math.Max(18, (int)(28 * scale));
            int gap = System.Math.Max(3, (int)(5 * scale));
            int totalWidth = visible * size + (visible - 1) * gap;
            int startX = cardBounds.Center.X - totalWidth / 2;
            int centerY = (int)((nameCenter.Y + numberBounds.Top) / 2f);

            for (int i = 0; i < visible; i++)
            {
                Skill skill = Player.Skills[i];
                var bounds = new Rectangle(startX + i * (size + gap), centerY - size / 2, size, size);
                sb.Draw(Pixel, bounds, UISkillIconCatalog.Fill(skill));
                UIUtils.DrawRectangleOutlines(sb, bounds, UITheme.GoldBright,
                    System.Math.Max(1, (int)(2 * scale)), Pixel);
                DrawCompactText(sb, UISkillIconCatalog.Glyph(skill.Type), bounds.Center.ToVector2(),
                    UITheme.Ink, 0.48f * scale, size - 4f);
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
            UIUtils.DrawEngravedFrame(sb, portraitBounds, Pixel, UITheme.GoldBright,
                UITheme.PanelDark, 2, 6, 14, false);

            DrawCompactText(sb, "SKILLS", new Vector2(portraitBounds.Center.X,
                portraitBounds.Top + 24 * scale), UITheme.GoldBright, 0.42f * scale,
                portraitBounds.Width - 20f);

            float rowHeight = System.Math.Max(20f, 27f * scale);
            float y = portraitBounds.Top + 54f * scale;
            int maximumRows = System.Math.Max(1,
                (int)((portraitBounds.Bottom - y - 8f) / rowHeight));
            int shown = System.Math.Min(maximumRows, Player.Skills.Count);
            for (int i = 0; i < shown; i++)
            {
                Skill skill = Player.Skills[i];
                string line = $"{UISkillIconCatalog.Glyph(skill.Type)}  {GameText.SkillName(skill.Type)}  " +
                    $"[{CampaignSkillBalance.Cost(skill.Type)} SP]";
                DrawCompactText(sb, line, new Vector2(portraitBounds.Center.X, y + rowHeight / 2f),
                    UITheme.Ink, 0.44f * scale, portraitBounds.Width - 18f);
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
            string display = UIFontGuard.Sanitize(Font, text);
            Vector2 measured = Font.MeasureString(display);
            if (measured.X <= 0f) return;
            float textScale = System.Math.Min(preferredScale, maximumWidth / measured.X);
            sb.DrawString(Font, display, center - measured * textScale / 2f, color,
                0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }
    }
}
