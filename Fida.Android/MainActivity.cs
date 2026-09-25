#nullable enable
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace Fida.Android;

[Activity(Label = "Fida", Icon = "@drawable/fida_icon", MainLauncher = true,
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
        var gameView = (View)_game.Services.GetService(typeof(View))!;
        SetContentView(gameView);
        gameView.SetOnApplyWindowInsetsListener(new SafeAreaInsetsListener());
        gameView.RequestApplyInsets();
        _game.Run();
    }

    private sealed class SafeAreaInsetsListener : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(View view, WindowInsets insets)
        {
            if (view.Width <= 0 || view.Height <= 0)
            {
                // The first callback can precede layout. Re-request after the
                // view is attached/measured instead of publishing invalid scale data.
                view.Post(view.RequestApplyInsets);
                return insets;
            }

            int left;
            int top;
            int right;
            int bottom;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
#pragma warning disable CA1416 // Guarded by the Android 11 runtime check above.
                var safeInsets = insets.GetInsets(
                    WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
                left = safeInsets.Left;
                top = safeInsets.Top;
                right = safeInsets.Right;
                bottom = safeInsets.Bottom;
#pragma warning restore CA1416
            }
            else
            {
#pragma warning disable CA1422 // Required compatibility path for Android 8-10.
                left = insets.SystemWindowInsetLeft;
                top = insets.SystemWindowInsetTop;
                right = insets.SystemWindowInsetRight;
                bottom = insets.SystemWindowInsetBottom;
#pragma warning restore CA1422
                if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
                {
#pragma warning disable CA1416 // Guarded by the Android 9 runtime check above.
                    var cutout = insets.DisplayCutout;
                    if (cutout != null)
                    {
                        left = System.Math.Max(left, cutout.SafeInsetLeft);
                        top = System.Math.Max(top, cutout.SafeInsetTop);
                        right = System.Math.Max(right, cutout.SafeInsetRight);
                        bottom = System.Math.Max(bottom, cutout.SafeInsetBottom);
                    }
#pragma warning restore CA1416
                }
            }

            global::Fida.UI.UIMobileSafeArea.SetInsets(left, top, right, bottom,
                view.Width, view.Height);
            return insets;
        }
    }
}
