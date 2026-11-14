#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;

namespace Fida.UI.Rules;

/// <summary>Family reads as one word on the page; the order is the index order.</summary>
public enum SkillFamily { Offense, Defense, Vitality, Counter, Control }

/// <summary>
/// The single written source for a skill's codex page. One effect sentence, one worked example,
/// and a derived footnote. Copy is checked against Logic/Skills by tests.
/// </summary>
public static class SkillCodexText
{
    public sealed record Entry(SkillFamily Family, string Effect, string Example);

    private static readonly IReadOnlyDictionary<SkillType, Entry> Entries =
        new Dictionary<SkillType, Entry>
        {
            [SkillType.PlusAttack] = new(SkillFamily.Offense,
                "Before your attack's damage is set, add 1.", "7 → 1 damage · with Attack Up → 2"),
            [SkillType.DoubleAttack] = new(SkillFamily.Offense,
                "Before your attack's damage is set, double it.", "3 damage → 6"),
            [SkillType.Long] = new(SkillFamily.Offense,
                "When you commit an attack, use your two numbers added together as the damage.",
                "5 and 5 → 10 damage instead of 3"),
            [SkillType.Bian] = new(SkillFamily.Offense,
                "When you commit an attack, set its damage to any value from 1 to 5.", "6 damage → 3"),
            [SkillType.PlusDefence] = new(SkillFamily.Defense,
                "When you are hit, reduce the damage by 1, never below 1.", "5 damage → 4"),
            [SkillType.DoubleDefence] = new(SkillFamily.Defense,
                "When you are hit, halve the damage, never below 1.", "6 damage → 3"),
            [SkillType.Shi] = new(SkillFamily.Defense,
                "After the first attack that hurts you in a turn, later attacks deal no damage.",
                "First hit lands · second hit → 0"),
            [SkillType.DoubleHealth] = new(SkillFamily.Vitality,
                "At battle start, double your maximum health and heal to it.", "11 HP → 22 HP"),
            [SkillType.TripleHealth] = new(SkillFamily.Vitality,
                "At battle start, triple your maximum health and heal to it.", "11 HP → 33 HP"),
            [SkillType.Xue] = new(SkillFamily.Vitality,
                "After your attack deals damage, heal the same amount.", "Deal 3 damage → heal 3"),
            [SkillType.Mo] = new(SkillFamily.Counter,
                "After you take damage, deal half of it, rounded up, as true damage to any target.",
                "Take 3 → retaliate 2"),
            [SkillType.Tong] = new(SkillFamily.Control,
                "At turn start, take an extra number step and choose both of your numbers.",
                "1 and 1 → choose any two"),
            [SkillType.Di] = new(SkillFamily.Control,
                "At turn start, set one of your numbers to any value from 0 to 9.", "1 and 6 → 9 and 6"),
            [SkillType.Xian] = new(SkillFamily.Control,
                "Once your attack is committed, set one number to any value from 0 to 9.", "2 and 5 → 2 and 0"),
            [SkillType.Shen] = new(SkillFamily.Control,
                "After you take damage, set one of your numbers to any value from 0 to 9.", "4 and 7 → 9 and 7"),
            [SkillType.Seng] = new(SkillFamily.Control,
                "After you take damage, take one extra choose-and-add step.", "Extra step: 1 + 8 → 9"),
            [SkillType.Dao] = new(SkillFamily.Control,
                "After your attack ends, take one extra choose-and-add step.", "Extra step: 2 + 6 → 8"),
            [SkillType.SuperGod] = new(SkillFamily.Control,
                "After you take damage, set both of your numbers to any values from 0 to 9.",
                "2 and 8 → 7 and 7"),
            [SkillType.SuperImmortal] = new(SkillFamily.Control,
                "Once your attack is committed, set both numbers to any values from 0 to 9.",
                "2 and 8 → 0 and 5"),
            [SkillType.SuperEmperor] = new(SkillFamily.Control,
                "At turn start, set both numbers to any values from 0 to 9.", "2 and 8 → 0 and 5")
        };

    public static Entry For(SkillType skill) =>
        Entries.TryGetValue(skill, out var entry)
            ? entry
            : new Entry(SkillFamily.Control, "A special battle effect.", "");

    public static bool HasCopy(SkillType skill) => Entries.ContainsKey(skill);

    public static SkillFamily FamilyOf(SkillType skill) => For(skill).Family;

    public static string FamilyLabel(SkillFamily family) => family switch
    {
        SkillFamily.Offense => "OFFENSE",
        SkillFamily.Defense => "DEFENSE",
        SkillFamily.Vitality => "VITALITY",
        SkillFamily.Counter => "COUNTER",
        _ => "CONTROL"
    };

    /// <summary>Player-facing name of the stage the skill fires at.</summary>
    public static string Stage(SkillTrigger trigger) => trigger switch
    {
        SkillTrigger.GameStart => "Battle start",
        SkillTrigger.TurnStart => "Turn start",
        SkillTrigger.OrderingAttack => "Before attack",
        SkillTrigger.OrderedAttack => "Attack committed",
        SkillTrigger.Defending => "When hit",
        SkillTrigger.Damaging => "After dealing damage",
        SkillTrigger.Damaged => "After taking damage",
        SkillTrigger.AttackCompleted => "After the attack ends",
        _ => "In battle"
    };

    public static string Limit(Skill skill) => skill.Usage.UsageLimit switch
    {
        SkillUsageLimit.PerTurn => "once per turn",
        SkillUsageLimit.PerRound => "once per round",
        SkillUsageLimit.Limited => skill.Usage.MaxUses > 1 ? $"up to {skill.Usage.MaxUses} times" : "once per battle",
        _ => "every time"
    };

    /// <summary>Where the skill was earned, first met, or can be earned. Never throws.</summary>
    public static string Source(SkillType skill, bool owned)
    {
        var reward = CampaignCatalog.Nodes.FirstOrDefault(node => node.Rewards.Contains(skill));
        if (owned && reward != null) return "earned at " + Label(reward);

        string firstSeen = SkillCodex.FirstSeen(skill);
        if (!owned && firstSeen.Length > 0)
        {
            var node = CampaignCatalog.GetNode(firstSeen);
            return node == null ? "first seen in a duel" : "first seen at " + Label(node);
        }

        if (reward != null) return "reward at " + Label(reward);
        var fight = CampaignCatalog.Nodes.FirstOrDefault(node =>
            node.Enemies.Any(enemy => enemy.Skills.Contains(skill)) ||
            node.Reinforcements.Any(enemy => enemy.Skills.Contains(skill)));
        if (fight != null) return "fought at " + Label(fight);
        return "online duel";
    }

    public static string Footnote(SkillType skill, bool owned)
    {
        Skill instance = SkillFactory.CreateSkill(skill);
        return $"{Stage(instance.Trigger)} · {Limit(instance)} · {Source(skill, owned)}";
    }

    private static string Label(CampaignNode node) => $"{node.Id} {node.Name}";

    /// <summary>Every selectable skill must have its own page copy.</summary>
    public static IReadOnlyList<SkillType> MissingCopy() =>
        SkillFactory.SelectableSkills.Where(skill => !HasCopy(skill)).ToList();
}
