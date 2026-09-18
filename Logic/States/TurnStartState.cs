using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 回合开始状态
    /// </summary>
    public class TurnStartState : GameState
    {

        public TurnStartState(GameManager gameManager, int sourcePlayerId, int skillCheckStage = 0) : base(gameManager, sourcePlayerId)
        {
        }

        public override void Enter()
        {
            // 只有在检查第一个技能时才显示回合开始消息
            GameManager.SetGameMessage(GameText.TurnStart(SourcePlayer.Name));
            GameManager.OnTurnStarted(new TurnStartEventArgs
            {
                SourcePlayerId = SourcePlayerId,
                Message = GameManager.GameMessage
            });
            

            foreach (var player in GameManager.Players)
            {
                player.ResetPerTurnSkills();
                player.ResetAttacksReceivedThisTurn();
            }

            // 整条回合主流程倒序入栈。
            GameManager.StateStack.Push(new ChooseNumberIndexState(GameManager, SourcePlayerId));

            foreach (var skill in SourcePlayer.Skills)
            {
                if (skill.Trigger == SkillTrigger.TurnStart && skill.CanUse(GameManager, SourcePlayer))
                {
                    var playerId = SourcePlayer.PlayerId;
                    var state = skill.CreateState(GameManager, new SkillContext(playerId));
                    if (state != null)
                    {
                        GameManager.StateStack.Push(state);
                    }
                }
            }


            // 所有回合开始技能检查完毕，进入选择数字阶段
            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
