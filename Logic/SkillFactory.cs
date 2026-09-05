using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 技能工厂：按 <see cref="SkillType"/> 创建具体技能实例，并提供可选技能清单与中文名称解析。
    /// 仅覆盖拥有具体实现类的 14 种技能；Gui/Huo/Shi 无实现类，不参与单机技能选择。
    /// </summary>
    public static class SkillFactory
    {
        /// <summary>
        /// 单机可选的 14 种技能（保持 <see cref="SkillType"/> 声明顺序）。
        /// </summary>
        public static readonly IReadOnlyList<SkillType> SelectableSkills =
        [
            SkillType.PlusAttack,
            SkillType.PlusDefence,
            SkillType.DoubleHealth,
            SkillType.DoubleAttack,
            SkillType.DoubleDefence,
            SkillType.TripleHealth,
            SkillType.Shen,
            SkillType.Xian,
            SkillType.Mo,
            SkillType.Xue,
            SkillType.Tong,
            SkillType.Di,
            SkillType.Long,
            SkillType.Bian,
        ];

        /// <summary>
        /// 按技能类型创建实例；无实现类的类型抛出 <see cref="InvalidOperationException"/>。
        /// </summary>
        public static Skill CreateSkill(SkillType type) => type switch
        {
            SkillType.PlusAttack => new PlusAttackSkill(),
            SkillType.PlusDefence => new PlusDefenceSkill(),
            SkillType.DoubleHealth => new DoubleHealthSkill(),
            SkillType.DoubleAttack => new DoubleAttackSkill(),
            SkillType.DoubleDefence => new DoubleDefenceSkill(),
            SkillType.TripleHealth => new TripleHealthSkill(),
            SkillType.Shen => new ShenSkill(),
            SkillType.Xian => new XianSkill(),
            SkillType.Mo => new MoSkill(),
            SkillType.Xue => new XueSkill(),
            SkillType.Tong => new TongSkill(),
            SkillType.Di => new DiSkill(),
            SkillType.Long => new LongSkill(),
            SkillType.Bian => new BianSkill(),
            _ => throw new InvalidOperationException($"SkillType {type} 没有对应的技能实现。")
        };

        /// <summary>
        /// 由中文技能名解析并创建技能实例；解析失败（空名/未知名）返回 null，不抛异常。
        /// </summary>
        public static Skill CreateSkillFromChineseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            SkillType? type = ParseSkillType(name);
            if (type == null)
                return null;

            return SelectableSkills.Contains(type.Value)
                ? CreateSkill(type.Value)
                : null;
        }

        /// <summary>
        /// 解析中文技能名到 <see cref="SkillType"/>；优先复用 <see cref="GameText"/> 的中英文映射，
        /// 再回退到内置中文→技能表（应对 gametext.json 缺失的情况）。解析失败返回 null。
        /// </summary>
        public static SkillType? ParseSkillType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // 首选 GameText 的正/反向映射（与 gametext.json 一致）
            try
            {
                return GameText.SkillTypeFromString(name);
            }
            catch
            {
                // GameText 解析失败（未知名）时落到内置表
            }

            // 回退：内置中文→技能表（不依赖 gametext.json）
            foreach (var kv in BuiltinChineseNames)
            {
                if (kv.Key == name)
                    return kv.Value;
            }

            return null;
        }

        // 与 gametext.json 的 skillNames 保持一致，保证在 gametext.json 缺失时 CSV 中文技能仍可解析
        private static readonly Dictionary<string, SkillType> BuiltinChineseNames = new()
        {
            { "加攻击", SkillType.PlusAttack },
            { "加防御", SkillType.PlusDefence },
            { "双倍生命", SkillType.DoubleHealth },
            { "双倍攻击", SkillType.DoubleAttack },
            { "双倍防御", SkillType.DoubleDefence },
            { "三倍生命", SkillType.TripleHealth },
            { "神", SkillType.Shen },
            { "仙", SkillType.Xian },
            { "魔", SkillType.Mo },
            { "血", SkillType.Xue },
            { "同", SkillType.Tong },
            { "帝", SkillType.Di },
            { "龙", SkillType.Long },
            { "变", SkillType.Bian },
        };
    }
}
