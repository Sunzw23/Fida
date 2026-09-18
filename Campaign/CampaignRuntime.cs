namespace Fida.Campaign
{
    /// <summary>当前进程的战役门面，不包含 UI 或战斗实现。</summary>
    public static class CampaignRuntime
    {
        private static CampaignProgressStore _store = new(CampaignProgressStore.DefaultPath());
        public static CampaignProgress Progress { get; private set; } = _store.Load();

        public static void Complete(CampaignNode node)
        {
            Progress.Complete(node);
            _store.Save(Progress);
        }

        public static void UseStore(CampaignProgressStore store)
        {
            _store = store;
            Progress = _store.Load();
        }

        public static void Reset(bool save = true)
        {
            Progress = new CampaignProgress();
            if (save) _store.Save(Progress);
        }
    }
}
