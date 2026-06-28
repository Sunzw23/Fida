using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Fida.UI;

namespace Fida.States
{
    /// <summary>
    /// 菜单状态抽象基类 - 提供事件驱动的按钮绑定机制
    /// </summary>
    public abstract class StateBase : IMenuState
    {
        protected readonly UIManager _uiManager;
        protected readonly Network.NetworkManager _networkManager;
        protected readonly GameStateMachine _stateMachine;

        /// <summary>
        /// 已绑定的按钮及其事件处理委托，用于在 OnExit 时统一解绑
        /// </summary>
        private readonly List<(UIButton button, Action<UIButton> handler)> _boundButtons
            = new List<(UIButton, Action<UIButton>)>();

        protected StateBase(UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
        {
            _uiManager = uiManager;
            _networkManager = networkManager;
            _stateMachine = stateMachine;
        }

        /// <summary>
        /// 绑定按钮点击事件 - 子类在 OnEnter 中调用
        /// </summary>
        protected void BindButton(string name, Action handler)
        {
            var button = _uiManager.GetButtonByName(name);
            if (button != null)
            {
                Action<UIButton> wrappedHandler = (btn) => handler();
                button.Clicked += wrappedHandler;
                _boundButtons.Add((button, wrappedHandler));
            }
        }

        /// <summary>
        /// 解绑所有已绑定的按钮事件 - 在 OnExit 中调用
        /// </summary>
        protected void UnbindAll()
        {
            foreach (var (button, handler) in _boundButtons)
            {
                button.Clicked -= handler;
            }
            _boundButtons.Clear();
        }

        /// <summary>
        /// 进入状态时调用 - 子类在此绑定按钮事件
        /// </summary>
        public abstract void OnEnter();

        /// <summary>
        /// 离开状态时调用 - 解绑所有按钮事件
        /// </summary>
        public virtual void OnExit()
        {
            UnbindAll();
        }

        /// <summary>
        /// 通用菜单输入处理（UI 更新 + 鼠标点击轮询 + 手柄确认键触发）
        /// </summary>
        public virtual void HandleInput(GameTime gameTime)
        {
            _uiManager.UpdateUIButtons(_uiManager.GameManager);
            _uiManager.UpdateControllerInput(gameTime);

            // 轮询所有按钮的 IsClicked()，触发 Clicked 事件
            // 使用 ToList() 创建快照，防止事件处理中修改集合导致枚举异常
            foreach (var c in _uiManager.GetControls().ToList())
            {
                if (c is UIButton b)
                {
                    b.IsClicked();
                }
            }

            // 处理手柄确认键：触发聚焦按钮的 Confirmed 事件
            var controls = _uiManager.GetControls();
            var controller = controls.Count > 0 ?
                _uiManager.GetControllerInput() : null;
            if (controller != null && controller.IsConnected && controller.IsConfirmPressed())
            {
                // 使用 ToList() 创建快照，防止事件处理中修改集合导致枚举异常
                foreach (var c in controls.ToList())
                {
                    if (c is UIButton b && b.IsFocused)
                    {
                        b.OnConfirmed();
                        break;
                    }
                }
            }
        }
    }
}