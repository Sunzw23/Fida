using Fida.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    public class Character : UI.UIControl
    {
        public Player Player { get; private set; }
        public Vector2 Position { get; set; }
        
        public SpriteFont Font { get; private set; }

        public UILabel NameLabel { get; private set; }
        public List<UIButton> NumberButton { get; private set; }
        public UIButton HealthButton { get; private set; }

        public Character(int id, Player player, Vector2 position, SpriteFont font, Texture2D pixel): base(id)
        {
            Position = position;
            Player = player;
            Font = font;
            NameLabel = new UI.UILabel(0, player.Name, position + UIConfig.NamePosition, font);
            NumberButton = [new UI.UIButton(1, TranslateRectangle(UIConfig.NumberAreas[0], position), Player.Numbers[0].ToString(), pixel, font), 
                new UI.UIButton(2, TranslateRectangle(UIConfig.NumberAreas[1], position), Player.Numbers[1].ToString(), pixel, font)];
            HealthButton = new UI.UIButton(3, TranslateRectangle(UIConfig.HealthArea, position), Player.Health.ToString(), pixel, font);

        }
        

        public Vector2 GetGlobalPosition()
        {
            return Position;
        }

        private Rectangle TranslateRectangle(Rectangle rect, Vector2 vector)
        {
            return new Rectangle((int)(rect.X + vector.X), (int)(rect.Y + vector.Y), rect.Width, rect.Height);
        }

        public override void Update(MouseState mouseState)
        {
            // 更新逻辑可以在这里实现
            // 例如处理鼠标事件、更新状态等
            NumberButton[0].Update(mouseState);
            NumberButton[1].Update(mouseState);
            HealthButton.Update(mouseState);
        }

        public void UpdateNumbers()
        {
            // 更新数字按钮的文本

            NumberButton[0].Update(Player.Numbers[0].ToString());
            NumberButton[1].Update(Player.Numbers[1].ToString());
        }

        public void UpdateHealth()
        {
            // 更新生命值按钮的文本
            var _healthButton = HealthButton;
            _healthButton.Update("Hp: " + Player.Health.ToString());
        }

        public override void Draw(SpriteBatch sb)
        {
            // 绘制角色的所有控件
            NameLabel.Draw(sb);
            NumberButton[0].Draw(sb);
            NumberButton[1].Draw(sb);
            HealthButton.Draw(sb);
        }



    };



}


