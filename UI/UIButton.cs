#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Fida.UI
{
    /// <summary>
    /// 按钮状态枚举
    /// </summary>
    public enum UIButtonState
    {
        Normal,     // 可用状态 - 静态，小边框
        Disabled,   // 禁用状态 - 静态，无边框
        Hovered,    // 鼠标悬浮/手柄聚焦 - 静态，大边框
        Pressed,    // 按下状态 - 双层边框
        Selected    // 已选择状态 - 双层边框
    }

    public class UIButton : UIButtonBase<UIButtonState>
    {
        public float MinimumTextScale { get; set; }
        // 按钮状态（优先级：Disabled > Selected > Pressed > Hovered > Normal）
        protected override UIButtonState GetState()
        {
            if (!IsEnabled) return UIButtonState.Disabled;
            if (IsSelected) return UIButtonState.Selected;
            if (_isPressed) return UIButtonState.Pressed;
            if (IsHovered || IsFocused) return UIButtonState.Hovered;
            return UIButtonState.Normal;
        }

        protected override bool IsPressedOrSelected() => State == UIButtonState.Pressed || State == UIButtonState.Selected;
        protected override bool IsDisabled() => State == UIButtonState.Disabled;
        protected override bool IsHoveredState() => State == UIButtonState.Hovered;

        /// <summary>
        /// 按钮点击事件 - 当按钮完成一次完整点击时触发
        /// </summary>
        public event Action<UIButton>? Clicked;


        // 由"启用→禁用"分支调用：UIButton 清除 Selected
        protected override void OnDisabledTransition() { IsSelected = false; }

        // 点击完成后触发 Clicked 事件
        protected override void OnClickCompleted()
        {
            Audio.AudioService.Feedback(Name.StartsWith("back", StringComparison.OrdinalIgnoreCase)
                ? Audio.SoundCue.Back : Audio.SoundCue.Confirm);
            Clicked?.Invoke(this);
        }

        public UIButton(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font) : base(id)
        {
            Bounds = bounds;
            Text = text;
            Pixel = pixel;
            Font = font;
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            Color backColor = !IsEnabled ? UITheme.PanelDark : IsSelected ? UITheme.AccentBlue : UITheme.PanelLight;
            Color foreColor = GetForeColor();
            bool primary = Name is "startGame" or "retry" or "resume";
            if (primary && State != UIButtonState.Disabled)
            {
                backColor = UIPaper.Face;
                foreColor = UITheme.BgTop * EntranceAmount;
            }

            // 绘制背景
            DrawPaperButton(sb, backColor, 8);
            if (IsSelected) foreColor = UIPaper.Ink * EntranceAmount;

            // 绘制文字（居中）
            string displayText = UIFontGuard.Sanitize(Font, Text);
            var size = Font.MeasureString(displayText);
            float availableWidth = MathHelper.Max(1, Bounds.Width - 32);
            float availableHeight = MathHelper.Max(1, Bounds.Height - 20);
            float preferredScale = 1f;
#if !FIDA_ANDROID
            preferredScale = UIDesktopLayout.Scale(sb.GraphicsDevice.Viewport);
#endif
            preferredScale = MathHelper.Max(preferredScale, MinimumTextScale);
            float textScale = UITextRenderer.FitScale(size, availableWidth, availableHeight, preferredScale);
            float pressOffset = PressAmount * 2f;
            var pos = new Vector2(
                Bounds.Center.X - size.X * textScale / 2,
                Bounds.Center.Y - size.Y * textScale / 2 + pressOffset
            );
            UITextRenderer.DrawString(sb, Font, displayText, pos, foreColor, 0f, Vector2.Zero,
                textScale, SpriteEffects.None, 0f);


        }

        public override void Update(string text)
        {
            Text = text;
        }

        /// <summary>
        /// 设置选定状态
        /// </summary>
        public override void SetSelected(bool selected)
        {
            if (IsEnabled)
            {
                IsSelected = selected;
                NotifyStateChanged();
            }
        }
    }
}
