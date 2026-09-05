using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Fida.Network;
using Fida.States;
using Fida.UI;

namespace Fida
{
    /// <summary>
    /// 菜单输入处理 - 使用状态模式委托给具体的状态类
    /// 通过 GameStateMachine 的生命周期事件自动管理 OnEnter/OnExit
    /// </summary>
    public class MenuInputHandler
    {
        private readonly UIManager _uiManager;
        private readonly Dictionary<State, IMenuState> _states;
        private readonly GameStateMachine _stateMachine;

        public MenuInputHandler(UIManager uiManager, NetworkManager networkManager, GameStateMachine stateMachine)
        {
            _uiManager = uiManager;
            _stateMachine = stateMachine;

            _states = new Dictionary<State, IMenuState>
            {
                { State.MainMenu,    new MainMenuState(uiManager, networkManager, stateMachine) },
                { State.Paused,      new PausedState(uiManager, networkManager, stateMachine) },
                { State.LevelSelect, new LevelSelectState(uiManager, networkManager, stateMachine) },
                { State.Skills,      new SkillSelectState(uiManager, networkManager, stateMachine) },
                { State.OnlineMenu,  new OnlineMenuState(uiManager, networkManager, stateMachine) },
                { State.Hosting,     new HostingState(uiManager, networkManager, stateMachine) },
                { State.Connecting,  new ConnectingState(uiManager, networkManager, stateMachine) },
                { State.OnlineLobby, new OnlineLobbyState(uiManager, networkManager, stateMachine) },
            };

            // 订阅状态机生命周期事件
            stateMachine.OnStateExiting += OnStateExiting;
            stateMachine.OnStateEntered += OnStateEntered;
        }

        private void OnStateExiting(State oldState)
        {
            if (_states.TryGetValue(oldState, out var handler))
            {
                handler.OnExit();
            }
        }

        private void OnStateEntered(State newState)
        {
            if (_states.TryGetValue(newState, out var handler))
            {
                handler.OnEnter();
            }
        }

        /// <summary>
        /// 初始状态进入（在 UI 构建完成后调用）
        /// </summary>
        public void OnInitialEnter()
        {
            _states[_stateMachine.CurrentState].OnEnter();
        }

        /// <summary>
        /// 获取当前状态对应的处理器
        /// </summary>
        private IMenuState CurrentStateHandler => _states[_stateMachine.CurrentState];

        /// <summary>
        /// 通用菜单输入处理（MainMenu / Paused / OnlineMenu / Hosting / Connecting）
        /// </summary>
        public void HandleMenuInput(GameTime gameTime)
        {
            CurrentStateHandler.HandleInput(gameTime);
        }

        /// <summary>
        /// 处理 Hosting 状态输入
        /// </summary>
        public void HandleHostingInput(GameTime gameTime)
        {
            CurrentStateHandler.HandleInput(gameTime);
        }

        /// <summary>
        /// 处理 Connecting 状态输入
        /// </summary>
        public void HandleConnectingInput(GameTime gameTime)
        {
            CurrentStateHandler.HandleInput(gameTime);
        }
    }
}