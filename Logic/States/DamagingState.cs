using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 造成伤害状态
    /// </summary>
    public class DamagingState : GameState
    {
        private readonly SkillContext _ctx;

        public DamagingState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        public override void Enter()
        {
            var targetPlayer = GameManager.Players.FirstOrDefault(p => p.PlayerId == _ctx.TargetPlayerId);
            if (targetPlayer == null)
            {
                GameManager.TransitionTo(new HandleStackState(GameManager));
                return;
            }

            var (hasShield, shieldIndex) = targetPlayer.HasShield();
            if (hasShield)
            {
                targetPlayer.SetNumber(shieldIndex, 1); // 护盾数字变为1
                GameManager.PushState(new ApplyingEffectState(GameManager, _ctx.TargetPlayerId));
                GameManager.SetGameMessage(GameText.ShieldBreak(targetPlayer.Name));
                // 护盾破碎会导致数字改变，进入效果应用阶段，但源玩家是目标玩家
                GameManager.TransitionTo(new HandleStackState(GameManager));
            }
            else
            {
                // 进入防御技能处理流水线
                GameManager.TransitionTo(new PreDamageState(GameManager, _ctx));
            }
        }
    }
}
