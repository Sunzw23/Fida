using Fida.Campaign;
using Fida.Campaign.Battle;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public sealed class CampaignStoryTests
{
    private static CampaignNode Node(string id) => Assert.IsType<CampaignNode>(CampaignCatalog.GetNode(id));
    [Fact]
    public void LongRoad_StartsAtPalaceThenBenchThenRoad()
    {
        var progress = new CampaignProgress();
        Assert.True(CampaignCatalog.IsUnlocked(progress, Node("P")));
        Assert.False(CampaignCatalog.IsUnlocked(progress, Node("T01")));
        progress.Complete(Node("P"));
        Assert.True(CampaignCatalog.IsUnlocked(progress, Node("T01")));
        Assert.False(CampaignCatalog.IsUnlocked(progress, Node("A1")));
        foreach (string id in new[] { "T01", "T02", "T03" }) progress.Complete(Node(id));
        Assert.All(new[] { "A1", "A2", "A3" }, id =>
            Assert.True(CampaignCatalog.IsUnlocked(progress, Node(id))));
        Assert.Contains(Node("P").AfterStory, page => page.Text.Contains("white bench"));
        Assert.Single(CampaignBattleFactory.Create(Node("P"), []).Groups[0].Players);
    }

    [Fact]
    public void EveryEncounter_HasBriefDialogueAndNoOldRevelation()
    {
        foreach (var node in CampaignCatalog.Nodes)
        {
            Assert.InRange(node.BeforeStory.Count, 1, 4);
            Assert.InRange(node.AfterStory.Count, 1, node.Id == "F" ? 6 : 5);
            Assert.False(node.IsTrueEndingRequired);
            foreach (var page in node.BeforeStory.Concat(node.AfterStory))
            {
                Assert.NotEqual("Arith", page.Speaker);
                Assert.InRange(page.Text.Length, 1, 240);
                Assert.DoesNotContain("Zero Decree", page.Text);
                Assert.DoesNotMatch("[\\u3400-\\u9fff]", page.Text);
            }
        }
        Assert.Equal("Arith", CampaignBattleFactory.Create(Node("P"), []).Players[0].Name);
    }

    [Fact]
    public void Ferry_OffersBothEndingsWithoutCompletionGate()
    {
        var final = Node("F");
        Assert.Equal(new[] { "together", "farewell" }, final.Endings.Select(ending => ending.Id));
        Assert.All(final.Endings, ending =>
        {
            Assert.InRange(ending.Pages.Count, 1, 5);
            Assert.All(ending.Pages, page => Assert.Empty(page.RequiredCompletedNode));
            Assert.All(ending.Pages, page => Assert.NotEqual("Arith", page.Speaker));
        });
    }

    [Fact]
    public void WitheredKing_AttacksHimselfThroughNormalDamageResolution()
    {
        var setup = CampaignBattleFactory.Create(Node("P"), []);
        var king = setup.Players.Single(player => player.Name == setup.Node.SelfTargetingEnemy);
        var game = new GameManager();
        game.Initialize(setup.Players, setup.Groups);
        PlayerActionRequiredEventArgs? request = null;
        int target = -1;
        game.PlayerActionRequired += (_, args) => request = args;
        game.CardActionStarted += (_, args) => target = args.TargetPlayerId;
        game.TransitionTo(new TargetSelectionState(game, new SkillContext(king.PlayerId, damage: 1)));
        new NarrativeTargetController(king.PlayerId).RequestAction(game, request!);
        Assert.Equal(king.PlayerId, target);
        Assert.Equal(11, setup.Players[0].Health);
    }

    [Fact]
    public void WitheredKing_CompletesHisDuelWithoutPlayerAttacks()
    {
        var setup = CampaignBattleFactory.Create(Node("P"), []);
        var game = new GameManager();
        game.Initialize(setup.Players, setup.Groups);
        PlayerActionRequiredEventArgs? pending = null;
        game.PlayerActionRequired += (_, request) => pending = request;
        var king = setup.Players.Single(player => player.Name == setup.Node.SelfTargetingEnemy);
        var controller = new NarrativeTargetController(king.PlayerId);
        game.StartGame();
        for (int i = 0; i < 500 && !game.IsGameEnded; i++)
        {
            var request = Assert.IsType<PlayerActionRequiredEventArgs>(pending);
            pending = null;
            if (request.SourcePlayerId == 0)
                game.UpdateGame(new GameAction(ActionType.SkipTurn) { TargetPlayerId = 0 });
            else controller.RequestAction(game, request);
        }
        Assert.True(game.IsGameEnded);
        Assert.False(king.IsAlive());
        Assert.Equal(11, setup.Players[0].Health);
    }

    [Fact]
    public void EndingHistory_IsOptionalInOldSavesAndPersistsInNewSaves()
    {
        string path = Path.Combine(Path.GetTempPath(), $"fida_story_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"CompletedNodes\":[\"P\"],\"UnlockedSkills\":[]}");
            var store = new CampaignProgressStore(path);
            var progress = store.Load();
            Assert.True(progress.IsCompleted("P"));
            Assert.Empty(progress.SeenEndings);
            progress.SeenEndings.Add("farewell");
            Assert.True(store.Save(progress));
            Assert.Contains("farewell", store.Load().SeenEndings);
        }
        finally { File.Delete(path); }
    }
}
