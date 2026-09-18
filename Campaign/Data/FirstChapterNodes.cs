using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Campaign
{
    public static partial class CampaignCatalog
    {
        static partial void AddFirstChapter(List<CampaignNode> nodes)
        {
            nodes.Add(Node("A1", "Chapter I", "Rustblade Arsenal", "The arsenal still arms a war that ended long ago.",
                3, 310, 235, ["P"], 1, [SkillType.DoubleAttack],
                [Enemy("Forge Guard A", 4, n0: 6, skills: [SkillType.PlusAttack]), Enemy("Forge Guard B", 4), Enemy("Blade Inspector", 3)]));

            nodes.Add(Node("A2", "Chapter I", "White Stone Gate", "The sentries guard no survivors, only an empty city.",
                3, 760, 235, ["P"], 1, [SkillType.DoubleDefence],
                [Enemy("White Shield", 6, skills: [SkillType.DoubleDefence]), Enemy("Crossbow Statue A", 4), Enemy("Crossbow Statue B", 4)]));

            nodes.Add(Node("A3", "Chapter I", "Silent Hospice", "The physician erased pain, and every name with it.",
                3, 1210, 235, ["P"], 1, [SkillType.DoubleHealth],
                [Enemy("Silent Physician", 4), Enemy("Patient A", 3), Enemy("Patient B", 3)]));

            nodes.Add(Node("AG", "Chapter I", "The Nameless General", "The General guards the first memory of the Zero Decree.",
                5, 760, 355, ["A1", "A2", "A3"], 2, [SkillType.TripleHealth],
                [Enemy("Nameless General", 8, skills: [SkillType.DoubleAttack]), Enemy("Broken Shieldguard", 5, skills: [SkillType.PlusDefence])],
                [Enemy("Rustblade Reinforcement A", 3), Enemy("Rustblade Reinforcement B", 3)], reinforcementDeaths: 1, boss: true));
        }
    }
}
