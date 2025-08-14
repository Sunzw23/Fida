using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    // 游戏事件参数基类
    public class GameEventArgs : EventArgs
    {
        public int SourcePlayerId { get; set; }
        public int TargetPlayerId { get; set; }
        public string Message { get; set; }
    }

    // 具体事件参数类
    public class GameStartEventArgs : GameEventArgs { }

    public class TurnStartEventArgs : GameEventArgs { }

    public class NumberChangeEventArgs : GameEventArgs
    {
        public int NumberIndex { get; set; }
        public int OldValue { get; set; }
        public int NewValue { get; set; }
    }

    public class AttackEventArgs : GameEventArgs
    {
        public int DamageValue { get; set; }
    }

    public class HealEventArgs : GameEventArgs
    {
        public int HealingValue { get; set; }
    }

    public class SkillActivatedEventArgs : GameEventArgs
    {
        public Skill SkillType { get; set; }
    }

    public class PlayerActionRequiredEventArgs : GameEventArgs
    {
        public ActionType RequiredActionType { get; set; }
        public List<int> ValidTargets { get; set; } = [];
        public List<int> ValidNumbers { get; set; } = [];
        public bool IsAttacking { get; set; } = false;
    }

    public class GameEndEventArgs : GameEventArgs
    {
        public int WinnerPlayerId { get; set; }
        public bool IsDraw { get; set; }
    }
}
