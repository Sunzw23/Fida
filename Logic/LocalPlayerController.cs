namespace Fida.Logic
{
    /// <summary>
    /// 本地人类玩家控制器。
    /// RequestAction 不做任何事，因为 UI 层通过 EnabledChecker 被动响应。
    /// 职责仅仅是"标记该玩家是本地可操作的"。
    /// </summary>
    public class LocalPlayerController : IPlayerController
    {
        public int PlayerId { get; }

        public LocalPlayerController(int playerId)
        {
            PlayerId = playerId;
        }

        public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args)
        {
            // 本地玩家：UI 层通过 EnabledChecker 被动响应，无需主动操作
        }
    }
}