using Fida.Campaign;
using Fida.Campaign.Battle;
using Fida.Campaign.Infrastructure;
using Fida.Logic;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;
using Xunit;

namespace Fida.Tests;

public class CampaignTests
{
    [Fact]
    public void SkillWheel_WrapsAndKeepsExactlyOneFocusedItem()
    {
        var wheel = new SkillWheelModel(
            [SkillType.PlusAttack, SkillType.PlusDefence, SkillType.DoubleHealth]);

        Assert.Equal(SkillType.PlusAttack, wheel.Focused);
        wheel.Move(-1);
        Assert.Equal(SkillType.DoubleHealth, wheel.Focused);
        wheel.Move(2);
        Assert.Equal(SkillType.PlusDefence, wheel.Focused);
        Assert.Equal(wheel.Focused, wheel.GetAtOffset(0));
    }

    [Fact]
    public void LoadoutAvailability_ExplainsWheelActions()
    {
        var progress = new CampaignProgress
        {
            UnlockedSkills =
            [
                SkillType.PlusAttack, SkillType.PlusDefence, SkillType.DoubleHealth,
                SkillType.TripleHealth, SkillType.DoubleAttack
            ]
        };
        var node = new CampaignNode { SkillBudget = 2 };

        Assert.Equal(CampaignSkillAvailability.Available,
            CampaignLoadout.Evaluate(progress, node, [], SkillType.PlusAttack));
        Assert.Equal(CampaignSkillAvailability.Selected,
            CampaignLoadout.Evaluate(progress, node, [SkillType.PlusAttack], SkillType.PlusAttack));
        Assert.Equal(CampaignSkillAvailability.Locked,
            CampaignLoadout.Evaluate(progress, node, [], SkillType.Shen));
        Assert.Equal(CampaignSkillAvailability.OverBudget,
            CampaignLoadout.Evaluate(progress, node, [], SkillType.DoubleAttack));
        Assert.Equal(CampaignSkillAvailability.Conflicting,
            CampaignLoadout.Evaluate(progress, node, [SkillType.DoubleHealth], SkillType.TripleHealth));
    }

    [Fact]
    public void MapCamera_FitsContentIntoAnyViewport()
    {
        var camera = new CampaignMapCamera(new Rectangle(0, 0, 1000, 500), 0, 0.1f, 3f);
        camera.SetViewport(new Rectangle(0, 0, 800, 600));

        Vector2 topLeft = camera.WorldToScreen(Vector2.Zero);
        Vector2 bottomRight = camera.WorldToScreen(new Vector2(1000, 500));
        Assert.InRange(topLeft.X, 0f, 800f);
        Assert.InRange(topLeft.Y, 0f, 600f);
        Assert.InRange(bottomRight.X, 0f, 800f);
        Assert.InRange(bottomRight.Y, 0f, 600f);
    }

    [Fact]
    public void MapCamera_ZoomKeepsPointerAnchoredToSameWorldPoint()
    {
        var camera = new CampaignMapCamera(new Rectangle(0, 0, 2000, 1200), 200, 0.25f, 4f);
        camera.SetViewport(new Rectangle(100, 80, 900, 600));
        var pointer = new Vector2(720, 330);
        Vector2 before = camera.ScreenToWorld(pointer);

        camera.ZoomAt(1.5f, pointer);

        Vector2 after = camera.ScreenToWorld(pointer);
        Assert.InRange(Vector2.Distance(before, after), 0f, 0.01f);
    }

    [Fact]
    public void MapNavigator_UsesSpatialDirectionInsteadOfCatalogOrder()
    {
        CampaignNode[] nodes =
        [
            new() { Id = "Center", MapX = 500, MapY = 500 },
            new() { Id = "FarRight", MapX = 900, MapY = 500 },
            new() { Id = "NearestRight", MapX = 620, MapY = 510 },
            new() { Id = "Above", MapX = 500, MapY = 300 }
        ];

        Assert.Equal(2, CampaignMapNavigator.FindNext(nodes, 0, Vector2.UnitX));
        Assert.Equal(3, CampaignMapNavigator.FindNext(nodes, 0, -Vector2.UnitY));
    }

