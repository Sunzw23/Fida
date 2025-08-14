using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    // 定义一个委托，用于处理开始按钮点击事件。
    public delegate void StartButtonHandler();
    // 定义一个委托，用于处理选择数字事件，传递被选择的数字。
    public delegate void ChooseNumberIndexHandler(int playerId, int numberIndex);
    // 定义一个委托，用于处理目标选择事件。
    public delegate void ChooseTargetHandler(int playerId);

    public struct Action
    {
        public ActionType Type { get; set; }
        public int TargetPlayerId { get; set; }
        public int NumberIndex { get; set; }
        public int Number { get; set; }
        public bool IsApply { get; set; }

        public Action()
        {
            this.Type = ActionType.None;
        }
    }

    public enum ActionType
    {
        ChooseNumberIndex,
        ChooseTarget,
        DecideApply,
        ChooseNumber,
        None,
    }
}
