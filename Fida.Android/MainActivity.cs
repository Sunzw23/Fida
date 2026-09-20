#nullable enable
using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;

namespace Fida.Android;

[Activity(Label = "Fida", MainLauncher = true,
    // The Android UI is designed around a single-column portrait layout.
    // Keeping the activity locked avoids a mid-battle resize that would move
    // cards and modal controls underneath the user's finger.
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = global::Android.Content.PM.ConfigChanges.Orientation |
                           global::Android.Content.PM.ConfigChanges.ScreenSize |
                           global::Android.Content.PM.ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : AndroidGameActivity
{
    private global::Fida.Game1? _game;

    protected override void OnCreate(Bundle? bundle)
    {
        base.OnCreate(bundle);
        AndroidAssetContent.Initialize(Assets!);
        _game = new global::Fida.Game1();
        SetContentView((global::Android.Views.View)_game.Services.GetService(typeof(global::Android.Views.View))!);
        _game.Run();
    }
}
