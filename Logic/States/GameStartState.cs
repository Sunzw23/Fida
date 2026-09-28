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
                triggeredStates.AddRange(player.CollectTriggeredStates(GameManager, new SkillContext(player.PlayerId), SkillTrigger.GameStart));
            }

            GameManager.PushStatesInOrder(triggeredStates);


            GameManager.TransitionTo(new HandleStackState(GameManager));
        }
    }
}
