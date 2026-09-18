using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Campaign
{
    public static partial class CampaignCatalog
    {
        static partial void AddBellTowerBranch(List<CampaignNode> nodes)
        {
            nodes.Add(Node("B1", "Chapter II - Bell Tower", "Blood Hunt", "The hunters forgot their prey, but remember how to survive.",
                7, 100, 500, ["AG"], 1, [SkillType.Xue],
                [Enemy("Blood Hunter A", 4, skills: [SkillType.Xue]), Enemy("Blood Hunter B", 4), Enemy("Blood Hunter C", 4)]));

            nodes.Add(Node("B2", "Chapter II - Bell Tower", "Twin Bell Chamber", "The twins share one memory that refuses to end.",
                7, 390, 500, ["AG"], 1, [SkillType.Tong],
                [Enemy("First Bellkeeper", 5, n0: 2, n1: 2, skills: [SkillType.Tong]), Enemy("Second Bellkeeper", 5)],
                [Enemy("Bell Wraith", 4, skills: [SkillType.PlusAttack])], reinforcementRound: 3));

            nodes.Add(Node("B3", "Chapter II - Bell Tower", "Retaliation Idols", "The idols return every wound to the other side.",
                7, 680, 500, ["AG"], 1, [SkillType.Mo],
                [Enemy("Retaliation Golem", 7, skills: [SkillType.Mo]), Enemy("Idol A", 3), Enemy("Idol B", 3)]));

            nodes.Add(Node("BG", "Chapter II - Bell Tower", "The Blood Bell", "Let the war's final day end for the first time.",
                10, 390, 635, ["B1", "B2", "B3"], 2, [],
                [Enemy("Bell Matriarch", 9, skills: [SkillType.Xue]), Enemy("Hunter Echo", 4)],
                [Enemy("Bell Echo A", 4, skills: [SkillType.Xue]), Enemy("Bell Echo B", 4)],
                reinforcementDeaths: 1, boss: true, trueEndingRequired: true));
        }
    }
}