    [Fact]
    public void CampaignDisplayText_UsesNoCjkCharacters()
    {
        static void AssertEnglishText(string text) =>
            Assert.DoesNotMatch("[\\u3400-\\u9fff]", text);

        foreach (var node in CampaignCatalog.Nodes)
        {
            AssertEnglishText(node.Chapter);
            AssertEnglishText(node.Name);
            AssertEnglishText(node.Story);
            AssertEnglishText(node.RewardText);
            Assert.All(node.Enemies, enemy => AssertEnglishText(enemy.Name));
        }
    }

    [Fact]
    public void Catalog_HasUniqueNodesAndValidParents()
    {
        Assert.True(CampaignGraphValidator.IsValid(CampaignCatalog.Nodes));
        Assert.Equal(18, CampaignCatalog.Nodes.Count);
        Assert.Equal(CampaignCatalog.Nodes.Count, CampaignCatalog.Nodes.Select(n => n.Id).Distinct().Count());
        var ids = CampaignCatalog.Nodes.Select(n => n.Id).ToHashSet();
        Assert.All(CampaignCatalog.Nodes.SelectMany(n => n.Parents), parent => Assert.Contains(parent, ids));
    }

    [Fact]
    public void GraphValidator_RejectsCycles()
    {
        CampaignNode[] nodes =
        [
            new() { Id = "X", Parents = ["Y"], RequiredCompletedParents = 1, Enemies = [new CampaignEnemyDefinition()] },
            new() { Id = "Y", Parents = ["X"], RequiredCompletedParents = 1, Enemies = [new CampaignEnemyDefinition()] }
        ];

        Assert.False(CampaignGraphValidator.IsValid(nodes));
    }

    [Fact]
    public void General_UnlocksAfterAnyTwoFirstChapterNodes()
    {
        var progress = new CampaignProgress();
        progress.Complete(CampaignCatalog.GetNode("P"));
        progress.Complete(CampaignCatalog.GetNode("A1"));
        Assert.False(CampaignCatalog.IsUnlocked(progress, CampaignCatalog.GetNode("AG")));

        progress.Complete(CampaignCatalog.GetNode("A3"));
        Assert.True(CampaignCatalog.IsUnlocked(progress, CampaignCatalog.GetNode("AG")));
    }

    [Fact]
    public void HighImpactSkills_CostMoreThanBasicSkills()
    {
        int basic = CampaignSkillBalance.Cost(SkillType.PlusAttack);
        Assert.True(CampaignSkillBalance.Cost(SkillType.Shen) > basic);
        Assert.True(CampaignSkillBalance.Cost(SkillType.Di) > basic);
        Assert.True(CampaignSkillBalance.Cost(SkillType.Bian) > basic);
        Assert.Equal(CampaignSkillBalance.Cost(SkillType.Long), CampaignSkillBalance.Cost(SkillType.Bian));
    }

    [Fact]
    public void Loadout_EnforcesBudgetUnlockAndHealthConflict()
    {
        var progress = new CampaignProgress
        {
            UnlockedSkills = [SkillType.DoubleHealth, SkillType.TripleHealth, SkillType.Long]
        };
        var node = CampaignCatalog.GetNode("A1"); // budget 3
        var selected = new HashSet<SkillType> { SkillType.DoubleHealth };

        Assert.False(CampaignLoadout.CanSelect(progress, node, selected, SkillType.TripleHealth));
        Assert.False(CampaignLoadout.CanSelect(progress, node, selected, SkillType.Long));
        Assert.False(CampaignLoadout.CanSelect(progress, node, selected, SkillType.PlusAttack));
    }

    [Fact]
    public void Loadout_PreventsTwoDamageReplacementSkills()
    {
        var progress = new CampaignProgress { UnlockedSkills = [SkillType.Long, SkillType.Bian] };
        var selected = new HashSet<SkillType> { SkillType.Long };

        Assert.False(CampaignLoadout.CanSelect(progress, CampaignCatalog.GetNode("F"), selected, SkillType.Bian));
    }

    [Fact]
    public void BattleFactory_GroupsInitialEnemiesAndKeepsReinforcementsInReserve()
    {
        var setup = CampaignBattleFactory.Create(CampaignCatalog.GetNode("AG"), []);

        Assert.Equal(3, setup.Players.Count);
        Assert.Equal(2, setup.Reinforcements.Count);
        Assert.Single(setup.Groups[0].Players);
        Assert.Equal(2, setup.Groups[1].Players.Count);
        Assert.All(setup.Groups[1].Players, p => Assert.NotEqual(0, p.PlayerId));
    }

