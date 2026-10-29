using Fida.Logic;

namespace Fida.Agent;

/// <summary>所有评分均来自 value table；真实状态机模拟动作，技能无需单独维护 AI 策略。</summary>
public sealed class LightweightAgent
{
    private readonly ActionSelector _selector;
    private readonly CompactActionTable _table;
    private GameState? _plannedState;
    private (int first, int target, int second)? _plan;
    public AgentProfile Profile => _selector.Profile;
    public string LastScorer => "Table";
    public int LastSimulationNodes { get; private set; }

    public LightweightAgent(AgentProfile? profile = null, CompactActionTable? table = null)
    {
        _selector = new(profile ?? AgentDefaults.Profile);
        _table = table ?? AgentDefaults.Table.Value;
    }

    public GameAction Decide(GameManager game, PlayerActionRequiredEventArgs args)
    {
        LastSimulationNodes = 0;
        if (game.IsGameEnded || !game.Players.Any(p => p.PlayerId == args.SourcePlayerId && p.IsAlive()))
            return new(ActionType.None);
        var source = game.Players.First(p => p.PlayerId == args.SourcePlayerId);
        bool ordinary = game.CurrentState is ChooseNumberIndexState && args.RequiredActionType == ActionType.ChooseNumberIndex;
        bool firstStep = ordinary && game.Query.IsChooseFirstNumberIndexState();
        int first = game.Query.GetFirstChosenNumberIndex();
        if (ordinary && !firstStep && ReferenceEquals(_plannedState, game.CurrentState) && _plan is { } plan &&
            first == plan.first && Allowed(args, plan.target) && game.Players.Any(p => p.PlayerId == plan.target && p.IsAlive()))
        {
            _plan = null;
            return IndexAction(plan.target, plan.second);
        }

        if (ordinary)
        {
            var combinations = (from i in Enumerable.Range(0, source.Numbers.Length)
                                where firstStep || i == first
                                from player in game.Players
                                where player.IsAlive() && (firstStep || Allowed(args, player.PlayerId))
                                from j in Enumerable.Range(0, player.Numbers.Length)
                                where player.PlayerId != source.PlayerId || i != j
                                select (first: i, target: player.PlayerId, second: j)).ToArray();
            if (combinations.Length == 0) return new(ActionType.None);
            var scores = new double[combinations.Length];
            bool direct = _table.Supports(game, source.PlayerId);
            var enemy = direct ? game.Players.First(p => p.PlayerId != source.PlayerId) : null;
            for (int i = 0; i < combinations.Length; i++)
            {
                var c = combinations[i];
                if (direct)
                    scores[i] = _table.Score(source, enemy!, c.first, c.target, c.second);
                else
                {
                    var snapshot = SimulationSnapshot.Copy(game);
                    if (firstStep) snapshot.UpdateGame(IndexAction(source.PlayerId, c.first));
                    snapshot.UpdateGame(IndexAction(c.target, c.second));
                    scores[i] = Continue(snapshot, source.PlayerId, Profile.SimulationDepth,
                        Profile.SimulationNodeBudget / combinations.Length);
                }
            }
            var choice = combinations[_selector.Select(scores)];
            if (firstStep)
            {
                _plannedState = game.CurrentState;
                _plan = choice;
                return IndexAction(source.PlayerId, choice.first);
            }
            return IndexAction(choice.target, choice.second);
        }

        var candidates = LegalActions(game, args).ToArray();
        if (candidates.Length == 0) throw new InvalidOperationException("当前决策没有合法动作。");
        var values = ScoreCandidates(game, candidates, source.PlayerId, Profile.SimulationDepth,
            Profile.SimulationNodeBudget);
        return candidates[_selector.Select(values)];
    }

    private double[] ScoreCandidates(GameManager game, GameAction[] actions, int root, int depth, int budget)
    {
        var values = new double[actions.Length];
        // 每个候选分配同样预算，避免动作顺序造成搜索质量差异。
        int each = Math.Max(0, budget / actions.Length - 1);
        for (int i = 0; i < actions.Length; i++)
        {
            var snapshot = SimulationSnapshot.Copy(game);
            snapshot.UpdateGame(actions[i]);
            LastSimulationNodes++;
            values[i] = Continue(snapshot, root, depth - 1, each);
        }
        return values;
    }

