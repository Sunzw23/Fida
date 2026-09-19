#nullable enable
using Android.App;
using Android.OS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.Android;

[Activity(Label = "Fida", MainLauncher = true,
    ConfigurationChanges = global::Android.Content.PM.ConfigChanges.Orientation |
                           global::Android.Content.PM.ConfigChanges.ScreenSize |
                           global::Android.Content.PM.ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : AndroidGameActivity
{
    private AndroidGame? _game;

    protected override void OnCreate(Bundle? bundle)
    {
        base.OnCreate(bundle);
        _game = new AndroidGame();
        SetContentView((global::Android.Views.View)_game.Services.GetService(typeof(global::Android.Views.View))!);
    }
}

/// <summary>Thin Android host; game rules remain in Fida.Core.</summary>
internal sealed class AndroidGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public AndroidGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }
}
