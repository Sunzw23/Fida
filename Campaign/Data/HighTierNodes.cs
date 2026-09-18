using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Campaign
{
    public static partial class CampaignCatalog
    {
        static partial void AddHighTierBranches(List<CampaignNode> nodes)
        {
            nodes.Add(Node("D1", "Chapter III - Dragon Tomb", "Dragon Tomb Gate", "The dragon is an ancient engine that predicts death by addition.",
                12, 220, 780, ["BG"], 1, [],
                [Enemy("Broken Dragon Head", 7, skills: [SkillType.Long]), Enemy("Left Dragonbone", 5), Enemy("Right Dragonbone", 5)]));

            nodes.Add(Node("D2", "Chapter III - Dragon Tomb", "Dragonbone Array", "The array predicted defeat and drove the King to invoke Zero.",
                14, 390, 900, ["D1"], 1, [SkillType.Long],
                [Enemy("Dragonbone Array", 10, skills: [SkillType.Long]), Enemy("Warding Dragonbone", 5, skills: [SkillType.DoubleDefence])],
                [Enemy("Heir Dragon Head", 7, skills: [SkillType.Long])], reinforcementDeaths: 1,
                boss: true, trueEndingRequired: true));

            nodes.Add(Node("E1", "Chapter III - Blank Archive", "The Blank Archive", "The histories survived, but every number was changed to zero.",
                12, 1300, 780, ["CG"], 1, [],
                [Enemy("Ink Clerk A", 5, skills: [SkillType.Bian]), Enemy("Ink Clerk B", 5), Enemy("Paper Guard", 6, skills: [SkillType.DoubleDefence])]));

            nodes.Add(Node("E2", "Chapter III - Blank Archive", "The Thousand-Faced Scribe", "The Scribe can alter numbers, but not what truly happened.",
                14, 1260, 900, ["E1"], 1, [SkillType.Bian],
                [Enemy("Thousand-Faced Scribe", 10, skills: [SkillType.Bian]), Enemy("Paper Guard", 5)],
                [Enemy("Ink Echo A", 5, skills: [SkillType.PlusAttack]), Enemy("Ink Echo B", 5, skills: [SkillType.PlusDefence])],
                reinforcementDeaths: 1, boss: true, trueEndingRequired: true));
        }
    }
}
