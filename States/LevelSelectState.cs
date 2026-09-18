using Microsoft.Xna.Framework;
using Fida.Logic;
using Fida.UI;
using Fida.Campaign;
using Fida.UI.Campaign;

namespace Fida.States
{
    /// <summary>
    /// 关卡选择状态：列出 <c>Content/scenes/*.csv</c> 中的全部场景，
    /// 选择一个场景后进入技能选择。按钮名称与场景文件同名，便于按名绑定。
    /// </summary>
    public class LevelSelectState : StateBase
    {
        public LevelSelectState(UIManager uiManager, Network.NetworkManager networkManager, GameStateMachine stateMachine)
            : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            // The campaign map is a composite browser; it exposes intents instead of
            // requiring one UI binding for every data-driven node.
        }

        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);
            var map = _uiManager.GetControlByName<UICampaignMap>("campaign_map");
            if (map == null) return;

            if (map.ConsumeBackRequest())
            {
                _stateMachine.SwitchToState(State.MainMenu);
                return;
            }

            if (map.TryConsumeSelectedNode(out string nodeId) && GameSetup.SelectCampaignNode(nodeId))
                _stateMachine.SwitchToState(State.Skills);
        }
    }
}
