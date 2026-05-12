using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// 菜单状态接口 - 每个菜单状态独立处理自己的按钮逻辑和输入
    /// </summary>
    public interface IMenuState
    {
        /// <summary>
        /// 进入该状态时调用 - 用于绑定按钮事件
        /// </summary>
        void OnEnter();

        /// <summary>
        /// 离开该状态时调用 - 用于解绑按钮事件
        /// </summary>
        void OnExit();

        /// <summary>
        /// 处理该状态的输入（UI 更新 + 按钮点击 + 手柄）
        /// </summary>
        void HandleInput(GameTime gameTime);
    }
}