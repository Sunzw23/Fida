using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 治疗状态
    /// </summary>
    public class HealingState : GameState
    {
        private readonly SkillContext _ctx;

        public HealingState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
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

            targetPlayer.Heal(_ctx.Healing);
            GameManager.SetGameMessage(GameText.Healed(targetPlayer.Name, _ctx.Healing));

            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
