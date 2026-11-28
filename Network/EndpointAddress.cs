#nullable enable
using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Fida.Network
{
    /// <summary>
    /// A direct endpoint a player typed or read off the hosting screen: an IP literal plus a port.
    ///
    /// Direct play has no rendezvous server, so the guest types what the host sees on the
    /// waiting screen. The accepted forms are the ones the host can actually print — a bare
    /// literal that falls back to the configured default port ("2408::1"), the bracketed IPv6
    /// form with an explicit port ("[2408::1]:8888"), and plain IPv4 ("192.168.1.7:8888").
    /// Host names are rejected on purpose: the transport dials IP literals, and resolving a
    /// name on the UI thread would stall the menu.
    /// </summary>
    public readonly struct EndpointAddress : IEquatable<EndpointAddress>
    {
        public const int DefaultPort = 8888;

        public IPAddress Address { get; }
        public int Port { get; }

        public EndpointAddress(IPAddress address, int port)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (port is <= 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            Address = address;
            Port = port;
        }

        /// <summary>Bracketed form used when printing an endpoint, e.g. "[2408::1]:8888".</summary>
        public static string Format(IPAddress address, int port)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            string text = address.AddressFamily == AddressFamily.InterNetworkV6
                ? "[" + address + "]"
                : address.ToString();
            return text + ":" + port.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses a typed endpoint. A missing port falls back to <paramref name="defaultPort"/>;
        /// a bare IPv6 literal keeps the default port because "2408::1:8888" is ambiguous.
        /// </summary>
        public static bool TryParse(string? text, out EndpointAddress endpoint, int defaultPort = DefaultPort)
        {
            endpoint = default;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (defaultPort is <= 0 or > 65535) defaultPort = DefaultPort;

            string value = text.Trim();

            // Accept a pasted "fida://host:port" or "host:port/" shape without lecturing the player.
            int scheme = value.IndexOf("://", StringComparison.Ordinal);
            if (scheme >= 0) value = value[(scheme + 3)..];
            value = value.TrimEnd('/');
            if (value.Length == 0) return false;

            string host;
            int port = defaultPort;

            if (value[0] == '[')
            {
                int close = value.IndexOf(']');
                if (close <= 1) return false;
                host = value[1..close];
                string rest = value[(close + 1)..];
                if (rest.Length > 0)
                {
                    if (rest[0] != ':' || !TryParsePort(rest[1..], out port)) return false;
                }
            }
            else
            {
                int first = value.IndexOf(':');
                int last = value.LastIndexOf(':');
                // More than one colon without brackets is an IPv6 literal, not host:port.
                if (first >= 0 && first == last)
                {
                    host = value[..first];
                    if (!TryParsePort(value[(first + 1)..], out port)) return false;
                }
                else
                {
                    host = value;
                }
            }

            if (host.Length == 0) return false;
            if (!IPAddress.TryParse(host, out IPAddress? address)) return false;
            endpoint = new EndpointAddress(address, port);
            return true;
        }

        private static bool TryParsePort(string text, out int port)
        {
            port = 0;
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)) return false;
            if (parsed is <= 0 or > 65535) return false;
            port = parsed;
            return true;
        }

        /// <summary>Address string without the port, ready for the socket APIs.</summary>
        public string HostText => Address.ToString();

        public override string ToString() => Format(Address, Port);

        public bool Equals(EndpointAddress other) =>
            Port == other.Port && Equals(Address, other.Address);

        public override bool Equals(object? obj) => obj is EndpointAddress other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Address, Port);

        public static bool operator ==(EndpointAddress left, EndpointAddress right) => left.Equals(right);

        public static bool operator !=(EndpointAddress left, EndpointAddress right) => !left.Equals(right);
    }
}
