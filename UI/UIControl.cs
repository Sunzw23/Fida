using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    public class UIControl
    {
        int Id;
        SpriteFont Font;

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
