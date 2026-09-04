using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{

    /// <summary>
    /// 回合结束状态
    /// </summary>
    public class PlayerTurnEndState : GameState
    {
        public PlayerTurnEndState(GameManager gameManager, int sourcePlayerId) : base(gameManager, sourcePlayerId) { }

        public override void Enter()
        {
            GameManager.EndCurrentTurn();
            var nextPlayer = GameManager.GetCurrentPlayer();
            GameManager.TransitionTo(new TurnStartState(GameManager, nextPlayer.PlayerId));
        }
    }
}
