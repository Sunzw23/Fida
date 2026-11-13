using Fida.Campaign;
using Fida.Logic;
using Xunit;

namespace Fida.Tests;

public sealed class CampaignRuntimeTests
{
    private static CampaignNode Node(string id) => Assert.IsType<CampaignNode>(CampaignCatalog.GetNode(id));
    [Fact]
    public void OfflineAndOnlineCampaignProgress_AreStoredSeparatelyAndOnlineClearsMerge()
    {
        string offlinePath = Path.Combine(Path.GetTempPath(), $"fida_offline_{Guid.NewGuid():N}.json");
        string onlinePath = Path.Combine(Path.GetTempPath(), $"fida_online_{Guid.NewGuid():N}.json");
        try
        {
            CampaignRuntime.UseStore(CampaignRuntime.Local, new CampaignProgressStore(offlinePath));
            CampaignRuntime.UseStore(CampaignRuntime.Online, new CampaignProgressStore(onlinePath));
            CampaignRuntime.Reset(CampaignRuntime.Local, save: false);
            CampaignRuntime.Reset(CampaignRuntime.Online, save: false);

            CampaignRuntime.Complete(Node("P"), CampaignRuntime.Local);
            Assert.True(CampaignRuntime.Progress.IsCompleted("P"));
            Assert.False(CampaignRuntime.OnlineProgress.IsCompleted("P"));

            CampaignRuntime.Complete(Node("A1"), CampaignRuntime.Online);
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
            CampaignRuntime.UseStore(CampaignRuntime.Local, new CampaignProgressStore(CampaignProgressStore.DefaultPath()));
            CampaignRuntime.UseStore(CampaignRuntime.Online, new CampaignProgressStore(CampaignProgressStore.OnlinePath()));
            if (File.Exists(offlinePath)) File.Delete(offlinePath);
            if (File.Exists(onlinePath)) File.Delete(onlinePath);
        }
    }
}
