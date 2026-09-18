using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Campaign
{
    public static partial class CampaignCatalog
    {
        static partial void AddFinale(List<CampaignNode> nodes)
        {
            nodes.Add(Node("F", "Finale", "The Crown of Zero", "Every path returns to the silent throne.",
                16, 820, 1030, ["D2", "E2"], 1, [],
                [Enemy("The Zero King", 12, skills: [SkillType.Di, SkillType.Shen]),
                 Enemy("Echo of Assault", 5, skills: [SkillType.DoubleAttack]),
                 Enemy("Echo of Defense", 5, skills: [SkillType.DoubleDefence])],
                [Enemy("Crown Core", 9, skills: [SkillType.Bian])], reinforcementDeaths: 2, boss: true));
        }
    }
}