    private double Continue(GameManager game, int root, int depth, int budget)
    {
        // 到达下一正常回合选择、终局或预算边界时，唯一估值源都是 table。
        if (game.IsGameEnded) return Evaluate(game, root);
        if (game.CurrentState is ChooseNumberIndexState && game.Query.IsChooseFirstNumberIndexState())
            return 0.99 * Evaluate(game, root); // 与训练时的 continuation discount 一致。
        if (depth <= 0 || budget <= 0)
            return Evaluate(game, root);
        var args = Request(game);
        var actions = LegalActions(game, args).ToArray();
        if (actions.Length == 0) throw new InvalidOperationException("模拟遇到没有合法动作的决策。");
        // 不足以完整展开本层时直接查表，不能只搜索前几个动作。
        if (budget < actions.Length) return Evaluate(game, root);
        var values = ScoreCandidates(game, actions, root, depth, budget);
        return game.AreAllies(root, args.SourcePlayerId) ? values.Max() : values.Min();
    }

    private double Evaluate(GameManager game, int root)
    {
        var allies = game.Players.Where(p => p.IsAlive() && game.AreAllies(root, p.PlayerId)).ToArray();
        var enemies = game.Players.Where(p => p.IsAlive() && !game.AreAllies(root, p.PlayerId)).ToArray();
        if (allies.Length == 0) return -1;
        if (enemies.Length == 0) return 1;
        bool friendlyTurn = game.AreAllies(root, game.Query.CurrentPlayerId);
        // 多人状态用同一基础表的友敌两两值取平均，没有技能/攻击/治疗手写权重。
        return (from ally in allies from enemy in enemies
                select friendlyTurn ? _table.Value(ally, enemy) : -_table.Value(enemy, ally)).Average();
    }

    private static IEnumerable<GameAction> LegalActions(GameManager game, PlayerActionRequiredEventArgs args)
    {
        switch (args.RequiredActionType)
        {
            case ActionType.ChooseNumberIndex:
                int first = game.Query.GetFirstChosenNumberIndex();
                foreach (var player in game.Players.Where(p => p.IsAlive() && Allowed(args, p.PlayerId)))
                for (int index = 0; index < player.Numbers.Length; index++)
                    if (first < 0 || player.PlayerId != args.SourcePlayerId || index != first)
                        yield return IndexAction(player.PlayerId, index);
                break;
            case ActionType.ChooseTarget:
                foreach (var player in game.Players.Where(p => p.IsAlive() && Allowed(args, p.PlayerId)))
                    yield return new(ActionType.ChooseTarget) { TargetPlayerId = player.PlayerId };
                break;
            case ActionType.ChooseNumber:
                foreach (int number in args.ValidNumbers.Count > 0 ? args.ValidNumbers : Enumerable.Range(0, 10))
                    yield return new(ActionType.ChooseNumber) { TargetPlayerId = args.SourcePlayerId, Number = number };
                break;
            case ActionType.DecideApply:
                int target = args.ValidTargets.Count > 0 ? args.ValidTargets[0] : args.SourcePlayerId;
                yield return new(ActionType.DecideApply) { TargetPlayerId = target, IsApply = false };
                yield return new(ActionType.DecideApply) { TargetPlayerId = target, IsApply = true };
                break;
        }
    }

    private static PlayerActionRequiredEventArgs Request(GameManager game) => new()
    {
        SourcePlayerId = game.CurrentState?.ActorId ?? game.Query.CurrentPlayerId,
        RequiredActionType = game.CurrentActionType,
        ValidTargets = game.CurrentStepValidTargets,
        ValidNumbers = game.CurrentStepValidNumbers
    };
    private static bool Allowed(PlayerActionRequiredEventArgs args, int id) => args.ValidTargets.Count == 0 || args.ValidTargets.Contains(id);
    private static GameAction IndexAction(int player, int index) => new(ActionType.ChooseNumberIndex) { TargetPlayerId = player, NumberIndex = index };
}

public sealed class LightweightAgentController : IAutomatedPlayerController
{
    public int PlayerId { get; }
    public LightweightAgent Agent { get; }
    public LightweightAgentController(int playerId, AgentProfile? profile = null)
    {
        PlayerId = playerId;
        Agent = new(profile);
    }
    public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args)
    {
        if (args.SourcePlayerId != PlayerId) return;
        var action = Agent.Decide(game, args);
        if (action.Type != ActionType.None) game.UpdateGame(action, GameActionOrigin.Automated);
    }
}
