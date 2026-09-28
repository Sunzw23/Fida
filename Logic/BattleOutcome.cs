namespace Fida.Logic
{
    /// <summary>
    /// The outcome of a finished battle, produced by the session manager and consumed by the
    /// result screen. A null value means no battle has ended yet (sentinel).
    /// </summary>
    public sealed class BattleOutcome
    {
        public BattleOutcome(bool won, string message, bool wasOnlineDuel)
        {
            Won = won;
            Message = message;
            WasOnlineDuel = wasOnlineDuel;
        }

        public bool Won { get; }
        public string Message { get; }
        public bool WasOnlineDuel { get; }
    }
}
