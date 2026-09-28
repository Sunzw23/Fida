using System.Collections.Generic;

namespace Fida.Campaign
{
    /// <summary>Wraps a (store, progress) pair for one campaign lane (local or online).</summary>
    public sealed class CampaignProgressSlot
    {
        private CampaignProgressStore _store;
        public CampaignProgress Progress { get; private set; }

        public CampaignProgressSlot(CampaignProgressStore store)
        {
            _store = store;
            Progress = _store.Load();
        }

        public void Complete(CampaignNode node)
        {
            Progress.Complete(node);
            _store.Save(Progress);
        }

        public void Reset(bool save = true)
        {
            Progress = new CampaignProgress();
            if (save) _store.Save(Progress);
        }

        public void UseStore(CampaignProgressStore store)
        {
            _store = store;
            Progress = _store.Load();
        }

        public void Save() => _store.Save(Progress);
    }

    /// <summary>当前进程的战役门面，不包含 UI 或战斗实现。进度按 local/online 槽位隔离。</summary>
    public static class CampaignRuntime
    {
        public static CampaignProgressSlot Local { get; } = new(new CampaignProgressStore(CampaignProgressStore.DefaultPath()));
        public static CampaignProgressSlot Online { get; } = new(new CampaignProgressStore(CampaignProgressStore.OnlinePath()));

        public static CampaignProgress Progress => Local.Progress;
        public static CampaignProgress OnlineProgress => Online.Progress;

        public static CampaignProgress GetProgress(bool online) => online ? OnlineProgress : Progress;

        public static void Complete(CampaignNode node, CampaignProgressSlot slot) => slot.Complete(node);

        public static void Reset(CampaignProgressSlot slot, bool save = true) => slot.Reset(save);

        public static void UseStore(CampaignProgressSlot slot, CampaignProgressStore store) => slot.UseStore(store);

        /// <summary>Merges peer clears into the online-only save; returns true if it changed.</summary>
        public static bool MergeOnlineProgress(IEnumerable<string> completedNodeIds)
        {
            bool changed = false;
            foreach (string id in completedNodeIds ?? [])
            {
                var node = CampaignCatalog.GetNode(id);
                if (node == null || OnlineProgress.IsCompleted(node.Id)) continue;
                OnlineProgress.Complete(node);
                changed = true;
            }

            if (changed) Online.Save();
            return changed;
        }
    }
}
