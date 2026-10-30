using System;
using Microsoft.Xna.Framework;

namespace Fida.States
{
    public sealed class SettingsState : StateBase
    {
        public SettingsState(UI.UIManager uiManager, Network.NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter() => BindControls();

        private void BindControls()
        {
            BindButton("fullscreen", () => Apply(() => DisplaySettingsManager.SetFullscreen(true)));
            BindButton("windowed", () => Apply(() => DisplaySettingsManager.SetFullscreen(false)));
            BindButton("resolution", () => Apply(DisplaySettingsManager.CycleResolution));
            BindButton("motion", () => Apply(DisplaySettingsManager.ToggleReducedMotion));
            BindButton("motionFull", () => Apply(() => SetReducedMotion(false)));
            BindButton("motionReduced", () => Apply(() => SetReducedMotion(true)));
            BindButton("back", _stateMachine.ReturnFromSettings);
        }

        private static void SetReducedMotion(bool reduced)
        {
            if (DisplaySettingsManager.Current.ReduceMotion != reduced)
                DisplaySettingsManager.ToggleReducedMotion();
        }

        private void Apply(Action change)
        {
            change();
            _uiManager.BuildSettingsScreen();
            BindControls();
        }
    }
}
