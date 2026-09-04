using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{

    public class PlusDefenceSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public PlusDefenceSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleDefence));
            int newDamage = _ctx.Damage - 1;
            if (newDamage == 0) newDamage = 1; // 最少造成1点伤害
            _ctx.Damage = newDamage;
            _ctx.SourcePlayerId = _ctx.PipelineOriginId;
            GameManager.TransitionTo(new PreDamageState(GameManager, _ctx));
        }
    }
}
