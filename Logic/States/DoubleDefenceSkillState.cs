using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{

    public class DoubleDefenceSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public DoubleDefenceSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleDefence));
            int newDamage = _ctx.Damage / 2;
            if (newDamage == 0) newDamage = 1; // 最少造成1点伤害
            _ctx.Damage = newDamage;
            _ctx.SourcePlayerId = _ctx.PipelineOriginId;
            GameManager.TransitionTo(new PreDamageState(GameManager, _ctx));
        }
    }
}
