// UIPanel.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// 菜单霓虹面板控件：统一绘制动态科技边框和高对比深色底板。
    /// 非交互控件（不可聚焦、无鼠标逻辑），仅用于菜单屏做底衬，
    /// 须排在按钮/标题等控件之前绘制。对局屏不使用此控件。
    /// </summary>
    public class UIPanel : UIControl
    {
        private Texture2D _pixel;

        public UIPanel(int id, Texture2D pixel) : base(id)
        {
            _pixel = pixel;
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            if (!IsVisible) return;
            var viewport = sb.GraphicsDevice.Viewport;
            UITheme.DrawMenuPanel(sb, _pixel, viewport, gameTime);
        }
    }
}
