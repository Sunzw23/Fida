using System.Collections.Generic;
using Fida.Logic;
using Fida.Scene;
using Xunit;

namespace Fida.Tests;

public class AutomatedActionPresenterTests
{
    [Fact]
    public void Presenter_WaitsForRenderedTimeBeforeRequestingAction()
    {
        var game = new GameManager();
        var controller = new RecordingAutomatedController(1);
        var presenter = new AutomatedActionPresenter();
        var args = new PlayerActionRequiredEventArgs
        {
            SourcePlayerId = 1,
            RequiredActionType = ActionType.ChooseNumberIndex
        };

        presenter.Schedule(controller, args, 0.7f);
        presenter.Update(game, 0.69f);
        Assert.Equal(0, controller.RequestCount);
        Assert.True(presenter.HasPendingAction);

        presenter.Update(game, 0.02f);
        Assert.Equal(1, controller.RequestCount);
        Assert.False(presenter.HasPendingAction);
    }

    [Fact]
    public void Presenter_GivesSecondNumberBeatMoreReadingTime()
    {
        var players = new List<Player>
        {
            new(0, "Player", 11, []),
            new(1, "Enemy", 3, [])
        };
        var game = new GameManager();
        game.Initialize(players);
        game.TransitionTo(new ChooseNumberIndexState(game, 1));
        var args = new PlayerActionRequiredEventArgs
        {
            SourcePlayerId = 1,
            RequiredActionType = ActionType.ChooseNumberIndex
        };

        float firstBeat = AutomatedActionPresenter.GetDelay(game, args);
        game.UpdateGame(new GameAction(ActionType.ChooseNumberIndex)
        {
            TargetPlayerId = 1,
            NumberIndex = 0
        });
        float secondBeat = AutomatedActionPresenter.GetDelay(game, args);

        Assert.True(secondBeat > firstBeat);
        Assert.Equal(1.15f, secondBeat);
    }

    private sealed class RecordingAutomatedController : IAutomatedPlayerController
    {
        public int PlayerId { get; }
        public int RequestCount { get; private set; }

        public RecordingAutomatedController(int playerId) => PlayerId = playerId;

        public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args) =>
            RequestCount++;
    }
}
