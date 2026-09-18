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

    /// <summary>
    /// Describes the visible direction of a number operation: the addend flows
    /// from one card into the source player's selected base number.
    /// </summary>
    public sealed class NumberOperationEventArgs : GameEventArgs
    {
        public int BaseNumberIndex { get; init; }
        public int BaseValue { get; init; }
        public int AddendPlayerId { get; init; }
        public int AddendNumberIndex { get; init; }
        public int AddendValue { get; init; }
        public int ResultValue { get; init; }
    }

    /// <summary>
    /// Raised immediately before an effect consumes its triggering number(s).
    /// Presentation may keep the old values visible until the card action finishes.
    /// </summary>
    public sealed class EffectNumbersConsumedEventArgs : GameEventArgs
    {
        public IReadOnlyList<int> NumberIndices { get; init; } = [];
    }

    /// <summary>Raised once an attack or heal target has been validated.</summary>
    public sealed class CardActionEventArgs : GameEventArgs
    {
        public int Damage { get; init; }
        public int Healing { get; init; }
        public bool IsDamaging => Damage > 0;
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
