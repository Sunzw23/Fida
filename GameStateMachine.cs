using System;
using Microsoft.Xna.Framework;
using Fida.Network;
using Fida.Scene;
using Fida.UI;

namespace Fida
{
    /// <summary>
    /// 游戏状态机 - 负责状态切换和 UI 构建
    /// </summary>
    public class GameStateMachine
    {
        private readonly UIManager _uiManager;
        private readonly PlayingSceneManager _playingSceneManager;
        private readonly NetworkManager _networkManager;

        public State CurrentState { get; private set; }

        /// <summary>
        /// 状态即将切换时触发（切换前调用旧状态的 OnExit）
        /// </summary>
        public event Action<State> OnStateExiting;

        /// <summary>
        /// 状态已切换时触发（切换后调用新状态的 OnEnter）
        /// </summary>
        public event Action<State> OnStateEntered;

        public GameStateMachine(UIManager uiManager, PlayingSceneManager playingSceneManager, NetworkManager networkManager)
        {
            _uiManager = uiManager;
            _playingSceneManager = playingSceneManager;
            _networkManager = networkManager;
            CurrentState = State.MainMenu;
        }

        /// <summary>
        /// 返回主菜单（清理网络资源）
        /// </summary>
        public void GoToMainMenu()
        {
            _networkManager.Cleanup();
            _playingSceneManager.IsInitialized = false;
            SwitchToState(State.MainMenu);
        }

        /// <summary>
        /// 切换到新状态并构建对应的 UI
        /// </summary>
        public void SwitchToState(State newState)
        {
            if (newState == CurrentState) return;

            // 通知旧状态即将退出
            OnStateExiting?.Invoke(CurrentState);

            CurrentState = newState;
            _uiManager.Clear();

            switch (newState)
            {
                case State.MainMenu:      _uiManager.BuildMainMenu(); break;
                case State.Rules:         _uiManager.BuildRulesScreen(); break;
                case State.Settings:      _uiManager.BuildSettingsScreen(); break;
                case State.Playing:
                    if (!_playingSceneManager.IsInitialized)
                    {
                        _playingSceneManager.Initialize(SceneConfig.PlayersScene1.Players);
                        _playingSceneManager.StartGame();
                    }
                    else _uiManager.BuildPlayingUI(_uiManager.GameManager);
                    break;
                case State.Paused:        _uiManager.BuildPauseMenu(); break;
                case State.GameOver:      break;
                case State.OnlineMenu:    _uiManager.BuildOnlineMenu(); break;
                case State.Hosting:       _uiManager.BuildHostingScreen(_networkManager.StatusText, _networkManager.HostRoomCode); break;
                case State.Connecting:    _uiManager.BuildConnectingScreen(_networkManager.StatusText); break;
                case State.OnlineLobby:   _uiManager.BuildOnlineLobby(_networkManager.IsHost); break;
                case State.OnlinePlaying: _uiManager.BuildPlayingUI(_uiManager.GameManager); break;
            }

            // 通知新状态已进入
            OnStateEntered?.Invoke(newState);
        }
    }
}