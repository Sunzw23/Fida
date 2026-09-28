using Fida.Logic;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// 覆盖 Player 的核心数据契约：数字变更事件、伤害/治疗公式、存活/护盾判定、每回合技能重置。
/// </summary>
public class PlayerTests
{
    private record NumberEvent(Player p, int index, int old, int @new);

    [Fact]
    public void SetNumber_FiresNumberChangedWithOldAndNewValue()
    {
        var p = new Player(0, "alice");
        List<NumberEvent> events = [];

        p.NumberChanged += (player, index, old, @new) => events.Add(new NumberEvent(player, index, old, @new));

        p.SetNumber(0, 7);

        Assert.Single(events);
        Assert.Same(p, events[0].p);
        Assert.Equal(0, events[0].index);
        Assert.Equal(1, events[0].old);   // 初始数字为 1
        Assert.Equal(7, events[0].@new);
        Assert.Equal(7, p.Numbers[0]);
        Assert.Equal(1, p.Numbers[1]);     // 另一个数字不受影响
    }

    [Fact]
    public void SetNumber_SecondIndex_OnlyTouchesThatSlot()
    {
        var p = new Player(1, "bob");
        var events = new List<int>();
        p.NumberChanged += (_, index, _, _) => events.Add(index);

        p.SetNumber(1, 5);

        Assert.Equal([1], events);
        Assert.Equal(1, p.Numbers[0]);     // 未触碰
        Assert.Equal(5, p.Numbers[1]);
    }

    [Fact]
    public void ResetAllNumbersToOne_SetsBothAndFiresTwoEvents()
    {
        var p = new Player(0, "alice");
        p.SetNumber(0, 9);
        p.SetNumber(1, 4);

        var events = new List<(int index, int old, int @new)>();
        p.NumberChanged += (_, index, old, @new) => events.Add((index, old, @new));

        p.ResetAllNumbersToOne();

        Assert.Equal(2, events.Count);
        Assert.Equal((0, 9, 1), events[0]);
        Assert.Equal((1, 4, 1), events[1]);
        Assert.Equal(1, p.Numbers[0]);
        Assert.Equal(1, p.Numbers[1]);
    }

    [Fact]
    public void TakeDamage_HpEqualsAbsoluteDifference()
    {
        var p = new Player(0, "alice"); // Health = 11
        var fired = 0;
        p.HealthChanged += _ => fired++;

        p.TakeDamage(3);
        Assert.Equal(Math.Abs(11 - 3), p.Health);   // 8
        p.TakeDamage(5);
        Assert.Equal(Math.Abs(8 - 5), p.Health);   // 3
        Assert.Equal(2, fired);
    }

    [Fact]
    public void Heal_ClampsToMaxHealthAndNotBelowZero()
    {
        var p = new Player(0, "alice"); // MaxHealth = 11, Health = 11
        p.Heal(5);
        Assert.Equal(11, p.Health);          // 上限截断

        p.Health = 2;
        p.Heal(-5);
        Assert.Equal(0, p.Health);          // 下限截断
    }

    [Fact]
    public void IsAlive_FalseOnlyWhenHealthIsZero()
    {
        var p = new Player(0, "alice");
        p.Health = 1;
        Assert.True(p.IsAlive());
        p.Health = 0;
        Assert.False(p.IsAlive());
    }

    [Fact]
    public void HasShield_ReportsIndexWhoseNumberIsZero()
    {
        var p = new Player(0, "alice");

        p.SetNumber(0, 0);
        Assert.Equal((true, 0), p.HasShield());

        p.SetNumber(0, 3);
        p.SetNumber(1, 0);
        Assert.Equal((true, 1), p.HasShield());

        p.SetNumber(0, 0);
        p.SetNumber(1, 0);
        Assert.Equal((true, 0), p.HasShield());

        p.SetNumber(0, 3);
        p.SetNumber(1, 3);
        Assert.Equal((false, -1), p.HasShield());
    }
    [Fact]
    public void ResetPerTurnSkills_OnlyResetsPerTurnLimitSkills()
    {
        var perTurn = new PlusAttackSkill();
        perTurn.Usage = new SkillUsageState(SkillUsageLimit.PerTurn); // MaxUses=1, Remaining=1
        perTurn.Usage.MarkUsed(); // Remaining -> 0, IsActivated -> true
        Assert.False(perTurn.Usage.CanUse());

        // 非 PerTurn 的技能应被 ResetPerTurnSkills 跳过，保持其原状（这里保持已激活）
        var unlimited = new DoubleAttackSkill(); // 默认 Unlimited
        unlimited.Usage.MarkUsed();
        Assert.True(unlimited.Usage.IsActivated);

        var p = new Player(0, "alice");
        p.Skills.Add(perTurn);
        p.Skills.Add(unlimited);

        p.ResetPerTurnSkills();

        Assert.True(perTurn.Usage.CanUse());        // 已恢复可用
        Assert.False(perTurn.Usage.IsActivated);
        Assert.Equal(1, perTurn.Usage.RemainingUses);
        Assert.True(unlimited.Usage.IsActivated);   // 非 PerTurn 的保持原状（未被重置）
    }

    [Fact]
    public void GetSkillsByTrigger_FiltersByTrigger()
    {
        var attack = new PlusAttackSkill();   // OrderingAttack
        var defence = new PlusDefenceSkill(); // Defending

        var p = new Player(0, "alice");
        p.Skills.Add(attack);
        p.Skills.Add(defence);

        List<Skill> TakeAll(System.Collections.Generic.IEnumerator<Skill> en)
        {
            var list = new List<Skill>();
            while (en.MoveNext()) list.Add(en.Current);
            return list;
        }

        var attacking = TakeAll(p.GetSkillsByTrigger(SkillTrigger.OrderingAttack));
        Assert.Single(attacking);
        Assert.Same(attack, attacking[0]);

        var defending = TakeAll(p.GetSkillsByTrigger(SkillTrigger.Defending));
        Assert.Single(defending);
        Assert.Same(defence, defending[0]);
    }

    [Fact]
    public void GetSkillsByTrigger_PreservesLoadoutOrderForAttackModifiers()
    {
        var loadedSkills = new Skill[]
        {
            new DoubleAttackSkill(),
            new LongSkill(),
            new PlusAttackSkill()
        };
        var player = new Player(0, "alice");
        player.Skills.AddRange(loadedSkills);

        var actual = new List<Skill>();
        var matching = player.GetSkillsByTrigger(SkillTrigger.OrderingAttack);
        while (matching.MoveNext()) actual.Add(matching.Current);

        Assert.Equal(loadedSkills, actual);
    }
}
