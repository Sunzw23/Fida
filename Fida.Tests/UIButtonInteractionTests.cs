using Fida.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Xunit;

namespace Fida.Tests;

public class UIButtonInteractionTests
{
    private static UIButton Create(int kind) => kind switch
    {
        1 => new UIEditorialButton(0, new Rectangle(10, 10, 100, 50), "Action", null!, null!),
        2 => new UIIconButton(0, new Rectangle(10, 10, 100, 50), UIIconButton.IconKind.Back, null!, null!),
        _ => new UIButton(0, new Rectangle(10, 10, 100, 50), "Action", null!, null!)
    };
    private static MouseState Mouse(int x, bool down = false) => new(x, 20, 0,
        down ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released,
        ButtonState.Released, ButtonState.Released, ButtonState.Released);

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void DisabledButtonDoesNotReenableOnHover(int kind)
    {
        var button = Create(kind);
        button.Disable();
        button.Update(Mouse(20)); button.Update(Mouse(20, true)); button.Update(Mouse(20));
        Assert.Equal(UIButtonState.Disabled, button.State);
        Assert.False(button.CanFocus);
        Assert.False(button.IsClicked());
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void FocusHighlightsButDoesNotCaptureMouseClicksElsewhere(int kind)
    {
        var button = Create(kind);
        button.SetFocus(true);
        button.Update(Mouse(200));
        Assert.Equal(UIButtonState.Hovered, button.State);
        button.Update(Mouse(200, true)); button.Update(Mouse(200));
        Assert.False(button.IsClicked());
        button.SetFocus(false);
        Assert.Equal(UIButtonState.Normal, button.State);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void DraggingOutCancelsClickEvenWhenFocused(int kind)
    {
        var button = Create(kind);
        button.SetFocus(true);
        button.Update(Mouse(20)); button.Update(Mouse(20, true));
        button.Update(Mouse(200, true)); button.Update(Mouse(20));
        Assert.False(button.IsClicked());
        button.Update(Mouse(20, true)); button.Update(Mouse(20));
        Assert.True(button.IsClicked());
        Assert.False(button.IsClicked());
    }

    [Fact]
    public void AnimatedNumberKeepsSelectionWhenUnavailableWithoutAcceptingInput()
    {
        var button = new UIAnimatedButton(0, new Rectangle(10, 10, 100, 50), "6", null!, null!);
        button.SetSelected(true);
        button.EnabledChecker = () => false;
        button.SetFocus(true);
        button.Update(Mouse(20)); button.Update(Mouse(20, true)); button.Update(Mouse(20));
        Assert.Equal(UIAnimatedButtonState.Selected, button.State);
        Assert.False(button.CanFocus);
        Assert.False(button.IsClicked());
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void ReleaseOutsideDoesNotTurnIntoClickOnReentry(int kind)
    {
        var button = Create(kind);
        button.Update(Mouse(20)); button.Update(Mouse(20, true));
        button.Update(Mouse(200));
        Assert.False(button.IsClicked());
        button.Update(Mouse(20));
        Assert.False(button.IsClicked());
    }

    [Fact]
    public void EnabledCheckerCanRestoreAvailability()
    {
        var button = Create(0);
        bool available = false;
        button.EnabledChecker = () => available;
        button.Update(Mouse(20));
        Assert.Equal(UIButtonState.Disabled, button.State);
        available = true;
        button.Update(Mouse(20));
        Assert.Equal(UIButtonState.Hovered, button.State);
    }
}
