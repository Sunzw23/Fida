namespace Fida.Logic
{
    /// <summary>
    /// 远程网络玩家控制器。
    /// RequestAction 不做任何事，因为网络消息会通过 NetworkManager.ProcessMessages() 直接调用 game.UpdateGame(action)。
    /// 职责仅仅是"标记该玩家是远程的，等待网络消息"。
    /// </summary>
    public class RemotePlayerController : IPlayerController
    {
        public int PlayerId { get; }

        public RemotePlayerController(int playerId)
        {
            PlayerId = playerId;
        }

        public void RequestAction(GameManager game, PlayerActionRequiredEventArgs args)
        {
            // 远程玩家：等待网络消息到达后由 NetworkManager.ProcessMessages 直接调用 UpdateGame
        }
    }
}