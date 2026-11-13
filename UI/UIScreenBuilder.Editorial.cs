using Microsoft.Xna.Framework;

namespace Fida.UI;

public partial class UIScreenBuilder
{
    private UIEditorialLayout EditorialLayout => new(_resourceManager.GraphicsDevice.Viewport);
    private UIEditorialButton EditorialButton(string name, string text, Rectangle bounds, bool primary = false, bool active = false, bool left = false)
    {
        var button = new UIEditorialButton(_uiControls.Count + 100, bounds, text, _resourceManager.Pixel, _resourceManager.LabelFont)
        { Name = name, FocusIndex = _uiControls.Count, Primary = primary, Active = active, AlignLeft = left };
        _uiControls.Add(button);
        return button;
    }
    private void EditorialBack(string name)
    {
        _uiControls.Add(new UIIconButton(90, EditorialLayout.Back, UIIconButton.IconKind.Back, _resourceManager.Pixel, _resourceManager.Font)
        { Name = name, FocusIndex = 0 });
    }
    private void EditorialFocus()
    {
        if (_focusManager.CurrentInputMode == InputMode.Controller) _focusManager.FocusFirst();
    }
    private void BuildEditorialMainMenu(bool connected)
    {
        _uiControls.Add(new UIEditorialSurface(_resourceManager, UIEditorialScreen.Menu) { Name = "menu_art", Connected = connected });
        var items = connected
            ? new[] { ("startGame", "Campaign"), ("duel", "Duel"), ("connectionSettings", "Connection"), ("rules", "Rules"), ("settings", "Settings"), ("disconnect", "Disconnect"), ("exit", "Exit") }
            : new[] { ("startGame", "Campaign"), ("onlineMode", "Online"), ("rules", "Rules"), ("settings", "Settings"), ("exit", "Exit") };
        for (int i = 0; i < items.Length; i++)
            EditorialButton(items[i].Item1, items[i].Item2, EditorialLayout.MenuButton(i, connected), primary: i == 0, left: true).MainMenuStyle = true;
        EditorialFocus();
    }
    private void BuildEditorialSettings()
    {
        _uiControls.Add(new UIEditorialSurface(_resourceManager, UIEditorialScreen.Settings) { Name = "settings_art" });
        EditorialBack("back");
        var l = EditorialLayout;
#if FIDA_ANDROID
        EditorialButton("displayManaged", "Device managed", l.SettingControl(0)).EnabledChecker = () => false;
        EditorialButton("resolutionManaged", "Native resolution", l.SettingControl(1)).EnabledChecker = () => false;
#else
        EditorialButton("windowed", "Windowed", l.SettingControl(0, 0, 2), active: !DisplaySettingsManager.Current.Fullscreen);
        EditorialButton("fullscreen", "Fullscreen", l.SettingControl(0, 1, 2), active: DisplaySettingsManager.Current.Fullscreen);
        EditorialButton("resolution", DisplaySettingsManager.ResolutionLabel + "  >", l.SettingControl(1));
#endif
        EditorialButton("motionFull", "Full", l.SettingControl(2, 0, 2), active: !UIMotion.Reduced);
        EditorialButton("motionReduced", "Reduced", l.SettingControl(2, 1, 2), active: UIMotion.Reduced);
        BuildAudioControls(l, 3, true);
        BuildAudioControls(l, 4, false);
        EditorialFocus();
    }
    private void BuildAudioControls(UIEditorialLayout layout, int row, bool music)
    {
        string prefix = music ? "music" : "effects";
        int volume = music ? DisplaySettingsManager.Current.MusicVolume : DisplaySettingsManager.Current.EffectsVolume;
        EditorialButton(prefix + "Down", "-", layout.SettingControl(row, 0, 4));
        EditorialButton(prefix + "Mute", volume == 0 ? "Off" : volume + "%", layout.SettingControl(row, 1, 4), active: volume == 0);
        EditorialButton(prefix + "Up", "+", layout.SettingControl(row, 2, 4));
        EditorialButton(prefix + "Test", music ? "Reset" : "Test", layout.SettingControl(row, 3, 4));
    }
    private void BuildEditorialLobby(bool isHost)
    {
        _uiControls.Add(new UIEditorialSurface(_resourceManager, UIEditorialScreen.Lobby) { Name = "lobby_art", IsHost = isHost });
        EditorialBack("backToMain");
        EditorialButton("startPvP", "Ready", EditorialLayout.Ready, primary: true);
        EditorialButton("disconnect", "Leave room", EditorialLayout.Leave);
        EditorialFocus();
    }
}


