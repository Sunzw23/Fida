using System;

namespace Fida.Logic
{
    /// <summary>
    /// GameQuery 类负责封装对 GameManager 状态信息的只读查询。
    /// 它将原先散落在 GameManager 中的"状态信息"查询方法集中到一个独立对象中，
    /// 让 GameManager 更专注于状态转换与事件，而查询逻辑则通过 gm.Query.Xxx 访问。
    /// </summary>
    public class GameQuery
    {
        private readonly GameManager _gameManager;

        public GameQuery(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        /// <summary>
        /// 当前回合玩家的 ID。若尚无当前玩家则返回 -1。
        /// </summary>
        public int CurrentPlayerId => _gameManager.Players.Count > 0 && _gameManager.CurrentPlayerIndex >= 0 && _gameManager.CurrentPlayerIndex < _gameManager.Players.Count ? _gameManager.Players[_gameManager.CurrentPlayerIndex].PlayerId : -1;

        /// <summary>
        /// 是否处于选择数字索引状态的第一阶段（Stage 0）
        /// </summary>
        public bool IsChooseFirstNumberIndexState()
        {
            if (_gameManager.CurrentState is ChooseNumberIndexState chooseState && chooseState.Stage == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取第一步已选择的数字索引（仅在 ChooseNumberIndexState 的第二阶段有效）
        /// 返回 -1 表示不在第二阶段或第一步尚未完成
        /// </summary>
        public int GetFirstChosenNumberIndex()
        {
            if (_gameManager.CurrentState is ChooseNumberIndexState chooseState && chooseState.Stage == 1)
            {
                return chooseState.FirstStepNumberIndex;
            }
            return -1;
        }

        /// <summary>
        /// 当前是否处于"伤害/扣血"目标选择（DamageValue > 0 或 HealingValue < 0）
        /// </summary>
        public bool IsDamaging()
        {
            if (_gameManager.CurrentState is TargetSelectionState targetSelectionState && (targetSelectionState.DamageValue > 0 || targetSelectionState.HealingValue < 0))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 当前是否处于"治疗"目标选择（HealingValue > 0）
        /// </summary>
        public bool IsHealing()
        {
            if (_gameManager.CurrentState is TargetSelectionState targetSelectionState && targetSelectionState.HealingValue > 0)
            {
                return true;
            }
            return false;
        }
    }
}
