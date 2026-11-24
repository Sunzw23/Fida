using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;

namespace Fida.Logic
{
    /// <summary>
    /// 单机开局设置：记录玩家选择的场景与本地玩家技能，并据此加载玩家列表。
    /// 作为 <c>GameStateMachine</c> 与 <c>GameSessionManager</c> 两个单机入口的唯一玩家来源。
    /// </summary>
    public static class GameSetup
    {
        private static readonly HashSet<SkillType> SelectedSkills = [];
        private static readonly List<string> SelectedCompanions = [];

        /// <summary>当前树状战役节点。</summary>
        public static string SelectedNodeId { get; private set; } = "P";

        public static CampaignNode SelectedNode => CampaignCatalog.GetNode(SelectedNodeId) ?? CampaignCatalog.Nodes[0];

        public static IReadOnlyCollection<SkillType> SelectedLocalSkills => SelectedSkills;

        /// <summary>本场战斗邀请的同伴 id（按邀请顺序）；空表示独自作战（默认）。</summary>
        public static IReadOnlyCollection<string> SelectedCompanionIds => SelectedCompanions;

        /// <summary>该节点的战前简报是否已经播过；重试或返回配装时不再重复播放。</summary>
        public static bool NodeBriefingShown { get; private set; }

        public static void MarkNodeBriefingShown() => NodeBriefingShown = true;

        /// <summary>当前选择的场景名（默认 DefaultScene）。</summary>
        public static string SelectedSceneName { get; set; } = "DefaultScene";

        /// <summary>本地玩家（id 0）最终使用的技能；为 null 时沿用该场景 CSV 中的默认技能。</summary>
        public static SkillType? SelectedLocalSkill
        {
            get => SelectedSkills.Count == 1 ? SelectedSkills.First() : null;
            set
            {
                SelectedSkills.Clear();
                if (value.HasValue) SelectedSkills.Add(value.Value);
            }
        }

        /// <summary>
        /// 按当前选择加载单机玩家列表：加载所选场景，并将本地玩家（id 0）的技能
        /// 替换为所选技能（未选则保留场景默认）。
        /// </summary>
        public static List<Player> LoadLocalPlayers()
        {
            if (CampaignCatalog.GetNode(SelectedNodeId) is { } campaignNode)
                return CampaignBattleFactory.Create(campaignNode, SelectedSkills,
                    companionIds: SelectedCompanions).Players;

            string sceneName = SceneLibrary.SceneNames().Contains(SelectedSceneName) ? SelectedSceneName : "DefaultScene";
            var players = SceneLibrary.LoadScene(sceneName);

            if (SelectedLocalSkill is { } skillType)
            {
                var local = players.FirstOrDefault(p => p.PlayerId == 0);
                if (local != null)
                    local.ReplaceSkills(new List<Skill> { SkillFactory.CreateSkill(skillType) });
            }

            return players;
        }

        /// <summary>
        /// 取某场景的默认技能（本地玩家 id 0 的中文技能，若其存在）；用于技能选择界面的预选高亮。
        /// </summary>
        public static SkillType? GetDefaultLocalSkill(string sceneName)
        {
            var local = SceneLibrary.LoadScene(sceneName).FirstOrDefault(p => p.PlayerId == 0);
            if (local == null || local.Skills.Count == 0)
                return null;
            return local.Skills[0].Type;
        }

        /// <summary>切换场景并重置本地技能选择（由技能选择界面在确认时落定具体技能）。</summary>
        public static void SelectScene(string sceneName)
        {
            SelectedSceneName = sceneName;
            SelectedLocalSkill = null;
        }

        public static bool SelectCampaignNode(string nodeId, bool online = false)
        {
            var node = CampaignCatalog.GetNode(nodeId);
            if (node == null || !CampaignCatalog.IsUnlocked(CampaignRuntime.GetProgress(online), node)) return false;
            SelectedNodeId = node.Id;
            SelectedSkills.Clear();
            // Every battle starts with an empty party; the player opts in on the companion screen.
            SelectedCompanions.Clear();
            NodeBriefingShown = false;
            return true;
        }

        /// <summary>邀请/取消一名同伴；不在节点阵容内或已满编时被忽略。</summary>
        public static bool ToggleCampaignCompanion(string? companionId)
        {
            var next = CampaignLoadout.ToggleCompanion(SelectedNode, SelectedCompanions, companionId);
            if (next.Count == SelectedCompanions.Count && next.SequenceEqual(SelectedCompanions))
                return false;
            SelectedCompanions.Clear();
            SelectedCompanions.AddRange(next);
            return true;
        }

        /// <summary>用一组 id 覆盖本场同伴（非法项与超额项会被丢弃）。</summary>
        public static void SetCampaignCompanions(IEnumerable<string>? companionIds)
        {
            var next = CampaignLoadout.SanitizeCompanions(SelectedNode, companionIds);
            SelectedCompanions.Clear();
            SelectedCompanions.AddRange(next);
        }

        public static void ClearCampaignCompanions() => SelectedCompanions.Clear();

        public static bool ToggleCampaignSkill(SkillType skill, bool online = false)
        {
            if (SelectedSkills.Remove(skill)) return true;
            if (!CampaignLoadout.CanSelect(CampaignRuntime.GetProgress(online), SelectedNode, SelectedSkills, skill)) return false;
            SelectedSkills.Add(skill);
            return true;
        }

        public static int SelectedSkillCost => CampaignSkillBalance.TotalCost(SelectedSkills);

        public static CampaignBattleSetup LoadCampaignBattle() =>
            CampaignBattleFactory.Create(SelectedNode, SelectedSkills,
                companionIds: SelectedCompanions);
    }
}
