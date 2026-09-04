using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{

    public class DoubleHealthSkillState : GameState
    {
        public DoubleHealthSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.DoubleHealth));
            SourcePlayer.MaxHealth = SourcePlayer.MaxHealth * 2;
            SourcePlayer.Health = SourcePlayer.MaxHealth;
            GameManager.OnSkillActivated(new SkillActivatedEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                SkillType = SkillType.DoubleHealth,
                Message = GameManager.GameMessage
            });
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
