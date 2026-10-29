using System.IO.Compression;
using Fida.Agent;
using Fida.Campaign;
using Fida.Campaign.Battle;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class LightweightAgentTests
{
    [Fact]
    public void Selector_StrengthEndpointsAndSeedAreReliable()
    {
        double[] scores = [-1, 0, 0.5, 1];
        Assert.Equal(3, new ActionSelector(new() { Strength = 100 }).Select(scores));
        var a = new ActionSelector(new() { Strength = 0, Seed = 123 });
        var b = new ActionSelector(new() { Strength = 0, Seed = 123 });
        var choices = Enumerable.Range(0, 400).Select(_ => a.Select(scores)).ToArray();
        Assert.Equal(choices, Enumerable.Range(0, 400).Select(_ => b.Select(scores)).ToArray());
        Assert.Equal(4, choices.Distinct().Count());
    }

    [Fact]
    public void Selector_StrongerProfileReducesLossAndRejectsInvalidInputs()
    {
        double[] scores = [-1, -0.5, 0, 0.5, 1];
        var weak = new ActionSelector(new() { Strength = 25, Seed = 321 });
        var strong = new ActionSelector(new() { Strength = 80, Seed = 321 });
        double weakScore = 0, strongScore = 0;
        for (int i = 0; i < 5000; i++)
        {
            weakScore += scores[weak.Select(scores)];
            int choice = strong.Select(scores);
            Assert.True(1 - scores[choice] <= strong.Profile.MaxActionLoss + 1e-9);
            strongScore += scores[choice];
        }
        Assert.True(strongScore > weakScore + 1000);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionSelector(new() { Strength = 101 }));
        Assert.Throws<ArgumentException>(() => strong.Select([]));
        Assert.Throws<ArgumentException>(() => strong.Select([double.NaN]));
    }

    [Fact]
    public void Table_CanonicalNumbersAndActionsPreserveScores()
    {
        var table = AgentDefaults.Table.Value!;
        Assert.NotNull(table);
        Assert.Equal(11, table.MaxHealth);
        var p = new Player(7, "P", 11, []) { Health = 3 };
        var e = new Player(12, "E", 11, []) { Health = 5 };
        p.SetNumber(0, 2); p.SetNumber(1, 6);
        e.SetNumber(0, 1); e.SetNumber(1, 4);
        var scores = (from i in Enumerable.Range(0, 2) from j in Enumerable.Range(0, 2)
                      select table.Score(p, e, i, e.PlayerId, j)).ToArray();
        p.SetNumber(0, 6); p.SetNumber(1, 2);
        e.SetNumber(0, 4); e.SetNumber(1, 1);
        var swapped = (from i in Enumerable.Range(0, 2) from j in Enumerable.Range(0, 2)
                       select table.Score(p, e, 1 - i, e.PlayerId, 1 - j)).ToArray();
        Assert.Equal(scores, swapped);
        Assert.Equal(55, (from i in Enumerable.Range(0, 10) from j in Enumerable.Range(i, 10 - i)
                          select CompactActionTable.PairIndex(i, j)).Distinct().Count());
    }

    [Fact]
    public void Table_InvalidAndTruncatedFilesFailExplicitly()
    {
        using var stream = new MemoryStream();
        using (var zip = new GZipStream(stream, CompressionMode.Compress, true))
        using (var writer = new BinaryWriter(zip))
        {
            writer.Write("FDQ1"u8.ToArray()); writer.Write(99); writer.Write(11); writer.Write(0);
        }
        stream.Position = 0;
        Assert.Throws<InvalidDataException>(() => CompactActionTable.Load(stream));
    }

    [Fact]
    public void PackedTable_AllBitBoundariesPreserveScoresAndTruncationIsRejected()
    {
        const int count = 55 * 55 * 6;
        var packed = new byte[(count * 6 + 7) / 8];
        for (int i = 0; i < count; i++)
        {
            int code = (i * 17 + 3) % 64, bit = i * 6;
            packed[bit / 8] |= (byte)(code << (bit % 8));
            if (bit % 8 > 2) packed[bit / 8 + 1] |= (byte)(code >> (8 - bit % 8));
        }
        byte[] FileData(bool truncated)
        {
            using var output = new MemoryStream();
            using (var zip = new GZipStream(output, CompressionMode.Compress, true))
            using (var writer = new BinaryWriter(zip))
            {
                writer.Write("FDQ2"u8.ToArray()); writer.Write(1); writer.Write(1); writer.Write(count);
                writer.Write(64); writer.Write(6);
                for (int i = 0; i < 64; i++) writer.Write((short)((i - 32) * 1000));
                writer.Write(truncated ? packed[..^1] : packed);
            }
            return output.ToArray();
        }
        using var valid = new MemoryStream(FileData(false));
        var table = CompactActionTable.Load(valid);
        var own = new Player(7, "Own", 1, []);
        var enemy = new Player(12, "Enemy", 1, []);
        var pairs = (from a in Enumerable.Range(0, 10) from b in Enumerable.Range(a, 10 - a) select (a, b)).ToArray();
        int expectedIndex = 0;
        foreach (var p in pairs)
        foreach (var e in pairs)
        {
            own.SetNumber(0, p.a); own.SetNumber(1, p.b);
            enemy.SetNumber(0, e.a); enemy.SetNumber(1, e.b);
            for (int action = 0; action < 6; action++)
            {
                int first = action / 3, second = action % 3;
                double score = table.Score(own, enemy, first, second == 2 ? 7 : 12, second == 2 ? 1 - first : second);
                double expected = (((expectedIndex++ * 17 + 3) % 64) - 32) * 1000 / 32767.0;
                Assert.Equal(expected, score);
            }
        }
        using var truncated = new MemoryStream(FileData(true));
        Assert.Throws<EndOfStreamException>(() => CompactActionTable.Load(truncated));
    }

    [Fact]
    public void Table_ChoosesImmediateWinWithNonzeroPlayerIds()
    {
        var source = new Player(7, "Source", 11, []);
        var enemy = new Player(12, "Enemy", 11, []) { Health = 1 };
        source.SetNumber(0, 6); source.SetNumber(1, 2);
        var game = new GameManager(); game.Initialize([source, enemy]);
        game.TransitionTo(new ChooseNumberIndexState(game, source.PlayerId));
        var agent = new LightweightAgent(new() { Strength = 100, Seed = 9 });
        for (int steps = 0; steps < 8 && !game.IsGameEnded; steps++)
            game.UpdateGame(agent.Decide(game, Request(game)));
        Assert.True(game.IsGameEnded);
        Assert.True(source.IsAlive());
        Assert.False(enemy.IsAlive());
    }

    [Fact]
    public void SkillAndMultiplayerStatesUseTableWithoutInvalidActions()
    {
        var a = new Player(5, "A", 11, [SkillFactory.CreateSkill(SkillType.Bian)]);
        var b = new Player(9, "B", 11, []);
        var c = new Player(14, "C", 11, []);
        var game = new GameManager(); game.Initialize([a, b, c]);
        var context = new SkillContext(a.PlayerId, damage: 1);
        game.TransitionTo(new OrderingAttackState(game, context));
        Assert.IsType<BianSkillState>(game.CurrentState);
        var agent = new LightweightAgent(new() { Strength = 100 });
        var action = agent.Decide(game, Request(game));
        Assert.Contains(action.Number, game.CurrentStepValidNumbers);
        Assert.Equal("Table", agent.LastScorer);
        Assert.True(agent.LastSimulationNodes > 0);
        game.UpdateGame(action);
        Assert.NotEqual(typeof(BianSkillState), game.CurrentState?.GetType());
        Assert.IsType<LightweightAgentController>(CampaignControllerFactory.Create(5, CampaignAgentType.Rule));
        Assert.IsType<LightweightAgentController>(CampaignControllerFactory.Create(5, CampaignAgentType.Scripted));
    }

    [Fact]
    public void SkillRewriteUsesItsOwnIndexInsteadOfNormalTurnPlan()
    {
        var a = new Player(5, "A", 11, [SkillFactory.CreateSkill(SkillType.Shen)]);
        var b = new Player(9, "B", 11, []);
        a.SetNumber(0, 6); a.SetNumber(1, 3);
        var game = new GameManager(); game.Initialize([a, b]);
        game.TransitionTo(new ShenSkillState(game, new SkillContext(a.PlayerId)));
        var agent = new LightweightAgent(new() { Strength = 100 });
        var index = agent.Decide(game, Request(game));
        Assert.Equal(1, index.NumberIndex);
        game.UpdateGame(index);
        var number = agent.Decide(game, Request(game));
        Assert.InRange(number.Number, 0, 9);
        game.UpdateGame(number);
        Assert.IsType<TargetSelectionState>(game.CurrentState);
    }

    [Fact]
    public void TableSimulationDeclinesDamageBonusWhenItWouldMissExactKill()
    {
        var a = new Player(5, "A", 11, [SkillFactory.CreateSkill(SkillType.PlusAttack)]);
        var b = new Player(9, "B", 11, []) { Health = 2 };
        var game = new GameManager(); game.Initialize([a, b]);
        var context = new SkillContext(a.PlayerId, damage: 2);
        game.TransitionTo(new OrderingAttackState(game, context));
        Assert.IsType<PlusAttackSkillState>(game.CurrentState);
        var state = game.CurrentState;
        int events = 0;
        game.CardActionStarted += (_, _) => events++;
        game.PlayerActionRequired += (_, _) => events++;
        a.HealthChanged += _ => events++;
        b.HealthChanged += _ => events++;
        var agent = new LightweightAgent(new() { Strength = 100 });
        var action = agent.Decide(game, Request(game));
        Assert.False(action.IsApply);
        Assert.Equal(0, events);
        Assert.Same(state, game.CurrentState);
        Assert.Equal(2, context.Damage);
        Assert.Equal(2, b.Health);
        Assert.False(game.IsGameEnded);
        game.UpdateGame(action);
        Assert.IsType<TargetSelectionState>(game.CurrentState);
        Assert.Equal(2, context.Damage);
    }

    [Fact]
    public void TableSimulationPreservesAttackEnumeratorAndSkillUsage()
    {
        var a = new Player(5, "A", 11, [SkillFactory.CreateSkill(SkillType.PlusAttack),
            SkillFactory.CreateSkill(SkillType.DoubleAttack), SkillFactory.CreateSkill(SkillType.Xue)]);
        var b = new Player(9, "B", 11, [SkillFactory.CreateSkill(SkillType.Shen)]);
        var game = new GameManager(); game.Initialize([a, b]);
        var context = new SkillContext(a.PlayerId, damage: 2);
        game.TransitionTo(new OrderingAttackState(game, context));
        Assert.IsType<PlusAttackSkillState>(game.CurrentState);
        var enumerator = context.SkillEnumerator;
        var agent = new LightweightAgent(new() { Strength = 100 });
        var action = agent.Decide(game, Request(game));
        Assert.Same(enumerator, context.SkillEnumerator);
        Assert.Equal(SkillType.PlusAttack, enumerator!.Current.Type);
        Assert.Equal(1, b.Skills[0].Usage.RemainingUses);
        Assert.Equal(new[] { 1, 1 }, b.Numbers);
        game.UpdateGame(action);
        Assert.IsType<DoubleAttackSkillState>(game.CurrentState);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(33)]
    public void AllHealthCapsUseTheSameTableWithoutRuleScores(int maxHealth)
    {
        var game = new GameManager();
        game.Initialize([new Player(5, "A", maxHealth, []), new Player(9, "B", maxHealth, [])]);
        game.StartGame();
        var agent = new LightweightAgent(new() { Strength = 65, Seed = 42 });
        var first = agent.Decide(game, Request(game));
        Assert.Equal("Table", agent.LastScorer);
        Assert.NotEqual(ActionType.None, first.Type);
        game.UpdateGame(first);
        var second = agent.Decide(game, Request(game));
        Assert.True(second.TargetPlayerId != first.TargetPlayerId || second.NumberIndex != first.NumberIndex);
        game.UpdateGame(second);
    }

    [Fact]
    public void EverySkillCanUseTheGenericTableSimulationWithoutMutatingRealState()
    {
        foreach (var skillType in SkillFactory.SelectableSkills)
        {
            var a = new Player(5, "A", 11, [SkillFactory.CreateSkill(skillType)]);
            var b = new Player(9, "B", 11, []);
            var game = new GameManager(); game.Initialize([a, b]);
            game.StartGame();
            var agent = new LightweightAgent(new() { Strength = 65, Seed = 7 });
            for (int step = 0; step < 24 && !game.IsGameEnded; step++)
            {
                var state = game.CurrentState;
                var numbers = game.Players.SelectMany(p => p.Numbers).ToArray();
                var health = game.Players.Select(p => p.Health).ToArray();
                var uses = a.Skills.Select(s => s.Usage.RemainingUses).ToArray();
                var request = Request(game);
                var action = agent.Decide(game, request);
                Assert.Equal("Table", agent.LastScorer);
                Assert.Same(state, game.CurrentState);
                Assert.Equal(numbers, game.Players.SelectMany(p => p.Numbers).ToArray());
                Assert.Equal(health, game.Players.Select(p => p.Health).ToArray());
                Assert.Equal(uses, a.Skills.Select(s => s.Usage.RemainingUses).ToArray());
                Assert.NotEqual(ActionType.None, action.Type);
                if (request.ValidNumbers.Count > 0) Assert.Contains(action.Number, request.ValidNumbers);
                if (request.ValidTargets.Count > 0) Assert.Contains(action.TargetPlayerId, request.ValidTargets);
                game.UpdateGame(action);
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65)]
    [InlineData(100)]
    public void RealEngine_FullDuelsAlwaysAdvanceWithoutStalling(int strength)
    {
        var game = new GameManager();
        game.Initialize([new Player(7, "A", 11, []), new Player(12, "B", 11, [])]);
        game.StartGame();
        var agents = new Dictionary<int, LightweightAgent>
        {
            [7] = new(new() { Strength = strength, Seed = 18 }),
            [12] = new(new() { Strength = 0, Seed = 19 })
        };
        for (int step = 0; step < 3000 && !game.IsGameEnded; step++)
        {
            var previous = game.CurrentState;
            int previousFirst = game.Query.GetFirstChosenNumberIndex();
            var action = agents[previous!.ActorId].Decide(game, Request(game));
            Assert.NotEqual(ActionType.None, action.Type);
            game.UpdateGame(action);
            Assert.True(game.IsGameEnded || !ReferenceEquals(previous, game.CurrentState) ||
                        previousFirst != game.Query.GetFirstChosenNumberIndex());
        }
        Assert.True(game.IsGameEnded);
    }

    private static PlayerActionRequiredEventArgs Request(GameManager game) => new()
    {
        SourcePlayerId = game.CurrentState!.ActorId,
        RequiredActionType = game.CurrentActionType,
        ValidTargets = game.CurrentStepValidTargets,
        ValidNumbers = game.CurrentStepValidNumbers
    };
}
