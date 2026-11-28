using Microsoft.Xna.Framework;
using Fida.UI;

namespace Fida.States
{
    /// <summary>Dedicated Duel ready check. Campaign remains independent from this screen.</summary>
    public class OnlineLobbyState : StateBase
    {
        private bool _gameStarting;
        private double _resendAge;

        public OnlineLobbyState(UI.UIManager uiManager, Network.NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            _gameStarting = false;
            // Defensive reset: a duel always starts from a clean online session, so
            // leftovers from a campaign route cannot leak into it (shared-battle flag,
            // co-op token, pending snapshot, last campaign presence, node selection).
            _networkManager.ResetCampaignSession();
            Logic.GameSetup.ClearCampaignSelection();
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
            // Entering the lobby resets the peer's record of our vote, so a Ready pressed
            // before the peer arrived would otherwise be lost and the two peers would wait
            // for each other forever. Re-assert it whenever the peer reports "no vote".
            if (mode < 0 && !_gameStarting && _networkManager.LocalLobbyVote >= 0)
                _networkManager.SendLobbyVote(_networkManager.LocalLobbyVote);
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
            var art = _uiManager.GetControlByName<UIEditorialSurface>("lobby_art");
            if (art != null)
            {
                art.LocalReady = _networkManager.LocalLobbyVote == 0;
                art.RemoteReady = _networkManager.RemoteLobbyVote == 0;
            }
            var ready = _uiManager.GetControlByName<UIButton>("startPvP");
            if (ready != null)
            {
                ready.Update(_networkManager.LocalLobbyVote == 0 ? "Waiting for opponent" : "Ready");
                ready.EnabledChecker = () => _networkManager.LocalLobbyVote != 0;
            }
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

            // Heartbeat: while we are ready and the peer still reports no vote, repeat our
            // vote about once a second so a dropped or reset packet cannot strand the lobby.
            _resendAge += gameTime.ElapsedGameTime.TotalSeconds;
            if (!_gameStarting && _networkManager.LocalLobbyVote >= 0 &&
                _networkManager.RemoteLobbyVote < 0 && _resendAge >= 1.0)
            {
                _resendAge = 0;
                _networkManager.SendLobbyVote(_networkManager.LocalLobbyVote);
            }

            if (!_gameStarting) return;
            UnsubscribeEvents();
            _stateMachine.SwitchToState(_networkManager.CurrentGameMode switch
            {
                _ => State.DuelDraft
            });
        }
    }
}
