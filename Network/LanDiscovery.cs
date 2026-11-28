#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fida.Network
{
    /// <summary>A host that answered a LAN discovery broadcast.</summary>
    public readonly record struct LanHost(string Address, int Port);

    /// <summary>
    /// The LAN discovery wire format: <c>FIDA1|&lt;gamePort&gt;</c> as ASCII.
    /// Small and versioned by the magic string so an old build never mistakes a beacon.
    /// </summary>
    public static class LanDiscoveryPacket
    {
        /// <summary>UDP port every build listens on. It is not the game port.</summary>
        public const int DiscoveryPort = 8890;
        public const int MaxBytes = 24;
        private const string Magic = "FIDA1";

        public static byte[] Build(int gamePort)
        {
            if (gamePort is <= 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(gamePort));
            return Encoding.ASCII.GetBytes(Magic + "|" + gamePort.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public static bool TryParse(byte[]? payload, int length, out int gamePort)
        {
            gamePort = 0;
            if (payload == null || length <= 0 || length > MaxBytes || length > payload.Length) return false;
            string text = Encoding.ASCII.GetString(payload, 0, length);
            string[] parts = text.Split('|');
            if (parts.Length != 2 || parts[0] != Magic) return false;
            if (!int.TryParse(parts[1], out int port) || port is <= 0 or > 65535) return false;
            gamePort = port;
            return true;
        }
    }

    /// <summary>
    /// Advertises a hosted game on the local network so the guest can press Join without
    /// typing anything. Sends one small UDP datagram per second to each interface's directed
    /// broadcast address; the guest's <see cref="LanBrowser"/> listens for it.
    /// </summary>
    public sealed class LanBeacon : IDisposable
    {
        public const int DiscoveryPort = LanDiscoveryPacket.DiscoveryPort;

        private readonly byte[] _payload;
        private readonly UdpClient _udp;
        private Timer? _timer;
        private bool _disposed;

        public LanBeacon(int gamePort)
        {
            _payload = LanDiscoveryPacket.Build(gamePort);
            _udp = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
        }

        public void Start()
        {
            if (_disposed || _timer != null) return;
            _timer = new Timer(_ => Broadcast(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        /// <summary>One-shot sender, also the deterministic test seam (no timer involved).</summary>
        public static void SendTo(IPEndPoint target, int gamePort)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            using var udp = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
            byte[] payload = LanDiscoveryPacket.Build(gamePort);
            try { udp.Send(payload, payload.Length, target); }
            catch (SocketException) { /* A disconnected adapter must not take hosting down. */ }
            catch (ObjectDisposedException) { }
        }

        private void Broadcast()
        {
            if (_disposed) return;
            foreach (IPAddress address in BroadcastTargets())
            {
                try
                {
                    _udp.Send(_payload, _payload.Length, new IPEndPoint(address, DiscoveryPort));
                }
                catch (SocketException) { }
                catch (ObjectDisposedException) { return; }
            }
        }

        /// <summary>Directed broadcast per adapter, plus the limited broadcast as a fallback.</summary>
        internal static IEnumerable<IPAddress> BroadcastTargets()
        {
            var seen = new HashSet<IPAddress>();
            NetworkInterface[] interfaces;
            try { interfaces = NetworkInterface.GetAllNetworkInterfaces(); }
            catch (NetworkInformationException) { interfaces = Array.Empty<NetworkInterface>(); }

            foreach (NetworkInterface nic in interfaces)
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                IPInterfaceProperties properties;
                try { properties = nic.GetIPProperties(); }
                catch (NetworkInformationException) { continue; }
                foreach (UnicastIPAddressInformation info in properties.UnicastAddresses)
                {
                    if (info.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (info.IPv4Mask is not { } mask) continue;
                    byte[] address = info.Address.GetAddressBytes();
                    byte[] maskBytes = mask.GetAddressBytes();
                    if (address.Length != 4 || maskBytes.Length != 4) continue;
                    var broadcast = new byte[4];
                    for (int i = 0; i < 4; i++) broadcast[i] = (byte)(address[i] | ~maskBytes[i]);
                    var target = new IPAddress(broadcast);
                    if (seen.Add(target)) yield return target;
                }
            }

            if (seen.Count == 0 && seen.Add(IPAddress.Broadcast)) yield return IPAddress.Broadcast;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
            _udp.Dispose();
        }
    }

    /// <summary>
    /// Listens for <see cref="LanBeacon"/> datagrams while the guest waits on the connecting
    /// screen. Keeps the first host it hears and reports how long it has been listening so the
    /// UI can give up after a couple of seconds.
    /// </summary>
    public sealed class LanBrowser : IDisposable
    {
        public const int DiscoveryPort = LanDiscoveryPacket.DiscoveryPort;

        private readonly UdpClient _udp;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentQueue<LanHost> _hosts = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly Task _pump;
        private bool _disposed;

        public LanBrowser(int discoveryPort = DiscoveryPort)
        {
            _udp = new UdpClient(AddressFamily.InterNetwork);
            // Two game instances on one machine must not fight over the port.
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));
            _pump = Task.Run(PumpAsync);
        }

        public TimeSpan Elapsed => _clock.Elapsed;

        public bool TryTakeHost(out LanHost host)
        {
            while (!_disposed && _hosts.TryDequeue(out LanHost found))
            {
                host = found;
                return true;
            }
            host = default;
            return false;
        }

        private async Task PumpAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _udp.ReceiveAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (SocketException)
                {
                    // Windows surfaces ICMP port-unreachable as a socket error; keep listening.
                    try { await Task.Delay(50, _cts.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                    continue;
                }

                if (!LanDiscoveryPacket.TryParse(result.Buffer, result.Buffer.Length, out int port)) continue;
                IPAddress from = result.RemoteEndPoint.Address;
                if (from.IsIPv4MappedToIPv6) from = from.MapToIPv4();
                _hosts.Enqueue(new LanHost(from.ToString(), port));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts.Cancel();
            _udp.Dispose();
            try { _pump.Wait(TimeSpan.FromMilliseconds(250)); } catch (AggregateException) { }
            _cts.Dispose();
        }
    }
}
