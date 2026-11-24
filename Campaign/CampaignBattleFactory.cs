using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>把纯战役数据转换成现有战斗领域对象。</summary>
    public static class CampaignBattleFactory
    {
        /// <summary>
        /// <paramref name="companionIds"/> is the party the player invited on the companion
        /// screen, in invitation order. Unknown, forbidden or over-limit ids are ignored, and
        /// an empty party fights alone.
        /// </summary>
        public static CampaignBattleSetup Create(CampaignNode node, IEnumerable<SkillType> selectedSkills,
            int localPlayerId = 0, IEnumerable<string>? companionIds = null) =>
            CreateInternal(node, selectedSkills, null, localPlayerId, companionIds);

        public static CampaignBattleSetup CreateCooperative(CampaignNode node,
            IEnumerable<SkillType> playerZeroSkills, IEnumerable<SkillType> playerOneSkills) =>
            // The human partner occupies the second ally slot, so no campaign companion joins.
            CreateInternal(node, playerZeroSkills, playerOneSkills ?? [], 0, null);

        private static CampaignBattleSetup CreateInternal(CampaignNode node,
            IEnumerable<SkillType> selectedSkills, IEnumerable<SkillType>? partnerSkills,
            int localPlayerId, IEnumerable<string>? companionIds)
        {
            var localSkills = (selectedSkills ?? []).OrderBy(s => (int)s).Select(SkillFactory.CreateSkill).ToList();
            localPlayerId = partnerSkills == null ? 0 : Math.Clamp(localPlayerId, 0, 1);
            // A side chapter may start the player wounded; everything else uses the default 11.
            int localHealth = Math.Clamp(node.PlayerHealth, 1, CampaignDifficulty.DefaultPlayerHealth);
            var local = new Player(localPlayerId, CampaignPlayerName(localPlayerId), localHealth, localSkills)
            {
                PortraitKey = "arithmetist"
            };
            var players = new List<Player> { local };
            var agentTypes = new Dictionary<int, CampaignAgentType>();
            Player? partner = null;
            var companions = new List<Player>();
            int nextId = 1;
            if (partnerSkills != null)
            {
                partner = new Player(1 - localPlayerId, CampaignPlayerName(1 - localPlayerId), 11,
                    partnerSkills.Distinct().OrderBy(skill => (int)skill)
                        .Select(SkillFactory.CreateSkill).ToList()) { PortraitKey = "arithmetist" };
                players.Add(partner);
                nextId = 2;
            }
            else
            {
                foreach (var invited in InvitedCompanions(node, companionIds))
                {
                    var ally = new Player(nextId++, invited.Name, invited.Health,
                        invited.Skills.Distinct().OrderBy(skill => (int)skill)
                            .Select(SkillFactory.CreateSkill).ToList())
                    {
                        PortraitKey = invited.PortraitKey
                    };
                    players.Add(ally);
                    companions.Add(ally);
                    agentTypes[ally.PlayerId] = invited.AgentType;
                }
            }
            var reinforcements = new List<Player>();

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

            var allies = new List<Player> { local };
            if (partner != null) allies.Add(partner);
            allies.AddRange(companions);
            var groups = new List<Group>
            {
                new(0, "The Arithmetists", allies.OrderBy(player => player.PlayerId)),
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
            ? "Arith"
            : $"Player {playerId + 1}";

        /// <summary>The node's roster rules decide who may join and in which order.</summary>
        private static IEnumerable<CampaignCompanion> InvitedCompanions(CampaignNode node,
            IEnumerable<string>? companionIds) =>
            CampaignLoadout.SanitizeCompanions(node, companionIds)
                .Select(CampaignCompanions.Get)
                .OfType<CampaignCompanion>();

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
