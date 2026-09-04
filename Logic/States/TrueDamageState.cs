using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 真实伤害状态
    /// </summary>
    public class TrueDamageState : GameState
    {
        private readonly SkillContext _ctx;

        public TrueDamageState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
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

            targetPlayer.TakeDamage(_ctx.Damage);
            GameManager.SetGameMessage(GameText.TrueDamaged(targetPlayer.Name, _ctx.Damage));

            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
