using System.Net;
using System.Net.Sockets;
using Fida.Network;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// The direct-connect field accepts exactly what the hosting screen can print.
/// </summary>
public class EndpointAddressTests
{
    [Fact]
    public void ParsesIpv4WithPort()
    {
        Assert.True(EndpointAddress.TryParse("192.168.1.7:9000", out EndpointAddress endpoint));

        Assert.Equal(9000, endpoint.Port);
        Assert.Equal(AddressFamily.InterNetwork, endpoint.Address.AddressFamily);
        Assert.Equal("192.168.1.7", endpoint.HostText);
    }

    [Fact]
    public void FallsBackToTheDefaultPort()
    {
        Assert.True(EndpointAddress.TryParse("192.168.1.7", out EndpointAddress endpoint, 8888));

        Assert.Equal(8888, endpoint.Port);
    }

    [Fact]
    public void ParsesBracketedIpv6WithPort()
    {
        Assert.True(EndpointAddress.TryParse("[2408:8210::1]:6001", out EndpointAddress endpoint));

        Assert.Equal(6001, endpoint.Port);
        Assert.Equal(AddressFamily.InterNetworkV6, endpoint.Address.AddressFamily);
    }

    [Theory]
    [InlineData("2408:8210::1")]
    [InlineData("::1")]
    public void BareIpv6KeepsTheDefaultPort(string text)
    {
        // "2408::1:8888" cannot be split reliably, so a bare literal never carries a port.
        Assert.True(EndpointAddress.TryParse(text, out EndpointAddress endpoint, 7777));

        Assert.Equal(7777, endpoint.Port);
        Assert.Equal(AddressFamily.InterNetworkV6, endpoint.Address.AddressFamily);
    }

    [Fact]
    public void AcceptsAPastedLinkShape()
    {
        Assert.True(EndpointAddress.TryParse("fida://[::1]:9999/", out EndpointAddress endpoint));

        Assert.Equal(9999, endpoint.Port);
        Assert.Equal("::1", endpoint.HostText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("host.local:8888")]
    [InlineData("some-host")]
    [InlineData("192.168.1.7:")]
    [InlineData("192.168.1.7:0")]
    [InlineData("192.168.1.7:70000")]
    [InlineData("[::1")]
    [InlineData("[]:8888")]
    [InlineData("999.1.1.1")]
    public void RejectsWhatTheTransportCannotDial(string text)
    {
        Assert.False(EndpointAddress.TryParse(text, out _));
    }

    [Fact]
    public void FormatBracketsIpv6AndRoundTrips()
    {
        var address = IPAddress.Parse("2408:8210::1");

        string text = EndpointAddress.Format(address, 8888);

        Assert.Equal("[2408:8210::1]:8888", text);
        Assert.True(EndpointAddress.TryParse(text, out EndpointAddress parsed));
        Assert.Equal(address, parsed.Address);
        Assert.Equal(8888, parsed.Port);
    }

    [Fact]
    public void FormatLeavesIpv4Unbracketed()
    {
        Assert.Equal("10.0.0.5:8888", EndpointAddress.Format(IPAddress.Parse("10.0.0.5"), 8888));
    }
}
