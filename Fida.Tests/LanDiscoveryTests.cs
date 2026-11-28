using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Fida.Network;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// Serverless discovery: the beacon a host broadcasts and the browser a guest listens with.
/// Covers the wire format, the loopback round trip, and the "host with no relay" manager path.
/// </summary>
public class LanDiscoveryTests
{
    private static int FreeUdpPort()
    {
        using var probe = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)probe.Client.LocalEndPoint!).Port;
    }

    private static int FreeTcpPort()
    {
        var probe = new TcpListener(IPAddress.IPv6Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    [Fact]
    public void Packet_RoundTripsTheGamePort()
    {
        byte[] payload = LanDiscoveryPacket.Build(8888);

        Assert.True(LanDiscoveryPacket.TryParse(payload, payload.Length, out int port));
        Assert.Equal(8888, port);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nope")]
    [InlineData("FIDA1abc")]
    [InlineData("FIDA1|")]
    [InlineData("FIDA1|0")]
    [InlineData("FIDA1|70000")]
    [InlineData("FIDA2|8888")]
    [InlineData("FIDA1|8888|extra")]
    public void Packet_RejectsMalformedPayloads(string text)
    {
        byte[] payload = System.Text.Encoding.ASCII.GetBytes(text);

        Assert.False(LanDiscoveryPacket.TryParse(payload, payload.Length, out _));
    }

    [Fact]
    public void Browser_HearsABeaconAndReportsTheSender()
    {
        int port = FreeUdpPort();
        using var browser = new LanBrowser(port);

        LanBeacon.SendTo(new IPEndPoint(IPAddress.Loopback, port), 6001);

        LanHost host = default;
        Assert.True(SpinWait.SpinUntil(() => browser.TryTakeHost(out host), 3000),
            "the browser should receive the beacon it was sent");
        Assert.Equal("127.0.0.1", host.Address);
        Assert.Equal(6001, host.Port);
    }

    [Fact]
    public void HostingWithoutARelay_AcceptsALoopbackGuest()
    {
        var host = new NetworkManager();
        var guest = new NetworkManager();
        int port = FreeTcpPort();
        try
        {
            Assert.True(host.StartHostingDirect(port), "direct hosting should bind without a relay");
            guest.StartConnecting("::1", port);

            Assert.True(SpinWait.SpinUntil(() => host.IsConnected && guest.IsConnected, 5000),
                "a dual-stack direct host should accept the guest");
        }
        finally
        {
            host.Cleanup();
            guest.Cleanup();
            host.Dispose();
            guest.Dispose();
        }
    }

    [Fact]
    public void DirectJoin_RejectsAnAddressThatIsNotAnIpLiteral()
    {
        using var manager = new NetworkManager();

        Assert.False(manager.StartConnectingDirect("some-host:8888"));
        Assert.Contains("ADDRESS", manager.StatusText.ToUpperInvariant());
    }

    [Fact]
    public void DirectJoin_AcceptsAnIpv4AddressWithAPort()
    {
        using var manager = new NetworkManager();

        // Nothing listens on the port, so the connect fails later; parsing must still succeed.
        Assert.True(manager.StartConnectingDirect("127.0.0.1:1"));
        Assert.True(manager.IsConnected == false);
        manager.Cleanup();
    }

    [Fact]
    public void LanSearch_SurvivesCancelAndCleanup()
    {
        using var manager = new NetworkManager();

        Assert.True(manager.StartLanSearch());
        Assert.True(manager.IsSearchingLan);
        manager.Cleanup();
        Assert.False(manager.IsSearchingLan);
    }

    [Fact]
    public void LanSearch_DialsTheHostItHears()
    {
        var host = new NetworkManager();
        int port = FreeTcpPort();
        try
        {
            Assert.True(host.StartHostingDirect(port), "the host should listen without a relay");
            using var guest = new NetworkManager();
            Assert.True(guest.StartLanSearch());

            // Feed the guest the beacon a real host would broadcast.
            LanBeacon.SendTo(new IPEndPoint(IPAddress.Loopback, LanBeacon.DiscoveryPort), port);

            Assert.True(SpinWait.SpinUntil(() =>
            {
                guest.PollLanSearch();
                return host.IsConnected && guest.IsConnected;
            }, 5000), "the guest should dial the host it heard on the LAN");
        }
        finally
        {
            host.Cleanup();
            host.Dispose();
        }
    }
}
