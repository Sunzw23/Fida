using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fida.Network;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// 覆盖 relay 模式的客户端行为契约（房间码协议）：
/// 1. GameClient 在 relay 模式下发送 'C' + 0x01 + 4位房间码，收到 0x03 才触发 Connected。
/// 2. 错误房间码 → relay 断开 → 触发 Disconnected（"连接失败"），不触发 Connected。
/// 3. 直连模式（roleByte=0）不发任何字节。
/// 4. GameRelayHost 启动时发送 'H'，收到 0x01+房间码 后 RoomCode 可用，然后阻塞等待客机（0x02）。
/// 5. ServerEntry.IsRelay 从 CSV 第 4 列解析（"relay" 不区分大小写）。
/// 6. 端到端：完整握手后主机/客机经 relay 双向收发 NetworkMessage。
/// </summary>
public class RelayTests
{
    private const string RoomCode = "TEST";

    /// <summary>
    /// 假 relay（协议级）：
    /// - 收到 'H' → 回 0x01 + "TEST"（不发 0x02，host 停在"等待对手"）。
    /// - 收到 'C' → 再读 5 字节（0x01+4码），码==TEST 回 0x03；否则直接断开。
    /// 记录收到的全部字节到 <see cref="Received"/>。
    /// </summary>
    private sealed class FakeRelay : IDisposable
    {
        public TcpListener Listener { get; }
        public readonly System.Collections.Concurrent.ConcurrentQueue<byte> Received = new();
        public readonly System.Collections.Concurrent.ConcurrentQueue<int> Accepts = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly List<TcpClient> _conns = new();

