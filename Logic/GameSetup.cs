using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 单机开局设置：记录玩家选择的场景与本地玩家技能，并据此加载玩家列表。
    /// 作为 <c>GameStateMachine</c> 与 <c>GameSessionManager</c> 两个单机入口的唯一玩家来源。
    /// </summary>
    public static class GameSetup
    {
        /// <summary>当前选择的场景名（默认 DefaultScene）。</summary>
        public static string SelectedSceneName { get; set; } = "DefaultScene";

        /// <summary>本地玩家（id 0）最终使用的技能；为 null 时沿用该场景 CSV 中的默认技能。</summary>
        public static SkillType? SelectedLocalSkill { get; set; }

        /// <summary>
        /// 按当前选择加载单机玩家列表：加载所选场景，并将本地玩家（id 0）的技能
        /// 替换为所选技能（未选则保留场景默认）。
        /// </summary>
        public static List<Player> LoadLocalPlayers()
        {
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
    }
}
