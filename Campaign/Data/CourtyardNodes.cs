using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Campaign
{
    public static partial class CampaignCatalog
    {
        static partial void AddCourtyardBranch(List<CampaignNode> nodes)
        {
            nodes.Add(Node("C1", "Chapter II - Courtyard", "Altar of the Broken God", "The priest believes only the wounded may change fate.",
                8, 970, 500, ["AG"], 1, [SkillType.Shen],
                [Enemy("Broken God Priest", 6, skills: [SkillType.Shen]), Enemy("Acolyte A", 3), Enemy("Acolyte B", 3)]));

            nodes.Add(Node("C2", "Chapter II - Courtyard", "Deathless Gallery", "The immortals rewrite every outcome, so their battle never ends.",
                8, 1260, 500, ["AG"], 1, [SkillType.Xian],
                [Enemy("Immortal A", 5, skills: [SkillType.Xian]), Enemy("Immortal B", 5), Enemy("Gallery Sentinel", 6, skills: [SkillType.PlusDefence])]));

            nodes.Add(Node("C3", "Chapter II - Courtyard", "Court of the False Emperor", "A ruler without subjects still decrees new laws of number.",
                8, 1550, 500, ["AG"], 1, [SkillType.Di],
                [Enemy("False Emperor", 7, skills: [SkillType.Di]), Enemy("Civil Retainer", 4), Enemy("War Retainer", 5, skills: [SkillType.PlusAttack])]));

            nodes.Add(Node("CG", "Chapter II - Courtyard", "The Threefold Oracle", "The Zero King is the will the old King chose to abandon.",
                11, 1260, 635, ["C1", "C2", "C3"], 2, [],
                [Enemy("Threefold Oracle", 10, skills: [SkillType.Shen]), Enemy("Oracle Attendant", 4)],
                [Enemy("The Third Aspect", 6, skills: [SkillType.Xian])], reinforcementDeaths: 1,
                boss: true, trueEndingRequired: true));
        }
    }
}
