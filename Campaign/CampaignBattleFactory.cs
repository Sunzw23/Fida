using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>把纯战役数据转换成现有战斗领域对象。</summary>
    public static class CampaignBattleFactory
    {
        public static CampaignBattleSetup Create(CampaignNode node, IEnumerable<SkillType> selectedSkills)
        {
            var localSkills = (selectedSkills ?? []).OrderBy(s => (int)s).Select(SkillFactory.CreateSkill).ToList();
            var local = new Player(0, "The Arithmetist", 11, localSkills)
            {
                PortraitKey = "arithmetist"
            };
            var players = new List<Player> { local };
            var reinforcements = new List<Player>();
            var agentTypes = new Dictionary<int, CampaignAgentType>();

            int nextId = 1;
            foreach (var definition in node.Enemies)
            {
                var enemy = CreateEnemy(nextId++, definition);
                players.Add(enemy);
                agentTypes[enemy.PlayerId] = definition.AgentType;
            }
            foreach (var definition in node.Reinforcements)
            {
                var enemy = CreateEnemy(nextId++, definition);
                reinforcements.Add(enemy);
                agentTypes[enemy.PlayerId] = definition.AgentType;
            }

            var groups = new List<Group>
            {
                new(0, "The Arithmetist", [local]),
                new(1, "Echoes of Zero", players.Skip(1))
            };

            return new CampaignBattleSetup
            {
                Node = node,
                Players = players,
                Groups = groups,
                Reinforcements = reinforcements,
                AgentTypes = agentTypes
            };
        }

        private static Player CreateEnemy(int id, CampaignEnemyDefinition definition)
        {
            var skills = definition.Skills.Select(SkillFactory.CreateSkill).ToList();
            var player = new Player(id, definition.Name, definition.Health, skills);
            player.PortraitKey = definition.PortraitKey;
            player.SetNumber(0, definition.Number0);
            player.SetNumber(1, definition.Number1);
            return player;
        }
    }
}
