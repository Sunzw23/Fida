using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 选择数字状态
    /// </summary>
    public class ChooseNumberIndexState : GameState
    {
        private readonly int _initialStage;
        private readonly int _initialFirstNumberIndex;
        private readonly bool _changeBothNumbers;

        public ChooseNumberIndexState(GameManager gameManager, int sourcePlayerId, int stage = 0, int firstNumberIndex = -1, bool changeBothNumbers = false) : base(gameManager, sourcePlayerId)
        {
            _initialStage = stage;
            _initialFirstNumberIndex = firstNumberIndex;
            _changeBothNumbers = changeBothNumbers;
        }

        public int Stage => StepIndex;

        /// <summary>
        /// 获取第一步已选择的数字索引，如果第一步尚未完成则返回 -1
        /// </summary>
        public int FirstStepNumberIndex => Steps.Count > 0 && Steps[0].Result is { } firstResult ? firstResult.NumberIndex : -1;

        protected override void BuildSteps()
        {
            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = [SourcePlayerId],
                Prompt = GameText.ChooseFirstNumberIndex(SourcePlayer.Name)
            });

            Steps.Add(new ActionStep
            {
                RequiredType = ActionType.ChooseNumberIndex,
                ValidTargets = GameManager.Players.Where(p => p.IsAlive()).Select(p => p.PlayerId).ToList(),
                Prompt = GameText.ChooseSecondNumberIndex(SourcePlayer.Name)
            });

            StepIndex = _initialStage;
            if (StepIndex > 0 && _initialFirstNumberIndex >= 0)
            {
                Steps[0].Result = new GameAction
                {
                    Type = ActionType.ChooseNumberIndex,
                    TargetPlayerId = SourcePlayerId,
                    NumberIndex = _initialFirstNumberIndex
                };
            }
        }

        protected override bool ValidateStep(ActionStep step, GameAction action)
        {
            if (StepIndex != 1 || Steps[0].Result is not { } firstStepAction)
            {
                return true;
            }

            if (action.TargetPlayerId == SourcePlayerId && action.NumberIndex == firstStepAction.NumberIndex)
            {
                // 不能选择同一个数字，重置回第一步。
                Steps[0].Result = null;
                StepIndex = 0;
                BroadcastCurrentStep();
                return false;
            }

            return true;
        }

        protected override void Execute(List<GameAction> actions)
        {
            if (actions.Count < 2) return;

            var firstAction = actions[0];
            var secondAction = actions[1];
            var otherPlayerSelected = GameManager.Players.FirstOrDefault(p => p.PlayerId == secondAction.TargetPlayerId);
            if (otherPlayerSelected == null || !otherPlayerSelected.IsAlive())
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            var firstNumberIndex = firstAction.NumberIndex;
            int baseValue = SourcePlayer.Numbers[firstNumberIndex];
            int addendValue = otherPlayerSelected.Numbers[secondAction.NumberIndex];
            var newCalculatedNumber = (baseValue + addendValue) % 10;

            // Broadcast the operands before mutating either card. Presentation code
            // uses only player/number indices to draw a directional golden flow.
            GameManager.OnNumberOperationResolved(new NumberOperationEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                BaseNumberIndex = firstNumberIndex,
                BaseValue = baseValue,
                AddendPlayerId = otherPlayerSelected.PlayerId,
                AddendNumberIndex = secondAction.NumberIndex,
                AddendValue = addendValue,
                ResultValue = newCalculatedNumber
            });


            if (_changeBothNumbers)
            {
                GameManager.SetGameMessage(GameText.NumberChangedBothCheckEffect(SourcePlayer.Name, newCalculatedNumber));
                SourcePlayer.SetNumber(0, newCalculatedNumber);
                SourcePlayer.SetNumber(1, newCalculatedNumber);
            }
            else
            {
                GameManager.SetGameMessage(GameText.NumberChangedSingleCheckEffect(SourcePlayer.Name, SourcePlayer.Numbers[firstNumberIndex], newCalculatedNumber));
                SourcePlayer.SetNumber(firstNumberIndex, newCalculatedNumber);
            }
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            

            // 回合主流程已预先入栈，交还给栈调度。
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
