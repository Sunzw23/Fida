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
            UnbindAll();
            BindButton("fullscreen", () => Apply(() => DisplaySettingsManager.SetFullscreen(true)));
            BindButton("windowed", () => Apply(() => DisplaySettingsManager.SetFullscreen(false)));
            BindButton("resolution", () => Apply(DisplaySettingsManager.CycleResolution));
            BindButton("motion", () => Apply(DisplaySettingsManager.ToggleReducedMotion));
            BindButton("motionFull", () => Apply(() => SetReducedMotion(false)));
            BindButton("motionReduced", () => Apply(() => SetReducedMotion(true)));
            BindButton("back", _stateMachine.ReturnFromSettings);
            BindAudioControls("music", true);
            BindAudioControls("effects", false);
        }

        private void BindAudioControls(string prefix, bool music)
        {
            int Volume() => music ? DisplaySettingsManager.Current.MusicVolume : DisplaySettingsManager.Current.EffectsVolume;
            BindButton(prefix + "Down", () => Apply(() => DisplaySettingsManager.SetAudioVolume(music, Volume() - 10)));
            BindButton(prefix + "Up", () => Apply(() => DisplaySettingsManager.SetAudioVolume(music, Volume() + 10)));
            BindButton(prefix + "Mute", () => Apply(() => DisplaySettingsManager.SetAudioVolume(music, Volume() == 0 ? (music ? 65 : 80) : 0)));
            BindButton(prefix + "Test", () =>
            {
                if (music) Apply(() => DisplaySettingsManager.SetAudioVolume(true, 65));
                else Audio.AudioService.Feedback(Audio.SoundCue.Heal);
            });
        }

        private static void SetReducedMotion(bool reduced)
        {
            if (DisplaySettingsManager.Current.ReduceMotion != reduced)
                DisplaySettingsManager.ToggleReducedMotion();
        }

        private void Apply(Action change)
        {
            var focused = _uiManager.GetControls().Find(c => c.IsFocused)?.Name;
            change();
            _uiManager.BuildSettingsScreen();
            BindControls();
            if (focused != null)
            {
                var control = _uiManager.GetControlByName<UI.UIControl>(focused);
                if (control?.CanFocus == true) { _uiManager.ClearFocus(); control.SetFocus(true); }
            }
        }
    }
}
