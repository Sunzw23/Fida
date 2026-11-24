using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class ChapterExpansionTests
{
    private static CampaignNode Node(string id) => Assert.IsType<CampaignNode>(CampaignCatalog.GetNode(id));

    private static CampaignEnemyDefinition Enemy(int health = 4) =>
        new() { Name = "Test", Health = health };

    private static CampaignNode SideNode(string id, int tier, int units, int totalHealth,
        CampaignVow[]? vows = null, int budget = 3)
    {
        int perEnemy = Math.Max(1, totalHealth / Math.Max(1, units));
        int remainder = Math.Max(0, totalHealth - perEnemy * units);
        return new CampaignNode
        {
            Id = id,
            Chapter = "Chapter I",
            Tier = tier,
            SkillBudget = budget,
            Vows = vows ?? Array.Empty<CampaignVow>(),
            Enemies = Enumerable.Range(0, units)
                .Select(index => Enemy(perEnemy + (index < remainder ? 1 : 0))).ToArray()
        };
    }

    [Fact]
    public void ShippedContent_SatisfiesChaptersAndDifficultyContract()
    {
        var problems = CampaignGraphValidator.Validate(CampaignCatalog.Nodes);

        Assert.Empty(problems);
        Assert.NotEmpty(CampaignChapters.All);
        foreach (var node in CampaignCatalog.Nodes)
        {
            Assert.NotNull(CampaignChapters.ForNode(node));
            Assert.Contains(CampaignChapters.PaletteFor(node), CampaignChapters.Palettes);
        }
    }

    [Fact]
    public void SideChapter_IsResolvedFromTheNodeChapterString()
    {
        var smith = Assert.IsType<CampaignChapter>(
            CampaignChapters.Get(new[] { "The Smith's Errand" }));

        Assert.Equal(CampaignChapterKind.Side, smith.Kind);
        Assert.True(smith.IsSide);
        Assert.Equal("frost", CampaignChapters.PaletteFor(Node("AS1")));
        Assert.Equal(CampaignChapterKind.Side, CampaignChapters.KindFor(Node("AS3")));
        Assert.Equal(CampaignChapterKind.Main, CampaignChapters.KindFor(Node("A1")));
        Assert.Equal("paper", CampaignChapters.PaletteFor(Node("A1")));
        Assert.False(string.IsNullOrWhiteSpace(smith.Summary));
        Assert.Equal("The Smith's Errand", smith.DisplayTitle);
    }

    [Fact]
    public void Mainline_MayNotBeGatedByASideNode()
    {
        var gated = new CampaignNode
        {
            Id = "ZZ",
            Chapter = "Chapter I",
            Parents = ["AS3"],
            RequiredCompletedParents = 1,
            Enemies = [Enemy()]
        };
        var nodes = CampaignCatalog.Nodes.Append(gated).ToArray();

        Assert.Contains(CampaignGraphValidator.Validate(nodes),
            problem => problem.Contains("gated by side node", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(3, 2, 10, "units")]        // tier 3 needs 3-4 units
    [InlineData(4, 3, 20, "units")]        // tier 4 needs 4 units
    [InlineData(1, 3, 14, "health")]       // tier 1 is a short fight
    [InlineData(2, 3, 20, "health")]       // tier 2 is not a boss fight
    public void Difficulty_RejectsNodesOutsideTheirBand(int tier, int units, int health, string expected)
    {
        var problems = CampaignDifficulty.Audit(SideNode("S", tier, units, health));

        Assert.Contains(problems, problem => problem.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void Difficulty_BareHandsIsNarrowerThanAnyBand()
    {
        var greedy = SideNode("S", 3, 2, 8, new[] { CampaignVow.BareHands }, budget: 3);
        var crowded = SideNode("S", 3, 4, 16, new[] { CampaignVow.BareHands }, budget: 0);
        var wounded = new CampaignNode
        {
            Id = "S", Chapter = "Chapter I", Tier = 3, SkillBudget = 0,
            Vows = [CampaignVow.BareHands, CampaignVow.Wounded], PlayerHealth = 8,
            Enemies = [Enemy(5), Enemy(5), Enemy(5)]
        };
        var fine = SideNode("S", 3, 3, 15, new[] { CampaignVow.BareHands }, budget: 0);

        Assert.Contains(CampaignDifficulty.Audit(greedy), problem => problem.Contains("skillBudget"));
        Assert.Contains(CampaignDifficulty.Audit(crowded), problem => problem.Contains("at most 3 units"));
        Assert.Contains(CampaignDifficulty.Audit(wounded), problem => problem.Contains("wounded"));
        Assert.Empty(CampaignDifficulty.Audit(fine));
    }

    [Fact]
    public void Difficulty_VowsMustMatchTheBattleData()
    {
        var aloneWithoutRule = SideNode("S", 2, 3, 15, new[] { CampaignVow.Alone });
        var aloneWithRule = new CampaignNode
        {
            Id = "S", Chapter = "Chapter I", Tier = 2, SkillBudget = 3,
            Vows = [CampaignVow.Alone], Companions = [],
            Enemies = [Enemy(5), Enemy(5), Enemy(5)]
        };
        var woundedAtFull = SideNode("S", 2, 3, 15, new[] { CampaignVow.Wounded });
        var outnumberedWithThree = SideNode("S", 2, 3, 15, new[] { CampaignVow.Outnumbered });
        var noTier = new CampaignNode
        {
            Id = "S", Chapter = "Chapter I", SkillBudget = 3, Vows = [CampaignVow.Alone],
            Companions = [], Enemies = [Enemy(), Enemy(), Enemy()]
        };

        Assert.Contains(CampaignDifficulty.Audit(aloneWithoutRule), p => p.Contains("empty companions"));
        Assert.Empty(CampaignDifficulty.Audit(aloneWithRule));
        Assert.Contains(CampaignDifficulty.Audit(woundedAtFull), p => p.Contains("playerHealth"));
        Assert.Contains(CampaignDifficulty.Audit(outnumberedWithThree), p => p.Contains("four starting enemies"));
        Assert.Contains(CampaignDifficulty.Audit(noTier), p => p.Contains("vows require a tier"));
    }

    [Fact]
    public void Difficulty_SustainSkillsAreCappedPerEncounter()
    {
        var enemy = new CampaignEnemyDefinition { Name = "Carrier", Health = 4, Skills = [SkillType.Xue] };
        var node = new CampaignNode
        {
            Id = "S", Chapter = "Chapter I", Tier = 4, SkillBudget = 3,
            Enemies = [enemy, enemy, enemy, Enemy(3)]
        };

        Assert.Contains(CampaignDifficulty.Audit(node), p => p.Contains("Xue appears on 3 enemies"));
    }

    [Fact]
    public void ShippedSideChapter_CarriesItsVowsAndRewards()
    {
        var trial = Node("AS3");
        Assert.Equal(CampaignChapterKind.Side, CampaignChapters.KindFor(trial));
        Assert.Equal("ALONE", trial.VowText);
        Assert.Empty(trial.Companions!);
        Assert.Empty(CampaignDifficulty.Audit(trial));
        Assert.Contains("Ila joins", trial.RewardText);

        var watch = Node("AS5");
        Assert.Equal("WOUNDED", watch.VowText);
        Assert.Equal(8, watch.PlayerHealth);
        Assert.Empty(CampaignDifficulty.Audit(watch));
    }

    [Fact]
    public void CompanionRecruitment_RevealsIlaOnlyAfterHerChapter()
    {
        var progress = new CampaignProgress();
        var battle = Node("A1");
        var before = CampaignCompanions.Available(battle, progress);

        Assert.DoesNotContain(before, companion => companion.Id == "ila");
        Assert.Contains(before, companion => companion.Id == "mara_venn");
        Assert.Single(CampaignCompanions.ForNode("AS3"));
        Assert.Equal("ila", CampaignCompanions.ForNode("AS3")[0].Id);

        progress.Complete(Node("AS3"));
        var after = CampaignCompanions.Available(battle, progress);
        Assert.Contains(after, companion => companion.Id == "ila");
        // A null progress is the "everything the node allows" view used by tests and previews.
        Assert.Contains(CampaignCompanions.Available(battle), companion => companion.Id == "ila");
    }

    [Fact]
    public void PlayerHealth_FlowsIntoTheCampaignBattle()
    {
        var wounded = CampaignBattleFactory.Create(Node("AS5"), Array.Empty<SkillType>());
        var normal = CampaignBattleFactory.Create(Node("A1"), Array.Empty<SkillType>());

        Assert.Equal(8, wounded.Players[0].Health);
        Assert.Equal(8, wounded.Players[0].MaxHealth);
        Assert.Equal(11, normal.Players[0].Health);
    }

    [Fact]
    public void Vows_ParseSnakeCaseAndRejectUnknownValues()
    {
        Assert.Equal(CampaignVow.BareHands, CampaignVows.Parse("bare_hands"));
        Assert.Equal(CampaignVow.Outnumbered, CampaignVows.Parse(" Outnumbered "));
        Assert.Null(CampaignVows.Parse("nonsense"));

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new CampaignVowJsonConverter());
        var node = JsonSerializer.Deserialize<CampaignNode>(
            """{"id":"S","vows":["bare_hands","alone"]}""", options);
        Assert.NotNull(node);
        Assert.Equal(new[] { CampaignVow.BareHands, CampaignVow.Alone }, node!.Vows);

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CampaignNode>(
            """{"id":"S","vows":["make_it_easy"]}""", options));
    }
}
