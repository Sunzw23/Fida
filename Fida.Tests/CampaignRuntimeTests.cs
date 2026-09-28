using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public sealed class CampaignRuntimeTests
{
    [Fact]
    public void OfflineAndOnlineCampaignProgress_AreStoredSeparatelyAndOnlineClearsMerge()
    {
        string offlinePath = Path.Combine(Path.GetTempPath(), $"fida_offline_{Guid.NewGuid():N}.json");
        string onlinePath = Path.Combine(Path.GetTempPath(), $"fida_online_{Guid.NewGuid():N}.json");
        try
        {
            CampaignRuntime.UseStore(new CampaignProgressStore(offlinePath));
            CampaignRuntime.UseOnlineStore(new CampaignProgressStore(onlinePath));
            CampaignRuntime.Reset(save: false);
            CampaignRuntime.ResetOnline(save: false);

            CampaignRuntime.Complete(CampaignCatalog.GetNode("P"));
            Assert.True(CampaignRuntime.Progress.IsCompleted("P"));
            Assert.False(CampaignRuntime.OnlineProgress.IsCompleted("P"));

            CampaignRuntime.Complete(CampaignCatalog.GetNode("A1"), online: true);
            Assert.True(CampaignRuntime.OnlineProgress.IsCompleted("A1"));
            Assert.False(CampaignRuntime.Progress.IsCompleted("A1"));

            Assert.True(CampaignRuntime.MergeOnlineProgress(["A2", "UNKNOWN"]));
            Assert.True(CampaignRuntime.OnlineProgress.IsCompleted("A2"));
            Assert.False(CampaignRuntime.MergeOnlineProgress(["A1", "A2"]));

            Assert.True(File.Exists(offlinePath));
            Assert.True(File.Exists(onlinePath));
        }
        finally
        {
            CampaignRuntime.UseStore(new CampaignProgressStore(CampaignProgressStore.DefaultPath()));
            CampaignRuntime.UseOnlineStore(new CampaignProgressStore(CampaignProgressStore.OnlinePath()));
            if (File.Exists(offlinePath)) File.Delete(offlinePath);
            if (File.Exists(onlinePath)) File.Delete(onlinePath);
        }
    }
}
