using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Fida.UI
{
    public class UIControl
    {
        int Id;
        SpriteFont Font;

        /// <summary>
        /// 控件名称（来自 JSON 的 name 字段），用于按名称查找控件
        /// </summary>
        public string Name { get; set; } = "";

        // ─── 焦点导航支持 ──────────────────────────────────
        /// <summary>
        /// 此控件是否拥有手柄焦点
        /// </summary>
        public bool IsFocused { get; private set; }

        /// <summary>
        /// 焦点排序索引，用于确定导航顺序
        /// </summary>
        public int FocusIndex { get; set; } = -1;

        /// <summary>
        /// 此控件是否可被手柄聚焦
        /// </summary>
        public virtual bool CanFocus => false;

        /// <summary>
        /// 获取焦点时触发
        /// </summary>
        public event Action<UIControl> FocusGained;

        /// <summary>
        /// 失去焦点时触发
        /// </summary>
        public event Action<UIControl> FocusLost;

        /// <summary>
        /// 确认键按下时触发（手柄 A 键）
        /// </summary>
        public event Action<UIControl> Confirmed;

        /// <summary>
        /// 设置焦点状态
        /// </summary>
        public void SetFocus(bool focused)
        {
            if (focused == IsFocused) return;
            IsFocused = focused;
            if (focused)
                FocusGained?.Invoke(this);
            else
                FocusLost?.Invoke(this);
        }

        /// <summary>
        /// 触发确认事件（手柄 A 键按下时调用）
        /// </summary>
        public void OnConfirmed()
        {
            Confirmed?.Invoke(this);
        }

        public UIControl(int id)
        {
            Id = id;
        }

        public int GetId()
        {
            return Id;
        }

        public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // 默认实现为空，子类可以重写此方法进行绘制
        }

        public virtual void Update(MouseState mouseState)
        {
            // 默认实现为空，子类可以重写此方法进行更新逻辑
        }

        public virtual void SetFont(SpriteFont font)
        {
            Font = font; // 设置字体
        }

        public virtual void Update(GameTime gameTime)
        {
            // 默认实现为空，子类可以重写此方法进行每帧逻辑更新（如动画计时）
        }

        public virtual void Update(string _str)
        {
            // 默认实现为空，子类可以重写此方法进行更新逻辑
            // 例如，可以在这里处理字符串更新逻辑
            // 目前没有具体实现，留空以供子类使用
        }
    }
}