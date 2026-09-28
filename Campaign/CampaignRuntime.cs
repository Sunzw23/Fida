namespace Fida.Campaign
{
    /// <summary>当前进程的战役门面，不包含 UI 或战斗实现。</summary>
    public static class CampaignRuntime
    {
        private static CampaignProgressStore _store = new(CampaignProgressStore.DefaultPath());
        private static CampaignProgressStore _onlineStore = new(CampaignProgressStore.OnlinePath());
        public static CampaignProgress Progress { get; private set; } = _store.Load();
        public static CampaignProgress OnlineProgress { get; private set; } = _onlineStore.Load();

        public static CampaignProgress GetProgress(bool online) => online ? OnlineProgress : Progress;

        public static void Complete(CampaignNode node, bool online = false)
        {
            if (online)
            {
                OnlineProgress.Complete(node);
                _onlineStore.Save(OnlineProgress);
            }
            else
            {
                Progress.Complete(node);
                _store.Save(Progress);
            }
        }

        /// <summary>Merges peer clears into the online-only save; returns true if it changed.</summary>
        public static bool MergeOnlineProgress(System.Collections.Generic.IEnumerable<string> completedNodeIds)
        {
            bool changed = false;
            foreach (string id in completedNodeIds ?? [])
            {
                var node = CampaignCatalog.GetNode(id);
                if (node == null || OnlineProgress.IsCompleted(node.Id)) continue;
                OnlineProgress.Complete(node);
                changed = true;
            }

            if (changed) _onlineStore.Save(OnlineProgress);
            return changed;
        }

        public static void UseStore(CampaignProgressStore store)
        {
            _store = store;
            Progress = _store.Load();
        }

        public static void UseOnlineStore(CampaignProgressStore store)
        {
            _onlineStore = store;
            OnlineProgress = _onlineStore.Load();
        }

        public static void Reset(bool save = true)
        {
            Progress = new CampaignProgress();
            if (save) _store.Save(Progress);
        }

        public static void ResetOnline(bool save = true)
        {
            OnlineProgress = new CampaignProgress();
            if (save) _onlineStore.Save(OnlineProgress);
        }
    }
}
