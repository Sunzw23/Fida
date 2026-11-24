using System;
using System.Collections.Generic;
using System.Linq;
using Fida;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// Guards the wiring between the state machine and the menu input router. A screen whose
/// state is missing here still renders, but its dialogue card and buttons never receive
/// input - which is exactly how the companion screen froze once.
/// </summary>
public class InputRoutingTests
{
    /// <summary>States whose input is driven by GameSessionManager instead of the menu router.</summary>
    private static readonly HashSet<State> BattleStates = new() { State.Playing, State.OnlinePlaying };

    [Fact]
    public void EveryStateIsEitherMenuHandledOrBattleDriven()
    {
        var handled = new HashSet<State>(MenuInputHandler.HandledStates);

        var missing = Enum.GetValues<State>()
            .Where(state => !handled.Contains(state) && !BattleStates.Contains(state))
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void CompanionScreensReceiveInput()
    {
        var handled = MenuInputHandler.HandledStates;

        Assert.Contains(State.CompanionSelect, handled);
        Assert.Contains(State.OnlineCompanionSelect, handled);
    }
}
