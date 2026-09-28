using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 防御技能处理状态 — 逐一迭代目标玩家的防御技能，然后结算伤害和反应技能
    /// </summary>
    public class PreDamageState : GameState
    {
        private readonly SkillContext _ctx;
        private readonly int _attackerId;

        public PreDamageState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
            _attackerId = ctx.PipelineOriginId;

            if (_ctx.SkillEnumerator == null)
            {
                var target = gameManager.Players.FirstOrDefault(p => p.PlayerId == ctx.TargetPlayerId);
                _ctx.SkillEnumerator = target?.GetSkillsByTrigger(SkillTrigger.Defending);
            }
        }

        public override void Enter()
        {
            if (_ctx.SkillEnumerator != null && _ctx.SkillEnumerator.MoveNext())
            {
                var skill = _ctx.SkillEnumerator.Current;
                var target = GameManager.Players.FirstOrDefault(p => p.PlayerId == _ctx.TargetPlayerId);
                if (target != null && skill.CanUse(GameManager, target))
                {
                    var subCtx = _ctx.Derive(target.PlayerId);
                    var state = skill.CreateState(GameManager, subCtx);
                    if (state != null)
                    {
                        GameManager.TransitionTo(state);
                        return;
                    }
                }
                // 不可用则跳过
                GameManager.TransitionTo(new PreDamageState(GameManager, _ctx));
            }
            else
            {
                _ctx.DisposeEnumerator();
                ApplyDamage();
            }
        }

        private void ApplyDamage()
        {
            var targetPlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == _ctx.TargetPlayerId);
            if (targetPlayer == null)
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            if (_ctx.Damage <= 0)
            {
                GameManager.SetGameMessage($"{targetPlayer.Name}'s Stone blocked the additional attack.");
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            targetPlayer.RecordAttackReceived();
            targetPlayer.TakeDamage(_ctx.Damage);
            GameManager.SetGameMessage(GameText.Damaged(targetPlayer.Name, _ctx.Damage));

            // 推入受伤后反应技能（栈模式，直接调用 skill.CreateState 而非 SkillManager）
            var attacker = GameManager.Players.FirstOrDefault(p => p.PlayerId == _attackerId);

            // Damaging 技能（攻击者）
            var attackerStates = attacker == null
                ? new List<GameState>()
                : attacker.CollectTriggeredStates(GameManager, _ctx.Derive(attacker.PlayerId), SkillTrigger.Damaging).ToList();
            GameManager.PushStatesInOrder(attackerStates);

            // Damaged 技能（目标玩家）
            var targetStates = targetPlayer.CollectTriggeredStates(GameManager, _ctx.Derive(targetPlayer.PlayerId), SkillTrigger.Damaged).ToList();
            GameManager.PushStatesInOrder(targetStates);

            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
