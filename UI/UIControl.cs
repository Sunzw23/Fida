using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Runtime.CompilerServices;

namespace Fida.UI
{
    public class UIControl
    {
        SpriteFont Font;
        public bool IsVisible = true;

        public virtual void Draw(SpriteBatch spriteBatch)
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

        
        public virtual void Hide()
        {
            IsVisible = false; // 隐藏标签
        }

        public virtual void Show()
        {
            IsVisible = true; // 显示标签
        }

    }
}
