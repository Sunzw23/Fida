using System;
using System.Linq;
using Fida.Logic;
using Fida.UI;

namespace Fida.States
{
    /// <summary>
    /// 技能选择状态：选择本地玩家使用的技能，确认后进入对局。
    /// 默认高亮所选场景中本地玩家（id 0）的 CSV 技能。
    /// </summary>
    public class SkillSelectState : StateBase
    {
        public SkillSelectState(UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            // 预选该场景本地玩家的默认技能作为当前选择
            var defaultSkill = GameSetup.GetDefaultLocalSkill(GameSetup.SelectedSceneName);
            Highlight(defaultSkill);

            foreach (var type in SkillFactory.SelectableSkills)
            {
                SkillType chosen = type;
                BindButton("skill_" + chosen, () =>
                {
                    GameSetup.SelectedLocalSkill = chosen;
                    Highlight(chosen);
                });
            }

            BindButton("back", () => _stateMachine.SwitchToState(State.LevelSelect));
            BindButton("confirm", () => _stateMachine.SwitchToState(State.Playing));
        }

        /// <summary>高亮指定技能按钮，其余取消高亮；<c>null</c> 表示全部取消。</summary>
        private void Highlight(SkillType? selected)
        {
            foreach (var type in SkillFactory.SelectableSkills)
            {
                var btn = _uiManager.GetButtonByName("skill_" + type);
                if (btn != null)
                    btn.SetSelected(type == selected);
            }
        }
    }
}
