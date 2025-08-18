using Fida.UI;
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
    public class UIDicideApply: UI.UIControl
    {
        public event DecideApplyHandler OnDecideApply;
        UILabel DecideLabel;
        UIButton YesButton;
        UIButton NoButton;

        public UIDicideApply(Texture2D pixel, SpriteFont font, string message)
        {
            // 创建决定应用的UI
            UILabel decideLabel = new UILabel(message, UIConfig.ActionInfoPosition, font);
            DecideLabel = decideLabel;
            // 创建是按钮
            UIButton yesButton = new UIButton(UIConfig.YesButtonArea, "是", pixel, font);
            YesButton = yesButton;
            // 创建否按钮
            UIButton noButton = new UIButton(UIConfig.NoButtonArea, "否", pixel, font);
            NoButton = noButton;
            yesButton.Enable();
            noButton.Enable();
            // 订阅按钮的点击事件
            yesButton.OnClick += (sender) => OnDecideApply?.Invoke(true);
            noButton.OnClick += (sender) => OnDecideApply?.Invoke(false);
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            DecideLabel.Draw(spriteBatch);
            YesButton.Draw(spriteBatch);
            NoButton.Draw(spriteBatch);
        }

        public override void Update(MouseState mouseState)
        {
            YesButton.Update(mouseState);
            NoButton.Update(mouseState);
        }

        public void UpdateText(string message)
        {
            DecideLabel.UpdateText(message);
        }
        public override void Hide()
        {
            DecideLabel.Hide();
            YesButton.Hide();
            NoButton.Hide();
        }

        public override void Show()
        {
            DecideLabel.Show();
            YesButton.Show();
            NoButton.Show();
        }

        public void Enable()
        {
            YesButton.Enable();
            NoButton.Enable();
        }
    }
}
