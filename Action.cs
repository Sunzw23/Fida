using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{

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
