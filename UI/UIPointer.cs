using Microsoft.Xna.Framework.Input;
#if FIDA_ANDROID
using Microsoft.Xna.Framework.Input.Touch;
#endif

namespace Fida.UI;

/// <summary>Normalizes desktop mouse and mobile touch into one pointer state.</summary>
public static class UIPointer
{
#if FIDA_ANDROID
    private static int _lastX;
    private static int _lastY;

    public static MouseState GetState()
    {
        TouchCollection touches = TouchPanel.GetState();
        ButtonState button = ButtonState.Released;
        if (touches.Count > 0)
        {
            TouchLocation touch = touches[0];
            _lastX = (int)touch.Position.X;
            _lastY = (int)touch.Position.Y;
            button = touch.State == TouchLocationState.Released
                ? ButtonState.Released
                : ButtonState.Pressed;
        }

        return new MouseState(_lastX, _lastY, 0, button,
            ButtonState.Released, ButtonState.Released,
            ButtonState.Released, ButtonState.Released);
    }
#else
    public static MouseState GetState() => Mouse.GetState();
#endif
}
