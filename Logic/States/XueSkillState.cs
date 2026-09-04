using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    // 血
    public class XueSkillState : GameState
    {
        private readonly SkillContext _ctx;
        public XueSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId)
        {
            _ctx = ctx;
        }

        protected override void Execute(List<GameAction> actions)
        {
            // 血技能直接触发治疗，无需玩家输入
            GameManager.TransitionTo(new HealingState(GameManager,
                new SkillContext(SourcePlayerId) { TargetPlayerId = SourcePlayerId, Healing = _ctx.Damage }));
        }
    }
}
