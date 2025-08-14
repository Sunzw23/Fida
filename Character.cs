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
    // 用于数字按钮点击的新委托，仅传递数字在数组中的索引。
    public delegate void NumberButtonHandler(int numberIndex);
    // 用于生命值按钮点击的新委托，不传递任何参数，因为我们只需要知道它被按下了。
    public delegate void HealthButtonHandler();

    public class Character : UI.UIControl
    {
        // 声明两个更具体的事件，供外部订阅。
        public event NumberButtonHandler OnNumberButtonClick;
        public event HealthButtonHandler OnHealthButtonClick;

        public Player Player { get; private set; }
        public Vector2 Position { get; set; }

        public SpriteFont Font { get; private set; }

        public UILabel NameLabel { get; private set; }
        public List<UIButton> NumberButton { get; private set; }
        public UIButton HealthButton { get; private set; }

        public Character(Player player, Vector2 position, SpriteFont font, Texture2D pixel)
        {
            Position = position;
            Player = player;
            Font = font;
            NameLabel = new UI.UILabel(player.Name, position + UIConfig.NamePosition, font);

            // 使用事件驱动的UIButton，在构造函数中订阅 OnClick 事件。
            // 注意：ID 可以是任意数字，因为我们不再依赖它来判断类型。
            NumberButton = [
                new UI.UIButton(TranslateRectangle(UIConfig.NumberAreas[0], position), Player.Numbers[0].ToString(), pixel, font),
                new UI.UIButton(TranslateRectangle(UIConfig.NumberAreas[1], position), Player.Numbers[1].ToString(), pixel, font)
            ];
            HealthButton = new UI.UIButton(TranslateRectangle(UIConfig.HealthArea, position), UIText.HealthSpace(player.Health), pixel, font);

            // 将所有按钮都订阅到同一个事件处理方法。
            NumberButton[0].OnClick += HandleButtonClick;
            NumberButton[1].OnClick += HandleButtonClick;
            HealthButton.OnClick += HandleButtonClick;

            // 启用所有按钮。
            NumberButton[0].Enable();
            NumberButton[1].Enable();
            HealthButton.Enable();
        }

        // 这是一个通用的事件处理方法，用于响应所有按钮的点击。
        private void HandleButtonClick(object sender)
        {
            // 使用 'sender' 参数来确定是哪个按钮被点击了。
            if (sender is UIButton clickedButton)
            {
                // 首先判断是否是生命值按钮，这是最直接的判断方式。
                if (clickedButton == HealthButton)
                {
                    OnHealthButtonClick?.Invoke();
                }
                // 否则，它就是数字按钮。我们通过在列表中查找其索引来确定是哪一个。
                else
                {
                    int index = NumberButton.IndexOf(clickedButton);
                    if (index >= 0)
                    {
                        // 触发事件，并传递按钮在列表中的索引。
                        OnNumberButtonClick?.Invoke(index);
                    }
                }
            }
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
            // 更新逻辑保持不变。
            NumberButton[0].Update(mouseState);
            NumberButton[1].Update(mouseState);
            HealthButton.Update(mouseState);
        }

        public void UpdateNumbers()
        {
            // 更新数字按钮的文本。
            NumberButton[0].UpdateText(Player.Numbers[0].ToString());
            NumberButton[1].UpdateText(Player.Numbers[1].ToString());
        }

        public void UpdateHealth()
        {
            // 更新生命值按钮的文本。
            var _healthButton = HealthButton;
            _healthButton.UpdateText(UIText.HealthSpace(Player.Health));
        }

        public void Disable()
        {
            // 禁用所有按钮。
            NumberButton[0].Disable();
            NumberButton[1].Disable();
            HealthButton.Disable();
        }


        public override void Draw(SpriteBatch sb)
        {
            // 绘制角色的所有控件。
            NameLabel.Draw(sb);
            NumberButton[0].Draw(sb);
            NumberButton[1].Draw(sb);
            HealthButton.Draw(sb);
        }
    };
}
