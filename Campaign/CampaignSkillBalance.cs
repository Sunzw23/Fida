using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>技能费用的唯一来源；UI、校验和测试均读取这里。</summary>
    public static class CampaignSkillBalance
    {
        private static readonly IReadOnlyDictionary<SkillType, int> Costs =
            new Dictionary<SkillType, int>
            {
                [SkillType.PlusAttack] = 2,
                [SkillType.PlusDefence] = 2,
                [SkillType.DoubleHealth] = 2,
                [SkillType.DoubleDefence] = 3,
                [SkillType.Xue] = 3,
                [SkillType.DoubleAttack] = 3,
                [SkillType.TripleHealth] = 3,
                [SkillType.Tong] = 3,
                [SkillType.Mo] = 3,
                [SkillType.Ci] = 2,
                [SkillType.Xian] = 3,
                [SkillType.Shen] = 3,
                [SkillType.Seng] = 2,
                [SkillType.Dao] = 2,
                [SkillType.Di] = 3,
                [SkillType.Bian] = 5,
                [SkillType.Po] = 1,
                [SkillType.Zhen] = 3,
                [SkillType.Shatter] = 2,
                // Drawbacks return budget instead of spending it. Not selectable yet.
                [SkillType.HalfAttack] = -3,
                [SkillType.HalfHealth] = -2,
                [SkillType.Bleed] = -2,
                [SkillType.Long] = 5,
                [SkillType.SuperGod] = 6,
                [SkillType.SuperImmortal] = 6,
                [SkillType.SuperEmperor] = 6,
                [SkillType.Shi] = 2,
            };

        public static int Cost(SkillType type) => Costs.TryGetValue(type, out int cost) ? cost : 99;

        public static int TotalCost(IEnumerable<SkillType> skills)
        {
            int total = 0;
            foreach (var skill in skills) total += Cost(skill);
            return total;
        }

        public static bool Conflicts(IEnumerable<SkillType> selected, SkillType candidate) =>
            (candidate == SkillType.Zhen && Contains(selected, SkillType.Xue)) ||
            (candidate == SkillType.Xue && Contains(selected, SkillType.Zhen)) ||
            (candidate == SkillType.DoubleHealth && Contains(selected, SkillType.TripleHealth)) ||
            (candidate == SkillType.TripleHealth && Contains(selected, SkillType.DoubleHealth)) ||
            (candidate == SkillType.Long && Contains(selected, SkillType.Bian)) ||
            (candidate == SkillType.Bian && Contains(selected, SkillType.Long)) ||
            (candidate == SkillType.SuperGod && Contains(selected, SkillType.Shen)) ||
            (candidate == SkillType.Shen && Contains(selected, SkillType.SuperGod)) ||
            (candidate == SkillType.Shen && Contains(selected, SkillType.Seng)) ||
            (candidate == SkillType.Seng && Contains(selected, SkillType.Shen)) ||
            (candidate == SkillType.SuperImmortal && Contains(selected, SkillType.Xian)) ||
            (candidate == SkillType.Xian && Contains(selected, SkillType.SuperImmortal)) ||
            (candidate == SkillType.Xian && Contains(selected, SkillType.Dao)) ||
            (candidate == SkillType.Dao && Contains(selected, SkillType.Xian)) ||
            (candidate == SkillType.SuperEmperor && Contains(selected, SkillType.Di)) ||
            (candidate == SkillType.Di && Contains(selected, SkillType.SuperEmperor));

        private static bool Contains(IEnumerable<SkillType> skills, SkillType target)
        {
            foreach (var skill in skills) if (skill == target) return true;
            return false;
        }
    }
}
