using System;
using Microsoft.Xna.Framework;

namespace Fida.UI;

/// <summary>Small, frame-rate independent motion primitives shared by the UI.</summary>
public static class UIMotion
{
    public static bool Reduced => DisplaySettingsManager.Current.ReduceMotion;
    public static float Approach(float value, float target, double seconds, float speed = 14f) =>
        Reduced ? target : MathHelper.Lerp(value, target,
            1f - MathF.Exp(-speed * (float)Math.Max(0, seconds)));
    public static float Reveal(double seconds, float duration = 0.32f) =>
        Reduced ? 1f : MathHelper.SmoothStep(0, 1, MathHelper.Clamp((float)seconds / duration, 0, 1));
    public static float Pulse(double seconds, float speed = 2f) =>
        Reduced ? 0.5f : 0.5f + 0.5f * (float)Math.Sin(seconds * speed);
}
