using System;
using System.Collections.Generic;

namespace Fida.Logic
{
    // 动作类型枚举
    public enum ActionType
    {
        ChooseNumberIndex,
        ChooseTarget,
        DecideApply,
        ChooseNumber,
        None,
    }

    // 动作结构
    public struct GameAction
    {
        public ActionType Type { get; set; }
        public int TargetPlayerId { get; set; }
        public int NumberIndex { get; set; }
        public int Number { get; set; }
        public bool IsApply { get; set; }

        public GameAction(ActionType type)
        {
            this.Type = type;
        }
    }

    // 游戏事件参数基类
    public class GameEventArgs : EventArgs
    {
        public int SourcePlayerId { get; init; }
        public int TargetPlayerId { get; set; }
        public string Message { get; init; }
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
        public SkillType SkillType { get; set; }
    }

    public class PlayerActionRequiredEventArgs : GameEventArgs
    {
        public ActionType RequiredActionType { get; init; }
        public List<int> ValidTargets { get; init; } = [];
        public List<int> ValidNumbers { get; init; } = [];
        public bool IsAttacking { get; set; } = false;
    }

    public class GameEndEventArgs : GameEventArgs
    {
        public int WinnerPlayerId { get; set; }
        public bool IsDraw { get; set; }
    }

    /// <summary>
    /// 游戏状态变化事件参数
    /// </summary>
    public class GameStateEventArgs : GameEventArgs
    {
        public GameState NewState { get; init; }
    }
}