        public FakeRelay()
        {
            Listener = new TcpListener(IPAddress.Loopback, 0);
            Listener.Start();
            Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    TcpClient conn;
                    try { conn = await Listener.AcceptTcpClientAsync(_cts.Token); }
                    catch (OperationCanceledException) { break; }
                    catch { break; }
                    Accepts.Enqueue(1);
                    lock (_conns) _conns.Add(conn);
                    _ = Task.Run(() => Handle(conn));
                }
            });
        }

        private void Handle(TcpClient conn)
        {
            try
            {
                var stream = conn.GetStream();
                byte[] first = new byte[1];
                int n = stream.Read(first, 0, 1);
                if (n <= 0) return;
                Received.Enqueue(first[0]);

                if (first[0] == 'H')
                {
                    // host：回 0x01 + 房间码；不发 0x02 → host 停在等待客机
                    stream.Write(new byte[] { 0x01, (byte)'T', (byte)'E', (byte)'S', (byte)'T' }, 0, 5);
                    stream.Flush();
                }
                else if (first[0] == 'C')
                {
                    // client：再读 0x01 + 4位码
                    byte[] rest = new byte[5];
                    int m = 0;
                    while (m < 5)
                    {
                        int k = stream.Read(rest, m, 5 - m);
                        if (k <= 0) return;
                        m += k;
                    }
                    for (int i = 0; i < 5; i++) Received.Enqueue(rest[i]);
                    string code = System.Text.Encoding.ASCII.GetString(rest, 1, 4);
                    if (code == RoomCode)
                    {
                        stream.Write(new byte[] { 0x03 }, 0, 1);
                        stream.Flush();
                    }
                    else
                    {
                        // 错误码 → 直接断开（真实 relay 行为）
                        conn.Close();
                    }
                }
            }
            catch { }
        }

        public int Port => ((IPEndPoint?)Listener.Server.LocalEndPoint)!.Port;
        public void Dispose()
        {
            _cts.Cancel();
            Listener.Stop();
            lock (_conns)
            {
                foreach (var c in _conns) { try { c.Close(); } catch { } }
                _conns.Clear();
            }
        }
    }

    [Fact]
    public void GameClient_Connect_RelayRole_SendsRoleByteCAndCode()
    {
        using var relay = new FakeRelay();
        var client = new GameClient();
        bool connected = false;
        client.Connected += () => connected = true;

        client.Connect("127.0.0.1", relay.Port, roleByte: (byte)'C', roomCode: "TEST");

        SpinWait.SpinUntil(() => relay.Accepts.Count >= 1, 3000);
        SpinWait.SpinUntil(() => relay.Received.Count >= 6, 3000);
        SpinWait.SpinUntil(() => connected, 3000);

        Assert.True(connected, "GameClient 应在收到 0x03 后触发 Connected");
        // 完整 6 字节序列：'C', 0x01, 'T','E','S','T'
        var expected = new byte[] { (byte)'C', 0x01, (byte)'T', (byte)'E', (byte)'S', (byte)'T' };
        var got = new byte[6];
        for (int i = 0; i < 6; i++) got[i] = relay.Received.TryDequeue(out var b) ? b : (byte)0;
        Assert.Equal(expected, got);
    }

    [Fact]
    public void GameClient_Connect_RelayRole_WrongCode_Disconnects()
    {
        using var relay = new FakeRelay();
        var client = new GameClient();
        string? disconnectMsg = null;
        client.Disconnected += (m) => disconnectMsg = m;
        bool connected = false;
        client.Connected += () => connected = true;

        client.Connect("127.0.0.1", relay.Port, roleByte: (byte)'C', roomCode: "ZZZZ");

        SpinWait.SpinUntil(() => disconnectMsg != null, 5000);
        Assert.NotNull(disconnectMsg);
        Assert.Contains("Connection failed", disconnectMsg!);
        Assert.False(connected, "错误房间码不应触发 Connected");
    }

    [Fact]
    public void GameClient_Connect_Direct_SendsNoRoleByte()
    {
        using var relay = new FakeRelay();
        var client = new GameClient();
        client.Connect("127.0.0.1", relay.Port);

        SpinWait.SpinUntil(() => relay.Accepts.Count >= 1, 3000);
        // 直连模式不应发送任何字节
        Thread.Sleep(150);
        Assert.Empty(relay.Received);
    }

    [Fact]
    public void ServerEntry_IsRelay_ParsedFromFourthColumn()
    {
        string path = Path.Combine(Path.GetTempPath(), $"fida_relay_test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, "# comment\n127.0.0.1,8888,本地,relay\n::1,9999,直连\n");
        try
        {
            var cfg = ServerConfig.Load(path);
            var entries = cfg.GetServers();
            Assert.Equal(2, entries.Count);
            Assert.True(entries[0].IsRelay);
            Assert.False(entries[1].IsRelay);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ServerEntry_IsRelay_CaseInsensitive()
    {
        string path = Path.Combine(Path.GetTempPath(), $"fida_relay_test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, "127.0.0.1,8888,本地,RELAY\n");
        try
        {
            var cfg = ServerConfig.Load(path);
            Assert.True(cfg.GetServers()[0].IsRelay);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GameRelayHost_RoleHostConstant_IsH()
    {
        Assert.Equal((byte)'H', GameRelayHost.RoleHost);
    }

    [Fact]
    public void GameRelayHost_Start_SendsRoleByteHAndReceivesCode()
    {
        using var relay = new FakeRelay();
        var host = new GameRelayHost();
        try
        {
            host.Start("127.0.0.1", relay.Port);
            SpinWait.SpinUntil(() => relay.Received.TryPeek(out byte first) && first == 'H', 3000);
            byte role = relay.Received.TryDequeue(out var b) ? b : (byte)0;
            Assert.Equal((byte)'H', role);
            Assert.True(host.IsRunning);

            // host 收到 0x01+房间码 后，RoomCode 可用；随后阻塞等待客机（0x02 不发）
            SpinWait.SpinUntil(() => host.RoomCode == RoomCode, 3000);
            Assert.Equal(RoomCode, host.RoomCode);
        }
        finally
        {
            host.Dispose();
        }
    }

    /// <summary>
    /// 端到端行为测试：完整房间码握手后，模拟 relay 桥接（host_conn &lt;-&gt; client_conn），
    /// 验证主机 GameRelayHost 和客机 GameClient 经 relay 能双向收发 NetworkMessage。
    /// </summary>
    [Fact]
    public void EndToEnd_RelayBridging_HostClientExchangeMessages()
    {
        using var fakeRelay = new EndToEndRelay();

        // 主机端 GameRelayHost：'H' → 收码 → 等 0x02
        using var host = new GameRelayHost();
        host.Start("127.0.0.1", fakeRelay.Port);

        // 客机端 GameClient：'C'+0x01+码 → 收 0x03
        using var client = new GameClient();
        bool clientConnected = false;
        client.Connected += () => clientConnected = true;
        client.Connect("127.0.0.1", fakeRelay.Port, roleByte: (byte)'C', roomCode: "TEST");

        // 等待双方都"看到对端"
        SpinWait.SpinUntil(() => host.HasClientConnected, 5000);
        SpinWait.SpinUntil(() => client.IsConnected, 5000);
        Assert.True(host.HasClientConnected, "主机端应看到客机桥接进来");
        Assert.True(clientConnected, "客机端应触发 Connected");

        // 客机 -> 主机
        client.SendMessage(new NetworkMessage(MessageType.ChatMessage, System.Text.Encoding.UTF8.GetBytes("hello")));
        List<NetworkMessage> hostMsg = new();
        for (int i = 0; i < 500 && hostMsg.Count == 0; i++)
        {
            hostMsg = host.ReceiveMessages();
            if (hostMsg.Count == 0) Thread.Sleep(20);
        }
        Assert.NotEmpty(hostMsg);
        Assert.Equal(MessageType.ChatMessage, hostMsg[0].Type);
        Assert.Equal("hello", System.Text.Encoding.UTF8.GetString(hostMsg[0].Payload));

        // 主机 -> 客机
        host.SendToClient(new NetworkMessage(MessageType.ChatMessage, System.Text.Encoding.UTF8.GetBytes("world")));
        List<NetworkMessage> clientMsg = new();
        for (int i = 0; i < 500 && clientMsg.Count == 0; i++)
        {
            clientMsg = client.ReceiveMessages();
            if (clientMsg.Count == 0) Thread.Sleep(20);
        }
        Assert.NotEmpty(clientMsg);
        Assert.Equal(MessageType.ChatMessage, clientMsg[0].Type);
        Assert.Equal("world", System.Text.Encoding.UTF8.GetString(clientMsg[0].Payload));
    }

    /// <summary>
    /// 端到端用的假 relay：完整握手后双向字节桥接。
    /// host 侧：收 'H' → 回 0x01+"TEST"。
    /// client 侧：收 'C'+0x01+4码 → 回 0x03，并向 host 发 0x02。
    /// 然后双向 Pump。
    /// </summary>
    private sealed class EndToEndRelay : IDisposable
    {
        public readonly int Port;
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly List<TcpClient> _conns = new();

        public EndToEndRelay()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint?)_listener.Server.LocalEndPoint)!.Port;

            Task.Run(async () =>
            {
                try
                {
                    var c1 = await _listener.AcceptTcpClientAsync(_cts.Token);
                    var c2 = await _listener.AcceptTcpClientAsync(_cts.Token);
                    lock (_conns) { _conns.Add(c1); _conns.Add(c2); }

                    Socket s1 = c1.Client, s2 = c2.Client;
                    byte[] r1 = new byte[1];
                    byte[] r2 = new byte[1];
                    s1.Receive(r1);
                    s2.Receive(r2);
                    bool c1IsHost = r1[0] == 'H';
                    Socket host = c1IsHost ? s1 : s2;
                    Socket client = c1IsHost ? s2 : s1;

                    // host 握手：回 0x01 + 房间码
                    host.Send(new byte[] { 0x01, (byte)'T', (byte)'E', (byte)'S', (byte)'T' }, 0, 5, SocketFlags.None);

                    // client 握手：读 0x01+4码，回 0x03，并向 host 发 0x02
                    ReadExact(client, 5);
                    client.Send(new byte[] { 0x03 }, 0, 1, SocketFlags.None);
                    host.Send(new byte[] { 0x02 }, 0, 1, SocketFlags.None);

                    // 双向字节桥接
                    var t1 = Task.Run(() => Pump(host, client));
                    var t2 = Task.Run(() => Pump(client, host));
                    await Task.WhenAll(t1, t2);
                }
                catch { }
            });
        }

        private static byte[] ReadExact(Socket s, int count)
        {
            byte[] buf = new byte[count];
            int off = 0;
            while (off < count)
            {
                int n = s.Receive(buf, off, count - off, SocketFlags.None);
                if (n <= 0) return buf;
                off += n;
            }
            return buf;
        }

        private static void Pump(Socket src, Socket dst)
        {
            byte[] buf = new byte[65536];
            try
            {
                while (true)
                {
                    int n = src.Receive(buf, 0, buf.Length, SocketFlags.None);
                    if (n <= 0) break;
                    dst.Send(buf, 0, n, SocketFlags.None);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            lock (_conns)
            {
                foreach (var c in _conns) { try { c.Close(); } catch { } }
                _conns.Clear();
            }
        }
    }
}
