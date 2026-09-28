using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 游戏开始状态
    /// </summary>
    public class GameStartState(GameManager gameManager) : GameState(gameManager)
    {
        public override void Enter()
        {
            GameManager.SetGameMessage(GameText.GameStart);
            GameManager.OnGameStarted(new GameStartEventArgs { Message = GameManager.GameMessage });

            // 游戏开始后，直接进入第一个玩家的回合开始阶段
            if (GameManager.Players.Count == 0 || GameManager.CurrentPlayerIndex == -1)
            {
                return;
            }

            var firstPlayer = GameManager.Players[GameManager.CurrentPlayerIndex];
            GameManager.StateStack.Push(new TurnStartState(GameManager, firstPlayer.PlayerId));

            var triggeredStates = new List<GameState>();
            foreach (var player in GameManager.Players)
            {
                foreach (var skill in player.Skills)
                {
                    if (skill.Trigger == SkillTrigger.GameStart && skill.CanUse(GameManager, player))
                    {
                        var playerId = player.PlayerId;
                        var state = skill.CreateState(GameManager, new SkillContext(playerId));
                        if (state != null)
                        {
                            triggeredStates.Add(state);
                        }
                    }
                }
            }

            GameManager.PushStatesInOrder(triggeredStates);


            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
