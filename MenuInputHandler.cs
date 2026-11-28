#nullable enable
using System;
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
        /// <summary>
        /// Input handlers by state. Every menu-like state must appear here; battle states are
        /// driven by the session layer instead. <see cref="HandledStates"/> lets tests prove a
        /// new state cannot be added without wiring its input.
        /// </summary>
        private static readonly Dictionary<State, Func<UIManager, NetworkManager, GameStateMachine, IMenuState>>
            Factories = new()
            {
                { State.MainMenu,    (ui, net, machine) => new MainMenuState(ui, net, machine) },
                { State.Paused,      (ui, net, machine) => new PausedState(ui, net, machine) },
                { State.Rules,       (ui, net, machine) => new RulesState(ui, net, machine) },
                { State.Achievements,(ui, net, machine) => new AchievementsState(ui, net, machine) },
                { State.Settings,    (ui, net, machine) => new SettingsState(ui, net, machine) },
                { State.LevelSelect, (ui, net, machine) => new LevelSelectState(ui, net, machine) },
                { State.CompanionSelect, (ui, net, machine) => new CompanionSelectState(ui, net, machine) },
                { State.Skills,      (ui, net, machine) => new SkillSelectState(ui, net, machine) },
                { State.GameOver,    (ui, net, machine) => new GameOverState(ui, net, machine) },
                { State.OnlineMenu,  (ui, net, machine) => new OnlineMenuState(ui, net, machine) },
                { State.Hosting,     (ui, net, machine) => new HostingState(ui, net, machine) },
                { State.Connecting,  (ui, net, machine) => new ConnectingState(ui, net, machine) },
                { State.OnlineLobby, (ui, net, machine) => new OnlineLobbyState(ui, net, machine) },
                { State.DuelDraft,   (ui, net, machine) => new DuelDraftState(ui, net, machine) },
                { State.OnlineCampaignMap, (ui, net, machine) => new OnlineCampaignMapState(ui, net, machine) },
                { State.OnlineCompanionSelect, (ui, net, machine) => new OnlineCompanionSelectState(ui, net, machine) },
                { State.OnlineCampaignSkills, (ui, net, machine) => new OnlineCampaignSkillsState(ui, net, machine) },
            };

        /// <summary>States that own a menu input handler (everything except the battle states).</summary>
        public static IReadOnlyCollection<State> HandledStates => Factories.Keys;

        private readonly UIManager _uiManager;
        private readonly Dictionary<State, IMenuState> _states;
        private readonly GameStateMachine _stateMachine;

        public MenuInputHandler(UIManager uiManager, NetworkManager networkManager, GameStateMachine stateMachine)
        {
            _uiManager = uiManager;
            _stateMachine = stateMachine;

            _states = new Dictionary<State, IMenuState>();
            foreach (var pair in Factories)
                _states[pair.Key] = pair.Value(uiManager, networkManager, stateMachine);

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
            if (_states.TryGetValue(_stateMachine.CurrentState, out var handler)) handler.OnEnter();
        }

        /// <summary>
        /// 获取当前状态对应的处理器；未注册的状态返回 null（画面至少不会因此冻结或崩溃）
        /// </summary>
        private IMenuState? CurrentStateHandler =>
            _states.TryGetValue(_stateMachine.CurrentState, out var handler) ? handler : null;

        /// <summary>
        /// 通用菜单输入处理（MainMenu / Paused / OnlineMenu / Hosting / Connecting）
        /// </summary>
        public void HandleMenuInput(GameTime gameTime) => CurrentStateHandler?.HandleInput(gameTime);

        /// <summary>
        /// 处理 Hosting 状态输入
        /// </summary>
        public void HandleHostingInput(GameTime gameTime) => CurrentStateHandler?.HandleInput(gameTime);

        /// <summary>
        /// 处理 Connecting 状态输入
        /// </summary>
        public void HandleConnectingInput(GameTime gameTime) => CurrentStateHandler?.HandleInput(gameTime);
    }
}
