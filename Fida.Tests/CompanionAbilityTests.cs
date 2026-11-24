using Fida.Campaign;
using Fida.Campaign.Battle;
using System;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public class CompanionAbilityTests
{
    [Fact]
    public void GateWard_BlocksOnlyTheFirstHitOfTheTurn()
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var bram = new Player(1, "Bram", 9, []) { WardsFirstHit = true };
        var game = CreateGame(attacker, bram);
        Assert.True(bram.WardsFirstHit);

        Strike(game, attacker, bram, 3);
        Assert.Equal(9, bram.Health);

        // A second hit in the same turn lands: the ward only covers the first.
        bram.RecordAttackReceived();
        Strike(game, attacker, bram, 3);
        Assert.Equal(6, bram.Health);
    }

    [Fact]
    public void GateWard_DoesNotBlockWhenTheFlagIsOff()
    {
        var attacker = new Player(0, "Attacker", 11, []);
        var bram = new Player(1, "Bram", 9, []);
        var game = CreateGame(attacker, bram);

        Strike(game, attacker, bram, 3);

        Assert.Equal(6, bram.Health);
    }

    [Fact]
    public void DoubleStrike_DoublesTheArmedAttackOnly()
    {
        var ila = new Player(0, "Ila", 7, []);
        var target = new Player(1, "Target", 11, []);
        var game = CreateGame(ila, target);
        ila.DoubleStrikeArmed = true;

        Strike(game, ila, target, 3);

        Assert.Equal(5, target.Health);
        Assert.False(ila.DoubleStrikeArmed);

        Strike(game, ila, target, 3);
        Assert.Equal(2, target.Health);
    }

    [Theory]
    [InlineData(1.0, 40)]
    [InlineData(0.5, 68)]
    [InlineData(0.25, 81)]
    [InlineData(0.0, 95)]
    public void AdaptiveCompanion_ScalesWithTheWeakestAlly(double ratio, int expected)
    {
        Assert.Equal(expected, AdaptiveCompanionController.StrengthFor(ratio));
    }

    [Fact]
    public void AdaptiveCompanion_ClampsOutOfRangeRatios()
    {
        Assert.Equal(40, AdaptiveCompanionController.StrengthFor(2));
        Assert.Equal(95, AdaptiveCompanionController.StrengthFor(-1));
    }

    [Fact]
    public void SableBringsAnInkEchoAndRecordsTheSummonerLink()
    {
        var setup = CampaignBattleFactory.Create(Node("A1"), [], companionIds: ["sable"]);

        var sable = setup.Players.Single(player => player.Name == "Sable");
        var echo = setup.Players.Single(player => player.Name == "Ink Echo");

        Assert.Equal("sable", setup.CompanionIds[sable.PlayerId]);
        Assert.Equal(echo.PlayerId, setup.Summons[sable.PlayerId]);
        Assert.Equal(3, echo.Health);
        Assert.Equal(5, sable.Health);
        Assert.Empty(sable.Skills);
        Assert.Empty(echo.Skills);
        Assert.Contains(echo, setup.Groups[0].Players);
        Assert.Equal(CampaignAgentType.Rule, setup.AgentTypes[echo.PlayerId]);
    }

    [Fact]
    public void OnlySableIsASummoner()
    {
        var setup = CampaignBattleFactory.Create(Node("A1"), [], companionIds: ["sable"]);
        var rules = new CampaignBattleRules(new GameManager());
        rules.Configure(setup);
        var sable = setup.Players.Single(player => player.Name == "Sable");

        Assert.True(rules.IsSummoner(sable.PlayerId));
        Assert.False(rules.IsSummoner(setup.Players[0].PlayerId));

        // Her echo is alive at configuration time, so there is nothing to channel yet.
        Assert.False(rules.ShouldChannel(sable.PlayerId));
    }

    [Fact]
    public void RemovePlayer_TakesACorpseOffTheBoardButKeepsTheActor()
    {
        var a = new Player(0, "A", 11, []);
        var echo = new Player(1, "Echo", 3, []) { Health = 0 };
        var enemy = new Player(2, "Enemy", 5, []);
        var game = new GameManager();
        game.Initialize([a, echo, enemy],
            [new Group(0, "Allies", [a, echo]), new Group(1, "Enemies", [enemy])]);

        Assert.False(game.RemovePlayer(a.PlayerId));
        Assert.True(game.RemovePlayer(echo.PlayerId));
        Assert.DoesNotContain(game.Players, player => player.PlayerId == echo.PlayerId);
        Assert.DoesNotContain(game.Groups[0].Players, player => player.PlayerId == echo.PlayerId);
    }

    [Fact]
    public void Sable_ChannelsTwiceThenWritesANewInkEcho()
    {
        var setup = CampaignBattleFactory.Create(Node("A1"), [], companionIds: ["sable"]);
        var game = new GameManager();
        game.Initialize(setup.Players, setup.Groups);
        var rules = new CampaignBattleRules(game);
        var summoned = new List<Player>();
        var removed = new List<int>();
        rules.Summoned += player => summoned.Add(player);
        rules.Removed += id => removed.Add(id);
        rules.Configure(setup);
        var sable = setup.Players.Single(player => player.Name == "Sable");
        var echo = setup.Players.Single(player => player.Name == "Ink Echo");

        game.StartGame();
        echo.TakeDamage(echo.Health);

        // The echo is removed at the next round anchor, not mid-turn.
        SkipCurrent(game);
        AdvanceToTurn(game, 0);
        Assert.Contains(echo.PlayerId, removed);
        Assert.DoesNotContain(game.Players, player => player.PlayerId == echo.PlayerId);

        // Two of her own turns go into writing, then the echo returns.
        SkipCurrent(game);
        AdvanceToTurn(game, 1);
        Assert.True(rules.ShouldChannel(sable.PlayerId));

        SkipCurrent(game);
        AdvanceToTurn(game, 0);
        SkipCurrent(game);
        AdvanceToTurn(game, 1);
        Assert.True(rules.ShouldChannel(sable.PlayerId));

        SkipCurrent(game);
        AdvanceToTurn(game, 0);
        SkipCurrent(game);
        AdvanceToTurn(game, 1);
        Assert.False(rules.ShouldChannel(sable.PlayerId));
        var replacement = Assert.Single(summoned);
        Assert.Equal("Ink Echo", replacement.Name);
        Assert.Contains(game.Players, player => player.PlayerId == replacement.PlayerId);
    }
    [Fact]
    public void ShippedRoster_MatchesTheCompanionAbilityDesign()
    {
        var ila = CampaignCompanions.Get("ila")!;
        Assert.Equal([SkillType.PlusAttack, SkillType.DoubleAttack], ila.Skills);

        var bram = CampaignCompanions.Get("bram")!;
        Assert.Equal("gate_ward", bram.Passive);
        Assert.Equal([SkillType.DoubleDefence], bram.Skills);

        var sable = CampaignCompanions.Get("sable")!;
        Assert.Equal(5, sable.Health);
        Assert.Empty(sable.Skills);
        Assert.NotNull(sable.Summons);
        Assert.Equal(3, sable.Summons!.Health);

        var mara = CampaignCompanions.Get("mara_venn")!;
        Assert.Equal(10, mara.Health);
        Assert.Empty(mara.Skills);
        Assert.Equal(CampaignAgentType.Adaptive, mara.AgentType);
    }

    private static GameManager CreateGame(Player attacker, Player target)
    {
        var game = new GameManager();
        game.Initialize([attacker, target],
            [new Group(0, "Allies", [attacker]), new Group(1, "Enemies", [target])]);
        return game;
    }

    private static void SkipCurrent(GameManager game) =>
        game.UpdateGame(new GameAction(ActionType.SkipTurn)
        {
            TargetPlayerId = game.GetCurrentPlayer().PlayerId
        });

    private static void AdvanceToTurn(GameManager game, int playerId)
    {
        for (int step = 0; step < 12; step++)
        {
            if (game.IsGameEnded) throw new Exception("battle ended before the target turn.");
            var current = game.GetCurrentPlayer();
            if (current.PlayerId == playerId) return;
            game.UpdateGame(new GameAction(ActionType.SkipTurn) { TargetPlayerId = current.PlayerId });
        }
        throw new Exception($"turn for player {playerId} was never reached.");
    }

    private static void Strike(GameManager game, Player attacker, Player target, int damage)
    {
        var state = new TargetSelectionState(game,
            new SkillContext(attacker.PlayerId, damage: damage));
        state.Enter();
        state.Handle(new GameAction(ActionType.ChooseTarget) { TargetPlayerId = target.PlayerId });
    }

    private static CampaignNode Node(string id) => CampaignCatalog.GetNode(id)!;
}
