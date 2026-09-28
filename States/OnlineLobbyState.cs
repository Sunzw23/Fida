using Microsoft.Xna.Framework;
using Fida.UI;

namespace Fida.States
{
    /// <summary>Dedicated Duel ready check. Campaign remains independent from this screen.</summary>
    public class OnlineLobbyState : StateBase
    {
        private bool _gameStarting;

        public OnlineLobbyState(UI.UIManager uiManager, Network.NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            _gameStarting = false;
            _networkManager.ResetLobbyVotes();
            _networkManager.SendLobbyVote(-1);
            _networkManager.LobbyVoteReceived += OnRemoteVote;

            // Both peers see the same single-purpose confirmation.
            BindButton("startPvP", () => ChooseMode(0));

            BindButton("disconnect", () =>
            {
                UnsubscribeEvents();
                CancelDuelVote();
                _networkManager.Cleanup();
                _stateMachine.SwitchToState(State.OnlineMenu);
            });

            BindButton("backToMain", () =>
            {
                UnsubscribeEvents();
                CancelDuelVote();
                _stateMachine.SwitchToState(State.MainMenu);
            });

            UpdateStatus();
        }

        public override void OnExit()
        {
            UnsubscribeEvents();
            base.OnExit();
        }

        private void ChooseMode(int mode)
        {
            _networkManager.LocalLobbyVote = mode;
            _networkManager.SendLobbyVote(mode);
            UpdateStatus();
            TryStartMatchedMode();
        }

        private void OnRemoteVote(int mode)
        {
            UpdateStatus();
            TryStartMatchedMode();
        }

        private void TryStartMatchedMode()
        {
            int local = _networkManager.LocalLobbyVote;
            if (_gameStarting || local != 0 || local != _networkManager.RemoteLobbyVote) return;

            _networkManager.CurrentGameMode = local;
            _gameStarting = true;
        }

        private void CancelDuelVote()
        {
            if (_networkManager.LocalLobbyVote != 0) return;
            _networkManager.LocalLobbyVote = -1;
            _networkManager.SendLobbyVote(-1);
        }

        private void UpdateStatus()
        {
            _uiManager.GetControlByName<UILabel>("lobbyStatus")?.Update(
                Network.NetworkManager.GetLobbyVoteStatusText(
                    _networkManager.LocalLobbyVote, _networkManager.RemoteLobbyVote));
        }

        private void UnsubscribeEvents() => _networkManager.LobbyVoteReceived -= OnRemoteVote;

        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);
            _networkManager.ProcessLobbyMessages();
            UpdateStatus();

            if (!_gameStarting) return;
            UnsubscribeEvents();
            _stateMachine.SwitchToState(_networkManager.CurrentGameMode switch
            {
                _ => State.DuelDraft
            });
        }
    }
}
