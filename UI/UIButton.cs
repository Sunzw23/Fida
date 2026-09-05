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
        // 按钮状态（优先级：Disabled > Selected > Pressed > Hovered > Normal）
        protected override UIButtonState GetState()
        {
            if (!IsEnabled) return UIButtonState.Disabled;
            if (IsSelected) return UIButtonState.Selected;
            if (_isPressed) return UIButtonState.Pressed;
            if (IsHovered) return UIButtonState.Hovered;
            return UIButtonState.Normal;
        }

        protected override bool IsPressedOrSelected() => State == UIButtonState.Pressed || State == UIButtonState.Selected;
        protected override bool IsDisabled() => State == UIButtonState.Disabled;
        protected override bool IsHoveredState() => State == UIButtonState.Hovered;

        /// <summary>
        /// 按钮点击事件 - 当按钮完成一次完整点击时触发
        /// </summary>
        public event Action<UIButton> Clicked;


        // 由"启用→禁用"分支调用：UIButton 清除 Selected
        protected override void OnDisabledTransition() { IsSelected = false; }

        // 点击完成后触发 Clicked 事件
        protected override void OnClickCompleted() { Clicked?.Invoke(this); }

        public UIButton(int id, Rectangle bounds, string text, Texture2D pixel, SpriteFont font) : base(id)
        {
            Bounds = bounds;
            Text = text;
            Pixel = pixel;
            Font = font;
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            Color backColor = GetBackColor();
            Color foreColor = GetForeColor();
            Color borderColor = GetBorderColor();
            int borderThickness = GetBorderThickness();

            // 绘制背景
            sb.Draw(Pixel, Bounds, backColor);

            // 绘制文字（居中）
            var size = Font.MeasureString(Text);
            var pos = new Vector2(
                Bounds.Center.X - size.X / 2,
                Bounds.Center.Y - size.Y / 2
            );
            sb.DrawString(Font, Text, pos, foreColor);

            // 绘制边框（禁用状态 borderThickness=0，不绘制）
            if (borderThickness > 0)
            {
                if (HasDoubleBorder())
                {
                    // 外层边框（描金）
                    UIUtils.DrawRectangleOutlines(sb, Bounds, UITheme.Gold, borderThickness, Pixel);
                    // 内层边框（深墨），向内偏移
                    var innerBounds = new Rectangle(
                        Bounds.X + borderThickness,
                        Bounds.Y + borderThickness,
                        Bounds.Width - borderThickness * 2,
                        Bounds.Height - borderThickness * 2
                    );
                    UIUtils.DrawRectangleOutlines(sb, innerBounds, UITheme.Ink, 2, Pixel);
                }
                else
                {
                    UIUtils.DrawRectangleOutlines(sb, Bounds, borderColor, borderThickness, Pixel);
                }
            }
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
