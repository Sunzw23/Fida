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

            // 攻击 (数字变为 8 或 7)
            if (numbers[0] == 8 || numbers[0] == 7)
            {
                HandleAttackEffect(0, 1);
                return;
            }
            if (numbers[1] == 8 || numbers[1] == 7)
            {
                HandleAttackEffect(1, 1);
                return;
            }
            // 攻击 (数字变为 1 和 6)
            if ((numbers[0] == 1 && numbers[1] == 6) || (numbers[0] == 6 && numbers[1] == 1))
            {
                HandleAttackEffectBoth(2);
                return;
            }
            // 攻击 (数字变为 5 和 5)
            if (numbers[0] == 5 && numbers[1] == 5)
            {
                HandleAttackEffectBoth(3);
                return;
            }
            // 攻击 (数字变为 6 和 6)
            if (numbers[0] == 6 && numbers[1] == 6)
            {
                HandleAttackEffectBoth(5);
                return;
            }
            // 治愈 (数字变为 9)
            if (numbers[0] == 9)
            {
                HandleHealEffect(0, 1);
                return;
            }
            if (numbers[1] == 9)
            {
                HandleHealEffect(1, 1);
                return;
            }
            // 治愈 (两个数字都变为 0)
            if (numbers[0] == 0 && numbers[1] == 0)
            {
                HandleHealEffectBoth(10);
                return;
            }

            // 如果没有触发任何效果，则从状态栈中弹出下一个状态
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }

        private void HandleAttackEffect(int numberIndex, int damage)
        {
            GameManager.SetGameMessage(GameText.AttackTriggeredBySingleNumber(SourcePlayer.Name, SourcePlayer.Numbers[numberIndex], damage));
            int[] numbers = new int[2];
            Array.Copy(SourcePlayer.Numbers, numbers, 2);
            SourcePlayer.SetNumber(numberIndex, 1);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            var ctx = new SkillContext(SourcePlayerId, damage, numbers: numbers);
            GameManager.TransitionTo(new OrderingAttackState(GameManager, ctx));
        }

        private void HandleAttackEffectBoth(int damage)
        {
            GameManager.SetGameMessage(GameText.AttackTriggeredByNumberPair(SourcePlayer.Name, damage));
            int[] numbers = new int[2];
            Array.Copy(SourcePlayer.Numbers, numbers, 2);
            SourcePlayer.SetNumber(0, 1);
            SourcePlayer.SetNumber(1, 1);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            var ctx = new SkillContext(SourcePlayerId, damage, numbers: numbers);
            GameManager.TransitionTo(new OrderingAttackState(GameManager, ctx));
        }

        private void HandleHealEffect(int numberIndex, int healing)
        {
            GameManager.SetGameMessage(GameText.HealTriggeredBySingleNumber(SourcePlayer.Name, SourcePlayer.Numbers[numberIndex], healing));
            SourcePlayer.SetNumber(numberIndex, 1);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            var ctx = new SkillContext(SourcePlayerId, 0, healing);
            GameManager.TransitionTo(new TargetSelectionState(GameManager, ctx));
        }

        private void HandleHealEffectBoth(int healing)
        {
            GameManager.SetGameMessage(GameText.HealTriggeredByNumberPair(SourcePlayer.Name, healing));
            SourcePlayer.SetNumber(0, 1);
            SourcePlayer.SetNumber(1, 1);
            GameManager.PushState(new ApplyingEffectState(GameManager, SourcePlayerId));
            var ctx = new SkillContext(SourcePlayerId, 0, healing);
            GameManager.TransitionTo(new TargetSelectionState(GameManager, ctx));
        }
    }
}
