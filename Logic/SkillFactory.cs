using System;
using System.Collections.Generic;

namespace Fida.Logic
{
    /// <summary>
    /// 技能工厂：按 <see cref="SkillType"/> 创建具体技能实例，并提供可选技能清单与技能名解析。
    /// 所有名称/类型映射均派生自 <see cref="SkillCatalog"/>（单一事实来源）。
    /// </summary>
    public static class SkillFactory
    {
        /// <summary>
        /// 单机可选技能清单（派生自 <see cref="SkillCatalog"/>，保持展示顺序，排除无实现类的类型）。
        /// </summary>
        public static readonly IReadOnlyList<SkillType> SelectableSkills = SkillCatalog.Selectable;

        /// <summary>
        /// 按技能类型创建实例；无实现类的类型抛出 <see cref="InvalidOperationException"/>。
        /// </summary>
        public static Skill CreateSkill(SkillType type)
        {
            var factory = SkillCatalog.Get(type).Factory;
            return (factory ?? throw new InvalidOperationException($"SkillType {type} has no skill implementation.")).Invoke();
        }

        /// <summary>
        /// 由技能名（中/英文）解析并创建技能实例；解析失败（空名/未知名）或无实现类时返回 null，不抛异常。
        /// </summary>
        public static Skill CreateSkillFromChineseName(string name)
        {
            var type = ParseSkillType(name);
            if (type == null)
                return null;

            return SkillCatalog.CanCreate(type.Value)
                ? CreateSkill(type.Value)
                : null;
        }

        /// <summary>
        /// 解析技能名（中/英文）到 <see cref="SkillType"/>，派生自 <see cref="SkillCatalog"/>。解析失败返回 null。
        /// </summary>
        public static SkillType? ParseSkillType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return SkillCatalog.FromName(name);
        }
    }
}
