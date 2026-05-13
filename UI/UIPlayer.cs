using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Fida.Logic;

namespace Fida.UI
{
    public class UIPlayer : UIControl
    {
        public Player Player { get; private set; }
        public Vector2 Position { get; set; }

        public SpriteFont Font { get; private set; }
        public Texture2D Pixel { get; set; }

        public Texture2D PortraitTexture { get; set; }

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

        // 环境检查器 — 卡片在 ChooseTarget 时启用交互
        public Func<bool> EnabledChecker { get; set; }

        // 卡片交互状态
        private bool IsEnabled { get; set; } = true;
        private bool IsHovered { get; set; } = false;
        private bool _isPressed { get; set; } = false;
        private bool _clickStartedOnThis = false;
        private bool _mouseWasDown = false;

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

        private static readonly Color Black = Color.Black;
        private static readonly Color White = Color.White;

        public UIPlayer(int id, Player player, Vector2 position, SpriteFont font, Texture2D pixel, Texture2D portraitTexture = null) : base(id)
        {
            Position = position;
            Player = player;
            Font = font;
            Pixel = pixel;
            PortraitTexture = portraitTexture;

            float nameHeight = font.MeasureString(player.Name).Y;

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

            int portraitX = InnerCardBounds.Center.X - UIConfig.Int("portraitSize") / 2;
            int portraitY = InnerCardBounds.Y + verticalSpacing;
            PortraitBounds = new Rectangle(portraitX, portraitY, UIConfig.Int("portraitSize"), UIConfig.Int("portraitSize"));

            int nameY = portraitY + UIConfig.Int("portraitSize") + verticalSpacing;
            NameLabel = new UILabel(0, player.Name,
                new Vector2(InnerCardBounds.Center.X, nameY + nameHeight / 2),
                font);

            int nameBottomY = nameY + UIConfig.Int("nameHeight");
            int bottomY = nameBottomY + verticalSpacing;
            int spacing = (InnerCardBounds.Width - 3 * UIConfig.Int("bottomElementWidth")) / 4;
            int startX = InnerCardBounds.X + spacing;

            Number0Bounds = new Rectangle(startX, bottomY, UIConfig.Int("bottomElementWidth"), UIConfig.Int("bottomElementHeight"));
            Number0Button = new UIAnimatedButton(10, Number0Bounds,
                player.Numbers.Length > 0 ? player.Numbers[0].ToString() : "1",
                pixel, font);

            HealthBounds = new Rectangle(
                startX + UIConfig.Int("bottomElementWidth") + spacing,
                bottomY,
                UIConfig.Int("bottomElementWidth"),
                UIConfig.Int("bottomElementHeight"));
            HealthLabel = new UILabel(11, player.Health.ToString(),
                new Vector2(HealthBounds.Center.X, HealthBounds.Center.Y),
                font);

            Number1Bounds = new Rectangle(
                startX + 2 * (UIConfig.Int("bottomElementWidth") + spacing),
                bottomY,
                UIConfig.Int("bottomElementWidth"),
                UIConfig.Int("bottomElementHeight"));
            Number1Button = new UIAnimatedButton(12, Number1Bounds,
                player.Numbers.Length > 1 ? player.Numbers[1].ToString() : "1",
               pixel, font);
        }

        // ---- 状态驱动渲染 ----

        private Color GetCardBackColor()
        {
            return CardState switch
            {
                UIButtonState.Pressed => White,
                _ => Black
            };
        }

        private Color GetCardForeColor()
        {
            return CardState switch
            {
                UIButtonState.Pressed => Black,
                _ => White
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

            Number0Button.Update(mouseState);
            Number1Button.Update(mouseState);
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
            Number0Button.Update(gameTime);
            Number1Button.Update(gameTime);
        }

        public void UpdateNumbers()
        {
            if (Player.Numbers.Length > 0)
                Number0Button.Update(Player.Numbers[0].ToString());
            if (Player.Numbers.Length > 1)
                Number1Button.Update(Player.Numbers[1].ToString());
        }

        public void UpdateHealth()
        {
            HealthLabel.Update(Player.Health.ToString());
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

            // 绘制外框背景
            sb.Draw(Pixel, CardBounds, backColor);

            // 绘制外框边框
            if (HasDoubleBorder())
            {
                UIUtils.DrawRectangleOutlines(sb, CardBounds, White, outerThickness, Pixel);
                var innerBounds = new Rectangle(
                    CardBounds.X + outerThickness,
                    CardBounds.Y + outerThickness,
                    CardBounds.Width - outerThickness * 2,
                    CardBounds.Height - outerThickness * 2
                );
                UIUtils.DrawRectangleOutlines(sb, innerBounds, Black, 2, Pixel);
            }
            else
            {
                UIUtils.DrawRectangleOutlines(sb, CardBounds, foreColor, outerThickness, Pixel);
            }

            // 绘制内框边框（Pressed 时反色）
            UIUtils.DrawRectangleOutlines(sb, InnerCardBounds, foreColor, UIConfig.Int("innerBorderThickness"), Pixel);

            // 绘制头像
            if (PortraitTexture != null)
            {
                sb.Draw(PortraitTexture, PortraitBounds, foreColor);
            }

            // 绘制底部元素
            Number0Button.Draw(sb, gameTime);
            HealthLabel.SetColor(foreColor);
            HealthLabel.Draw(sb, gameTime);
            Number1Button.Draw(sb, gameTime);

            // 绘制名字（Pressed 时反色）
            NameLabel.SetColor(foreColor);
            NameLabel.Draw(sb, gameTime);
        }
    }
}