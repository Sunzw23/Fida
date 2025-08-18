using Fida.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    public class UIChooseNumber : UI.UIControl
    {
        public event ChooseNumberHandler OnChooseNumber;
        UILabel ChooseNumberLabel;
        Dictionary<int, UIButton> NumberButtons;

        public UIChooseNumber(Texture2D pixel, SpriteFont font, string message)
        {
            // 创建决定应用的UI
            UILabel chooseNumberLabel = new UILabel(message, UIConfig.ActionInfoPosition, font);
            ChooseNumberLabel = chooseNumberLabel;
            NumberButtons = [];
            // 创建按钮
            for (int i = 0; i < 10; i++)
            {
                UIButton numberButton = new UIButton(UIConfig.NumberButtonArea(i), i.ToString(), pixel, font);
                numberButton.Enable();
                NumberButtons.Add(i, numberButton);
            }
            // 订阅按钮点击事件
            foreach (var button in NumberButtons)
            {
                button.Value.OnClick += (sender) =>
                {
                    OnChooseNumber?.Invoke(button.Key);
                };
            }

        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            ChooseNumberLabel.Draw(spriteBatch);
            foreach (var button in NumberButtons)
            {
                button.Value.Draw(spriteBatch);
            }
        }

        public override void Update(MouseState mouseState)
        {
            foreach (var button in NumberButtons)
            {
                button.Value.Update(mouseState);
            }
        }

        public void UpdateText(string message)
        {
            ChooseNumberLabel.UpdateText(message);
        }
        public override void Hide()
        {
            ChooseNumberLabel.Hide();
            foreach (var button in NumberButtons)
            {
                button.Value.Hide();
            }
        }

        public override void Show()
        {
            ChooseNumberLabel.Show();
            foreach (var button in NumberButtons)
            {
                button.Value.Show();
            }
        }

        public void Enable()
        {
            foreach (var button in NumberButtons)
            {
                button.Value.Enable();
            }
        }
    }
}
