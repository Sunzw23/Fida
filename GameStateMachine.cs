#nullable enable annotations
using System;
using Microsoft.Xna.Framework;
using Fida.Network;
using Fida.Scene;
using Fida.Logic;
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
        private State _settingsReturnState = State.MainMenu;
        private State _rulesReturnState = State.MainMenu;
        public State RulesReturnState => _rulesReturnState;
        public int RulesPage { get; private set; }
        /// <summary>Outcome of the last finished battle, produced by the session manager for the result screen. Null until a battle ends.</summary>
        public Logic.BattleOutcome? LastBattleOutcome { get; private set; }

        public void SetLastBattleOutcome(Logic.BattleOutcome outcome) => LastBattleOutcome = outcome;

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
        /// Returns to the main menu while preserving an established online session.
        /// Explicit disconnect actions own network cleanup.
        /// </summary>
        public void GoToMainMenu()
        {
            _playingSceneManager.IsInitialized = false;
            SwitchToState(State.MainMenu);
        }

        /// <summary>
        /// 切换到新状态并构建对应的 UI
        /// </summary>
        public void SwitchToState(State newState)
        {
            // Main-menu presentation depends on live connection state. Re-entering it
            // after connect/disconnect must rebuild controls even though the enum is unchanged.
            if (newState == CurrentState && newState != State.MainMenu) return;

            // 通知旧状态即将退出
            OnStateExiting?.Invoke(CurrentState);

            if (newState == State.Settings && CurrentState != State.Settings)
                _settingsReturnState = CurrentState;

            CurrentState = newState;
            _uiManager.Clear();

            switch (newState)
            {
                case State.MainMenu:      _uiManager.BuildMainMenu(_networkManager.IsConnected, _networkManager.IsHost); break;
                case State.Rules:         _uiManager.BuildRulesScreen(); break;
                case State.Settings:      _uiManager.BuildSettingsScreen(); break;
                case State.LevelSelect:   _uiManager.BuildLevelSelect(); break;
                case State.Skills:        _uiManager.BuildSkillSelect(); break;
                case State.Playing:
                    if (!_playingSceneManager.IsInitialized)
                    {
                        _playingSceneManager.InitializeCampaign(GameSetup.LoadCampaignBattle());
                        _playingSceneManager.StartGame();
                    }
                    else _uiManager.BuildPlayingUI(_uiManager.GameManager);
                    break;
                case State.Paused:        _uiManager.BuildPauseMenu(); break;
                case State.GameOver:
                    if (LastBattleOutcome is { } outcome)
                    {
                        if (outcome.WasOnlineDuel)
                            _uiManager.BuildDuelResult(outcome.Won, outcome.Message);
                        else
                            _uiManager.BuildCampaignResult(outcome.Won, outcome.Message);
                    }
                    break;
                case State.OnlineMenu:    _uiManager.BuildOnlineMenu(_networkManager.IsConnected); break;
                case State.Hosting:       _uiManager.BuildHostingScreen(_networkManager.StatusText, _networkManager.HostRoomCode); break;
                case State.Connecting:    _uiManager.BuildConnectingScreen(_networkManager.StatusText); break;
                case State.OnlineLobby:   _uiManager.BuildOnlineLobby(_networkManager.IsHost); break;
                case State.DuelDraft:     _uiManager.BuildDuelDraft(); break;
                case State.OnlineCampaignMap: _uiManager.BuildLevelSelect(online: true); break;
                case State.OnlineCampaignSkills: _uiManager.BuildSkillSelect(online: true); break;
                case State.OnlinePlaying: _uiManager.BuildPlayingUI(_uiManager.GameManager); break;
            }

            // 通知新状态已进入
            OnStateEntered?.Invoke(newState);
        }

        public void OpenRules()
        {
            if (CurrentState == State.Rules) return;
            _rulesReturnState = CurrentState;
            RulesPage = 0;
            SwitchToState(State.Rules);
        }

        public void CloseRules() => SwitchToState(
            _rulesReturnState == State.Rules ? State.MainMenu : _rulesReturnState);

        public void SetRulesPage(int page)
        {
            if (CurrentState != State.Rules) return;
            RulesPage = (page % UIManager.RulesPageCount + UIManager.RulesPageCount) %
                UIManager.RulesPageCount;
            _uiManager.BuildRulesScreen(RulesPage);
        }

        public void ReturnFromSettings() => SwitchToState(
            _settingsReturnState == State.Paused ? State.Paused : State.MainMenu);
    }
}
