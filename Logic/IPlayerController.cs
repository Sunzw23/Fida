namespace Fida.Logic
{
    /// <summary>
    /// 玩家控制器接口 — 统一处理不同模式（本地/AI/远程）玩家的操作请求。
    /// 让 GameManager 和 PlayingSceneManager 不再关心"谁"在操作，
    /// 只负责通知"当前该操作的实体"去执行逻辑。
    /// </summary>
    public interface IPlayerController
    {
        int PlayerId { get; }

        /// <summary>
        /// 当游戏需要该玩家操作时调用
        /// </summary>
        void RequestAction(GameManager game, PlayerActionRequiredEventArgs args);
    }
}