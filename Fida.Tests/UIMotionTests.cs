using Fida;
using Fida.UI;
using Xunit;

namespace Fida.Tests;

public class UIMotionTests
{
    [Fact]
    public void Motion_IsFrameRateIndependentAndReducedModeSettlesImmediately()
    {
        bool previous = DisplaySettingsManager.Current.ReduceMotion;
        try
        {
            DisplaySettingsManager.Current.ReduceMotion = false;
            float atThirty = 0, atOneTwenty = 0;
            for (int i = 0; i < 30; i++) atThirty = UIMotion.Approach(atThirty, 1, 1d / 30);
            for (int i = 0; i < 120; i++) atOneTwenty = UIMotion.Approach(atOneTwenty, 1, 1d / 120);
            Assert.InRange(System.Math.Abs(atThirty - atOneTwenty), 0, 0.00001f);
            Assert.Equal(0f, UIMotion.Reveal(-1));
            Assert.Equal(1f, UIMotion.Reveal(1));
            DisplaySettingsManager.Current.ReduceMotion = true;
            Assert.Equal(1f, UIMotion.Approach(0, 1, 0));
            Assert.Equal(1f, UIMotion.Reveal(0));
            Assert.Equal(UIMotion.Pulse(0), UIMotion.Pulse(5));
        }
        finally { DisplaySettingsManager.Current.ReduceMotion = previous; }
    }
}
