using System.Reflection;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// 覆盖 ChooseNumberIndexState 的「同」技能核心分支与步骤校验。
///
/// 关键契约：Execute 自己发出的 SetNumber 事件（发生在 PushState/TransitionTo 触发回合
/// 级联之前）才是技能语义本身。级联（ApplyingEffectState 会按结果数字触发攻击/治疗并
/// SetNumber(…,1)、以及后续回合重置）会追加额外事件并改写最终 Numbers，因此断言只针对
/// “前 N 个事件”，而非最终 Numbers 或事件总数。
///
/// 计算值 n = (Source.Numbers[firstIndex] + other.Numbers[secondIndex]) % 10。
/// changeBothNumbers=true → Execute 依次 SetNumber(0,n)、SetNumber(1,n)。
/// changeBothNumbers=false → Execute 只 SetNumber(firstIndex,n)。
/// </summary>
public class ChooseNumberIndexStateTests
{
    private static readonly MethodInfo ExecuteMethod =
        typeof(ChooseNumberIndexState).GetMethod("Execute", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ChooseNumberIndexState.Execute 未找到");

    private static Player MakePlayer(int id, string name, int n0, int n1)
    {
        var p = new Player(id, name);
        p.SetNumber(0, n0);
        p.SetNumber(1, n1);
        return p;
    }

    private static void InvokeExecute(ChooseNumberIndexState state, List<GameAction> actions)
    {
        ExecuteMethod.Invoke(state, new object[] { actions });
    }

    private static List<GameAction> Actions(int firstIndex, int targetPlayerId, int secondIndex)
    {
        return
        [
            new GameAction(ActionType.ChooseNumberIndex) { TargetPlayerId = 0, NumberIndex = firstIndex },
            new GameAction(ActionType.ChooseNumberIndex) { TargetPlayerId = targetPlayerId, NumberIndex = secondIndex },
        ];
    }

    [Fact]
    public void BothNumbersBranch_SetsBothIndicesToCalculatedValue()
    {
        var gm = new GameManager();
        var alice = MakePlayer(0, "alice", n0: 2, n1: 9);
        var bob = MakePlayer(1, "bob", n0: 3, n1: 3);
        gm.Initialize([alice, bob]);

        var state = new ChooseNumberIndexState(gm, alice.PlayerId, changeBothNumbers: true);

        var events = new List<(int index, int @new)>();
        alice.NumberChanged += (_, index, _, @new) => events.Add((index, @new));

        // 计算值 = (2 + 3) % 10 = 5。两分支：先 SetNumber(0,5) 再 SetNumber(1,5)。
        InvokeExecute(state, Actions(firstIndex: 0, targetPlayerId: 1, secondIndex: 0));

        Assert.Equal((0, 5), events[0]);
        Assert.Equal((1, 5), events[1]);
    }

    [Fact]
    public void SingleNumberBranch_OnlySetsChosenIndex()
    {
        var gm = new GameManager();
        var alice = MakePlayer(0, "alice", n0: 2, n1: 9);
        var bob = MakePlayer(1, "bob", n0: 3, n1: 3);
        gm.Initialize([alice, bob]);

        var state = new ChooseNumberIndexState(gm, alice.PlayerId); // changeBothNumbers 默认 false

        var events = new List<(int index, int @new)>();
        alice.NumberChanged += (_, index, _, @new) => events.Add((index, @new));

        // 只选中索引 0；计算值 = (2 + 3) % 10 = 5 → Execute 仅 SetNumber(0,5)。
        InvokeExecute(state, Actions(firstIndex: 0, targetPlayerId: 1, secondIndex: 0));

        Assert.Equal((0, 5), events[0]);
    }

    [Fact]
    public void SingleNumberBranch_ChosenIndexIsRespected()
    {
        var gm = new GameManager();
        var alice = MakePlayer(0, "alice", n0: 1, n1: 4);
        var bob = MakePlayer(1, "bob", n0: 2, n1: 2);
        gm.Initialize([alice, bob]);

        var state = new ChooseNumberIndexState(gm, alice.PlayerId);

        var events = new List<(int index, int @new)>();
        alice.NumberChanged += (_, index, _, @new) => events.Add((index, @new));

        // 选中索引 1；计算值 = (4 + 2) % 10 = 6 → Execute 仅 SetNumber(1,6)。
        InvokeExecute(state, Actions(firstIndex: 1, targetPlayerId: 1, secondIndex: 1));

        Assert.Equal((1, 6), events[0]);
    }

    [Fact]
    public void ValidateStep_RejectsPickingSameNumberIndex()
    {
        var gm = new GameManager();
        var alice = MakePlayer(0, "alice", 2, 9);
        var bob = MakePlayer(1, "bob", 3, 3);
        gm.Initialize([alice, bob]);

        var state = new ChooseNumberIndexState(gm, alice.PlayerId);
        state.Enter(); // 构建两步，停在第一步

        // 第一步选索引 0（自己），完成后进入第二步
        state.Handle(new GameAction(ActionType.ChooseNumberIndex) { TargetPlayerId = 0, NumberIndex = 0 });
        Assert.Equal(1, state.Stage);

        // 第二步再选同一个数字（自己、索引 0）→ 被拒绝，回退到第一步
        state.Handle(new GameAction(ActionType.ChooseNumberIndex) { TargetPlayerId = 0, NumberIndex = 0 });
        Assert.Equal(0, state.Stage);
    }

    [Fact]
    public void BuildSteps_ProducesTwoChooseNumberIndexSteps()
    {
        var gm = new GameManager();
        var alice = MakePlayer(0, "alice", 2, 9);
        var bob = MakePlayer(1, "bob", 3, 3);
        gm.Initialize([alice, bob]);

        var state = new ChooseNumberIndexState(gm, alice.PlayerId);
        state.Enter();

        // Steps 是基类 GameState 的 protected 实例字段，需从基类型反射取得
        var steps = typeof(GameState)
            .GetField("Steps", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(state) as System.Collections.IList;

        Assert.NotNull(steps);
        Assert.Equal(2, steps.Count);
    }
}
