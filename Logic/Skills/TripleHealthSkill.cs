using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    public class TripleHealthSkill : Skill
    {
        public TripleHealthSkill()
        {
            Type = SkillType.TripleHealth;
            Value = 1;
            Trigger = SkillTrigger.GameStart;
            Usage = new SkillUsageState(maxUses: 1);
        }

        public override GameState CreateState(GameManager gameManager, SkillContext context)
        {
            return new TripleHealthSkillState(gameManager, context);
        }
    }

    public class TripleHealthSkillState : GameState
    {
        public TripleHealthSkillState(GameManager gameManager, SkillContext ctx) : base(gameManager, ctx.SourcePlayerId) { }

        protected override void Execute(List<GameAction> actions)
        {
            GameManager.SetGameMessage(GameText.SkillActivated(SourcePlayer.Name, SkillType.TripleHealth));
            SourcePlayer.MaxHealth = SourcePlayer.MaxHealth * 3;
            SourcePlayer.Health = SourcePlayer.MaxHealth;
            GameManager.OnSkillActivated(new SkillActivatedEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                SkillType = SkillType.TripleHealth,
                Message = GameManager.GameMessage
            });
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
