using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Campaign
{
    public static partial class CampaignCatalog
    {
        static partial void AddPrologue(List<CampaignNode> nodes)
        {
            nodes.Add(Node("P", "Prologue", "The Withered King", "The King repeats the Crown's last commands, then falls silent.",
                0, 860, 105, [], 0, [SkillType.PlusAttack, SkillType.PlusDefence],
                [new CampaignEnemyDefinition
                {
                    Name = "The Withered King",
                    PortraitKey = "withered_king",
                    Health = 3,
                    Number0 = 1,
                    Number1 = 6,
                    AgentType = CampaignAgentType.Scripted,
                    Skills = [SkillType.PlusAttack, SkillType.PlusDefence]
                }]));
        }
    }
}
