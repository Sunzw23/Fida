using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 处理状态栈的状态
    /// </summary>
    public class HandleStackState(GameManager gameManager) : GameState(gameManager)
    {
        public override void Enter()
        {
            if (GameManager.StateStack.Count > 0)
            {
                var nextState = GameManager.StateStack.Pop();
                GameManager.TransitionTo(nextState);
            }
            else
            {
                // 如果栈为空，通常意味着一个操作序列完成，进入回合结束
                GameManager.TransitionTo(new PlayerTurnEndState(GameManager, GameManager.GetCurrentPlayer().PlayerId));
            }
        }
    }
}
