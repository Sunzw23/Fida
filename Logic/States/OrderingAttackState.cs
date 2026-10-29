using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 攻击指令状态（逐一检查攻击前技能）
    /// </summary>
    public class OrderingAttackState : GameState
    {
        private readonly SkillContext _ctx;

        public OrderingAttackState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
            if (_ctx.SkillEnumerator == null)
                _ctx.SkillEnumerator = SourcePlayer.GetSkillsByTrigger(SkillTrigger.OrderingAttack);
        }

        public override void Enter()
        {
            // Swiss 分支由 AttackCompletedState 统一按配装顺序结算攻击后技能。
            if (_ctx.SkillEnumerator != null && _ctx.SkillEnumerator.MoveNext())
            {
                var currentSkill = _ctx.SkillEnumerator.Current;
                if (currentSkill.CanUse(GameManager, SourcePlayer))
                {
                    var skillState = currentSkill.CreateState(GameManager, _ctx);
                    if (skillState != null)
                    {
                        GameManager.TransitionTo(skillState);
                        return;
                    }
                }
                // 当前技能不可用或没有对应状态，跳过继续
                GameManager.TransitionTo(new OrderingAttackState(GameManager, _ctx));
            }
            else
            {
                // 所有攻击前技能检查完毕，清理枚举器，进入选择目标阶段
                _ctx.DisposeEnumerator();
                GameManager.TransitionTo(new TargetSelectionState(GameManager, _ctx));
            }
        }
    }
}
