using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>把纯战役数据转换成现有战斗领域对象。</summary>
    public static class CampaignBattleFactory
    {
        public static CampaignBattleSetup Create(CampaignNode node, IEnumerable<SkillType> selectedSkills,
            int localPlayerId = 0)
            => CreateInternal(node, selectedSkills, null, localPlayerId);

        public static CampaignBattleSetup CreateCooperative(CampaignNode node,
            IEnumerable<SkillType> playerZeroSkills, IEnumerable<SkillType> playerOneSkills) =>
            CreateInternal(node, playerZeroSkills, playerOneSkills ?? [], 0);

        private static CampaignBattleSetup CreateInternal(CampaignNode node,
            IEnumerable<SkillType> selectedSkills, IEnumerable<SkillType> partnerSkills, int localPlayerId)
        {
            var localSkills = (selectedSkills ?? []).OrderBy(s => (int)s).Select(SkillFactory.CreateSkill).ToList();
            localPlayerId = partnerSkills == null ? 0 : Math.Clamp(localPlayerId, 0, 1);
            var local = new Player(localPlayerId, CampaignPlayerName(localPlayerId), 11, localSkills)
            {
                PortraitKey = "arithmetist"
            };
            var players = new List<Player> { local };
            Player partner = null;
            Player tutorialCompanion = null;
            if (partnerSkills != null)
            {
                partner = new Player(1 - localPlayerId, CampaignPlayerName(1 - localPlayerId), 11,
                    partnerSkills.Distinct().OrderBy(skill => (int)skill)
                        .Select(SkillFactory.CreateSkill).ToList()) { PortraitKey = "arithmetist" };
                players.Add(partner);
            }
            var reinforcements = new List<Player>();
            var agentTypes = new Dictionary<int, CampaignAgentType>();

            if (partner == null && !string.IsNullOrWhiteSpace(node.TutorialCompanion))
            {
                tutorialCompanion = new Player(1, node.TutorialCompanion, 8, [])
                {
                    PortraitKey = "tutorial_ally"
                };
                players.Add(tutorialCompanion);
                agentTypes[tutorialCompanion.PlayerId] = CampaignAgentType.Rule;
            }

            int nextId = partner != null || tutorialCompanion != null ? 2 : 1;
            int enemyStartId = nextId;
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

            // Keep roster order identical on host and client. GameManager starts at
            // index zero, so local-first ordering makes the two peers start different turns.
            players.Sort((left, right) => left.PlayerId.CompareTo(right.PlayerId));

            var groups = new List<Group>
            {
                new(0, "The Arithmetists", (partner != null ? new[] { local, partner }
                        : tutorialCompanion != null ? new[] { local, tutorialCompanion }
                        : new[] { local })
                    .OrderBy(player => player.PlayerId)),
                new(1, "Echoes of Zero", players.Where(player => player.PlayerId >= enemyStartId))
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

        private static string CampaignPlayerName(int playerId) => playerId == 0
            ? "The Arithmetist"
            : $"Player {playerId + 1}";


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
