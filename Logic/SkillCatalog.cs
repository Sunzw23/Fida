using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 目录中单个 <see cref="SkillType"/> 的条目：英文展示名、可选中文名、可创建实例的工厂。
    /// 工厂为 null 表示该类型无具体实现类（Gui/Huo），不参与单机技能选择。
    /// </summary>
    public sealed record SkillEntry(SkillType Type, string EnglishName, string? ChineseName, Func<Skill>? Factory)
    {
        public bool CanCreate => Factory != null;
    }

    /// <summary>
    /// 技能目录：<see cref="SkillType"/> 的单一 C# 事实来源。
    /// 集中维护每种技能的英文名、中文名与对应实现类（工厂），
    /// 供 SkillFactory（可选清单 / 实例创建 / 中文名解析）与 GameText（名称往返）复用，
    /// 消除此前散落在多处、需手工保持一致的映射。
    /// 条目顺序即单机技能选择界面的展示顺序（与既有 SelectableSkills 顺序一致）。
    /// </summary>
    public static class SkillCatalog
    {
        private static readonly SkillEntry[] Entries =
        {
            new(SkillType.PlusAttack, "Attack Up", "加攻击", () => new PlusAttackSkill()),
            new(SkillType.PlusDefence, "Defense Up", "加防御", () => new PlusDefenceSkill()),
            new(SkillType.DoubleHealth, "Double Health", "双倍生命", () => new DoubleHealthSkill()),
            new(SkillType.DoubleAttack, "Double Attack", "双倍攻击", () => new DoubleAttackSkill()),
            new(SkillType.DoubleDefence, "Double Defense", "双倍防御", () => new DoubleDefenceSkill()),
            new(SkillType.TripleHealth, "Triple Health", "三倍生命", () => new TripleHealthSkill()),
            new(SkillType.Shen, "God", "神", () => new ShenSkill()),
            new(SkillType.Xian, "Immortal", "仙", () => new XianSkill()),
            new(SkillType.Seng, "Seng", "僧", () => new SengSkill()),
            new(SkillType.Dao, "Dao", "道", () => new DaoSkill()),
            new(SkillType.Mo, "Demon", "魔", () => new MoSkill()),
            new(SkillType.Ci, "Thorn", "刺", () => new CiSkill()),
            new(SkillType.Xue, "Blood", "血", () => new XueSkill()),
            new(SkillType.Tong, "Same", "同", () => new TongSkill()),
            new(SkillType.Di, "Emperor", "帝", () => new DiSkill()),
            new(SkillType.Long, "Dragon", "龙", () => new LongSkill()),
            new(SkillType.Bian, "Transform", "变", () => new BianSkill()),
            new(SkillType.Po, "Pierce", "破", () => new PoSkill()),
            new(SkillType.Zhen, "Truth", "真", () => new ZhenSkill()),
            new(SkillType.Shatter, "Shatter", "裂", () => new ShatterSkill()),
            new(SkillType.SuperGod, "Super God", "超神", () => new SuperGodSkill()),
            new(SkillType.SuperImmortal, "Super Immortal", "超仙", () => new SuperImmortalSkill()),
            new(SkillType.SuperEmperor, "Super Emperor", "超帝", () => new SuperEmperorSkill()),
            new(SkillType.Shi, "Stone", "石", () => new StoneSkill()),
            new(SkillType.Gui, "Ghost", null, null),
            new(SkillType.Huo, "Fire", null, null),
            // Drawbacks: named and priced, deliberately without an implementation so they stay
            // out of SelectableSkills and never reach a loadout. Wiring them is a separate step.
            new(SkillType.HalfAttack, "Half Attack", "攻击减半", null),
            new(SkillType.HalfHealth, "Half Health", "生命减半", null),
            new(SkillType.Bleed, "Bleed", "失血", null),
        };

        private static readonly IReadOnlyDictionary<SkillType, SkillEntry> ByType =
            Entries.ToDictionary(e => e.Type);

        private static readonly List<SkillType> SelectableList =
            Entries.Where(e => e.CanCreate).Select(e => e.Type).ToList();

        /// <summary>全部条目（保持展示顺序）。</summary>
        public static IReadOnlyList<SkillEntry> All => Entries;

        /// <summary>单机可选技能清单（保持展示顺序，排除无实现类的类型）。</summary>
        public static IReadOnlyList<SkillType> Selectable => SelectableList;

        /// <summary>按类型取条目；对全部合法 <see cref="SkillType"/> 均存在。</summary>
        public static SkillEntry Get(SkillType type) => ByType[type];

        /// <summary>是否有可创建的具体实现类。</summary>
        public static bool CanCreate(SkillType type) => Get(type).CanCreate;

        /// <summary>英文展示名。</summary>
        public static string EnglishName(SkillType type) => Get(type).EnglishName;

        /// <summary>中文名（无对应中文名时为 null）。</summary>
        public static string? ChineseName(SkillType type) => Get(type).ChineseName;

        /// <summary>
        /// 由英文名或中文名解析技能类型；未匹配或空名返回 null。
        /// </summary>
        public static SkillType? FromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            foreach (var entry in Entries)
            {
                if (string.Equals(entry.EnglishName, name, StringComparison.Ordinal) ||
                    (entry.ChineseName != null && string.Equals(entry.ChineseName, name, StringComparison.Ordinal)))
                {
                    return entry.Type;
                }
            }
            return null;
        }
    }
}
