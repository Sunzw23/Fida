using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    public class ActionStep
    {
        public ActionType RequiredType { get; init; }
        public List<int> ValidTargets { get; init; } = [];
        public List<int> ValidNumbers { get; init; } = [];
        public string Prompt { get; init; } = string.Empty;
        public GameAction? Result { get; set; }
    }

    /// <summary>
    /// 抽象游戏状态基类
    /// </summary>
    public abstract class GameState
    {
        protected readonly GameManager GameManager;
        protected readonly int SourcePlayerId;
        protected readonly Player SourcePlayer;
        protected readonly List<ActionStep> Steps = [];
        protected int StepIndex;

        protected GameState(GameManager gameManager, int sourcePlayerId = -1)
        {
            GameManager = gameManager;
            SourcePlayerId = sourcePlayerId;
            if (sourcePlayerId != -1)
            {
                SourcePlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);
            }
        }

        /// <summary>
        /// 进入状态时执行的逻辑
        /// </summary>
        public virtual void Enter()
        {
            Steps.Clear();
            StepIndex = 0;
            BuildSteps();

            if (Steps.Count == 0)
            {
                Execute([]);
                return;
            }

            BroadcastCurrentStep();
        }

        /// <summary>
        /// 处理当前状态下的逻辑和玩家输入
        /// </summary>
        public virtual void Handle(GameAction action)
        {
            if (StepIndex >= Steps.Count) return;

            var step = Steps[StepIndex];
            if (action.Type != step.RequiredType) return;
            if (step.ValidTargets.Count > 0 && !step.ValidTargets.Contains(action.TargetPlayerId)) return;
            if (step.ValidNumbers.Count > 0 && !step.ValidNumbers.Contains(action.Number)) return;
            if (!ValidateStep(step, action)) return;

            step.Result = action;
            StepIndex++;

            if (StepIndex >= Steps.Count)
            {
                Execute(Steps.Select(s => s.Result ?? default).ToList());
                return;
            }

            BroadcastCurrentStep();
        }

        protected virtual void BuildSteps() { }

        protected virtual bool ValidateStep(ActionStep step, GameAction action) => true;

        protected virtual void Execute(List<GameAction> actions) { }

        protected void BroadcastCurrentStep()
        {
            if (StepIndex >= Steps.Count) return;

            var step = Steps[StepIndex];
            // 先更新消息，再触发事件
            // 确保 OnPlayerActionRequired 事件中传递的是最新的消息
            GameManager.SetGameMessage(step.Prompt);
            GameManager.OnPlayerActionRequired(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                RequiredActionType = step.RequiredType,
                ValidTargets = step.ValidTargets,
                ValidNumbers = step.ValidNumbers,
                Message = GameManager.GameMessage
            });
        }
    }
}