    [Fact]
    public void BattleFactory_AssignsDataDrivenPortraitKeysWithEnemyFallback()
    {
        var prologue = CampaignBattleFactory.Create(CampaignCatalog.GetNode("P"), []);
        var laterBattle = CampaignBattleFactory.Create(CampaignCatalog.GetNode("A1"), []);

        Assert.Equal("arithmetist", prologue.Players[0].PortraitKey);
        Assert.Equal("withered_king", prologue.Players[1].PortraitKey);
        Assert.All(laterBattle.Players.Skip(1),
            enemy => Assert.Equal("enemy_fallback", enemy.PortraitKey));
    }

    [Fact]
    public void EncounterCoordinator_DeploysReinforcementsBeforeVictoryCheck()
    {
        var setup = CampaignBattleFactory.Create(CampaignCatalog.GetNode("AG"), []);
        var game = new GameManager();
        game.Initialize(setup.Players, setup.Groups);
        var coordinator = new CampaignEncounterCoordinator(game);
        coordinator.Configure(setup);

        setup.Players[1].TakeDamage(setup.Players[1].Health);

        Assert.Equal(5, game.Players.Count);
        Assert.Equal(4, game.Groups[1].Players.Count);
    }

    [Fact]
    public void TargetSelection_UsesCampaignTeamsForAttackAndHealing()
    {
        var setup = CampaignBattleFactory.Create(CampaignCatalog.GetNode("A1"), []);
        var game = new GameManager();
        game.Initialize(setup.Players, setup.Groups);
        PlayerActionRequiredEventArgs? requested = null;
        game.PlayerActionRequired += (_, e) => requested = e;

        new TargetSelectionState(game, new SkillContext(1, damage: 1)).Enter();
        Assert.Equal([0, 1], requested!.ValidTargets);
        Assert.DoesNotContain(2, requested.ValidTargets);

        new TargetSelectionState(game, new SkillContext(1, damage: 0, healing: 1)).Enter();
        Assert.DoesNotContain(0, requested!.ValidTargets);
        Assert.Contains(1, requested.ValidTargets);
        Assert.Contains(2, requested.ValidTargets);
    }

    [Fact]
    public void ProgressStore_RoundTripsCompletedNodesAndSkills()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fida_campaign_{Guid.NewGuid():N}.json");
        try
        {
            var store = new CampaignProgressStore(path);
            var progress = new CampaignProgress();
            progress.Complete(CampaignCatalog.GetNode("P"));
            Assert.True(store.Save(progress));

            var loaded = store.Load();
            Assert.True(loaded.IsCompleted("P"));
            Assert.Contains(SkillType.PlusAttack, loaded.UnlockedSkills);
            Assert.Contains(SkillType.PlusDefence, loaded.UnlockedSkills);
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    [Fact]
    public void JsonProvider_LoadsAdditionalNodeWithoutUiChanges()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fida_nodes_{Guid.NewGuid():N}.json");
        try
        {
            System.IO.File.WriteAllText(path, """
                [{"id":"X1","chapter":"扩展章","name":"测试节点","story":"test","skillBudget":4,
                  "mapX":10,"mapY":20,"parents":["P"],"requiredCompletedParents":1,
                  "rewards":["PlusAttack"],"enemies":[]}]
                """);
            var nodes = new JsonCampaignNodeProvider(path).Load();

            Assert.Single(nodes);
            Assert.Equal("X1", nodes[0].Id);
            Assert.Contains(SkillType.PlusAttack, nodes[0].Rewards);
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    [Fact]
    public void JsonProvider_LoadsAndCombinesChapterDirectoryInFileNameOrder()
    {
        string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fida_chapters_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "20-second.json"),
                "[{\"id\":\"B\",\"enemies\":[{}]}]");
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "10-first.json"),
                "[{\"id\":\"A\",\"enemies\":[{}]}]");

            var nodes = new JsonCampaignNodeProvider(directory).Load();

            Assert.Equal(new[] { "A", "B" }, nodes.Select(node => node.Id));
        }
        finally
        {
            System.IO.Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void JsonProvider_ReportsMalformedCampaignData()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fida_invalid_{Guid.NewGuid():N}.json");
        try
        {
            System.IO.File.WriteAllText(path, "[{ invalid json }]");

            var exception = Assert.Throws<System.IO.InvalidDataException>(
                () => new JsonCampaignNodeProvider(path).Load());

            Assert.Contains(path, exception.Message);
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}
