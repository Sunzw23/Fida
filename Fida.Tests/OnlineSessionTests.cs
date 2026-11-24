using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Fida.Network;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// 覆盖"离开战斗但不拆会话"的契约：
/// 1. 暂停菜单离开战斗会发 <see cref="MessageType.BattleForfeit"/>，双方连接保持。
/// 2. 通用的会话帧处理不会吞掉战斗帧（战役 presence 仍在队列中等待战斗界面处理）。
/// </summary>
public class OnlineSessionTests
{
    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.IPv6Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private static (NetworkManager Host, NetworkManager Guest) Connect()
    {
        var host = new NetworkManager();
        var guest = new NetworkManager();
        int port = FreePort();
        host.StartHosting("::1", port);
        guest.StartConnecting("::1", port);
        Assert.True(SpinWait.SpinUntil(() => host.IsConnected && guest.IsConnected, 5000),
            "host and guest should connect over the loopback address");
        return (host, guest);
    }

    private static void Close(NetworkManager host, NetworkManager guest)
    {
        host.Cleanup();
        guest.Cleanup();
        host.Dispose();
        guest.Dispose();
    }

    [Fact]
    public void BattleForfeit_IsDeliveredWithoutClosingTheSession()
    {
        var (host, guest) = Connect();
        try
        {
            int conceded = -1;
            guest.BattleForfeitReceived += playerId => conceded = playerId;

            host.SendBattleForfeit(host.GetLocalPlayerId());

            Assert.True(SpinWait.SpinUntil(() => { guest.ProcessSessionMessages(); return conceded == 0; }, 5000),
                "the guest should receive the concession");
            Assert.Equal(0, conceded);

            // The conceding player keeps its session: both ends still report a live connection.
            Assert.True(host.IsConnected, "conceding must not close the host's connection");
            Assert.True(guest.IsConnected, "conceding must not close the guest's connection");
        }
        finally { Close(host, guest); }
    }

    [Fact]
    public void ProcessSessionMessages_LeavesBattleFramesQueuedForTheBattleScreen()
    {
        var (host, guest) = Connect();
        try
        {
            int conceded = -1;
            guest.BattleForfeitReceived += playerId => conceded = playerId;

            // Presence is sent first, so it is already queued when the concession arrives.
            host.SendCampaignPresence(new CampaignPresence(0, CampaignOnlinePhase.Battle, "A1", false));
            host.SendBattleForfeit(0);

            Assert.True(SpinWait.SpinUntil(() => { guest.ProcessSessionMessages(); return conceded == 0; }, 5000),
                "the guest should receive the concession");
            Assert.Equal(string.Empty, guest.RemoteCampaignPresence.NodeId);

            guest.ProcessCampaignMessages();
            Assert.Equal("A1", guest.RemoteCampaignPresence.NodeId);
        }
        finally { Close(host, guest); }
    }
}
