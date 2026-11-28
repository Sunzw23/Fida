using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 应用效果状态
    /// </summary>
    public class ApplyingEffectState : GameState
    {
        public ApplyingEffectState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }

        public override void Enter()
        {
            var numbers = SourcePlayer.Numbers;
            var outcome = CombinationTable.Match(numbers, CombinationLoadout.Active);

            if (!outcome.IsNone)
            {
                Resolve(outcome);
                return;
            }

            // 如果没有触发任何效果，则从状态栈中弹出下一个状态
            CombinationLedger.RecordChain(GameManager.ChainDepth);
            GameManager.ChainDepth = 0;
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }

        /// <summary>
        /// Resolves whatever the table matched. A single-digit row consumes one number, a pair row
        /// consumes both; either way the digits reset to 1 and the next check is queued, so a
        /// freshly reset digit can pair with the other one (the "chain").
        /// </summary>
        private void Resolve(CombinationOutcome outcome)
        {
            int[] snapshot = new int[2];
            Array.Copy(SourcePlayer.Numbers, snapshot, 2);

            if (outcome.ConsumesBoth)
            {
                NotifyNumbersConsumed(0, 1);
                SourcePlayer.SetNumber(0, 1);
                SourcePlayer.SetNumber(1, 1);
            }
            else
            {
                NotifyNumbersConsumed(outcome.FirstIndex);
                SourcePlayer.SetNumber(outcome.FirstIndex, 1);
            }

            RecordCombination(outcome, snapshot);
            GameManager.ChainDepth++;
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));

            if (outcome.Kind == CombinationKind.Attack)
            {
                GameManager.SetGameMessage(outcome.ConsumesBoth
                    ? GameText.AttackTriggeredByNumberPair(SourcePlayer.Name, outcome.Amount)
                    : GameText.AttackTriggeredBySingleNumber(SourcePlayer.Name,
                        snapshot[outcome.FirstIndex], outcome.Amount));
                GameManager.TransitionTo(new OrderingAttackState(GameManager,
                    new SkillContext(SourcePlayerId, outcome.Amount, numbers: snapshot)));
            }
            else
            {
                GameManager.SetGameMessage(outcome.ConsumesBoth
                    ? GameText.HealTriggeredByNumberPair(SourcePlayer.Name, outcome.Amount)
                    : GameText.HealTriggeredBySingleNumber(SourcePlayer.Name,
                        snapshot[outcome.FirstIndex], outcome.Amount));
                GameManager.TransitionTo(new TargetSelectionState(GameManager,
                    new SkillContext(SourcePlayerId, 0, outcome.Amount)));
            }
        }

        /// <summary>Feeds the combination ledger (achievements) the rows this resolution used.</summary>
        private void RecordCombination(CombinationOutcome outcome, int[] snapshot)
        {
            if (outcome.ConsumesBoth) CombinationLedger.RecordPair(snapshot[0], snapshot[1]);
            else CombinationLedger.Record(snapshot[outcome.FirstIndex].ToString());
        }

        private void NotifyNumbersConsumed(params int[] indices) =>
            GameManager.OnEffectNumbersConsumed(new EffectNumbersConsumedEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                NumberIndices = indices
            });
    }
}
