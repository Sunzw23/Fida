using System;
using System.Linq;
using Fida.Logic;
using Fida.UI;

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
            var scenes = SceneLibrary.SceneNames();
            foreach (var scene in scenes)
            {
                string name = scene;
                BindButton(name, () =>
                {
                    GameSetup.SelectScene(name);
                    _stateMachine.SwitchToState(State.Skills);
                });
            }

            BindButton("back", () => _stateMachine.SwitchToState(State.MainMenu));
        }
    }
}
