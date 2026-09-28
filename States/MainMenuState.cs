using System;
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>Main menu navigation, including the connected-session shortcuts.</summary>
    public sealed class MainMenuState : StateBase
    {
        public MainMenuState(UI.UIManager uiManager, Network.NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            if (_networkManager.IsConnected)
            {
                // Campaign exploration belongs to each player: entering the map is local,
                // and coordination happens only when players meet at a node/loadout.
                BindButton("startGame", () =>
                {
                    _networkManager.CurrentGameMode = 2;
                    _stateMachine.SwitchToState(State.OnlineCampaignMap);
                });

                // Duel's shared draft/lobby is where both players confirm the same mode.
                BindButton("duel", () => _stateMachine.SwitchToState(State.OnlineLobby));
                BindButton("connectionSettings", () => _stateMachine.SwitchToState(State.OnlineMenu));
                BindButton("disconnect", () =>
                {
                    _networkManager.Cleanup();
                    _stateMachine.SwitchToState(State.MainMenu);
                });
            }
            else
            {
                BindButton("startGame", () => _stateMachine.SwitchToState(State.LevelSelect));
            }

            BindButton("onlineMode", () => _stateMachine.SwitchToState(
                _networkManager.IsConnected ? State.OnlineLobby : State.OnlineMenu));
            BindButton("settings", () => _stateMachine.SwitchToState(State.Settings));
            BindButton("rules", _stateMachine.OpenRules);
            BindButton("exit", () => Environment.Exit(0));
        }

        public override void HandleInput(GameTime gameTime) => base.HandleInput(gameTime);
    }
}
