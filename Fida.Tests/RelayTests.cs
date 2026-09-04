using System.Net;
using System.Net.Sockets;
using Fida.Network;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// 覆盖 relay 模式的客户端行为契约：
/// 1. GameClient 在 relay 模式下发送角色字节 'C'（与 GameRelayHost.RoleHost='H' 对称）。
/// 2. GameRelayHost 启动时发送 'H'。
/// 3. ServerEntry.IsRelay 从 CSV 第 4 列解析（"relay" 不区分大小写）。
/// </summary>
public class RelayTests
{
    private sealed class FakeRelay : IDisposable
    {
        public TcpListener Listener { get; }
        public readonly System.Collections.Concurrent.ConcurrentQueue<byte> Received = new();
        public readonly System.Collections.Concurrent.ConcurrentQueue<int> Accepts = new();
        private readonly CancellationTokenSource _cts = new();

        public FakeRelay()
        {
            Listener = new TcpListener(IPAddress.Loopback, 0);
            Listener.Start();
            Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        var conn = await Listener.AcceptTcpClientAsync(_cts.Token);
                        Accepts.Enqueue(1);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                byte[] buf = new byte[4096];
                                while (!_cts.IsCancellationRequested)
                                {
                                    int n = await conn.GetStream().ReadAsync(buf, 0, buf.Length, _cts.Token);
                                    if (n <= 0) break;
                                    for (int i = 0; i < n; i++) Received.Enqueue(buf[i]);
                                }
                            }
                            catch { }
                        });
                    }
                    catch (OperationCanceledException) { break; }
                    catch { }
                }
            });
        }

        public int Port => ((IPEndPoint?)Listener.Server.LocalEndPoint)!.Port;
        public void Dispose()
        {
            _cts.Cancel();
            Listener.Stop();
        }
    }

    [Fact]
    public void GameClient_Connect_RelayRole_SendsRoleByteC()
    {
        using var relay = new FakeRelay();
        var client = new GameClient();
        bool connected = false;
        client.Connected += () => connected = true;

        client.Connect("127.0.0.1", relay.Port, roleByte: (byte)'C');

        SpinWait.SpinUntil(() => relay.Accepts.Count >= 1, 3000);
        SpinWait.SpinUntil(() => relay.Received.TryPeek(out byte first) && first == 'C', 3000);

        Assert.True(connected, "GameClient 应在角色字节发送后触发 Connected");
        byte role = relay.Received.TryDequeue(out var b) ? b : (byte)0;
        Assert.Equal((byte)'C', role);
    }

    [Fact]
    public void GameClient_Connect_Direct_SendsNoRoleByte()
    {
        using var relay = new FakeRelay();
        var client = new GameClient();
        client.Connect("127.0.0.1", relay.Port);

        SpinWait.SpinUntil(() => relay.Accepts.Count >= 1, 3000);
        // 直连模式不应发送任何角色字节
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
    public void GameRelayHost_Start_SendsRoleByteHToRelay()
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
        }
        finally
        {
            host.Dispose();
        }
    }

    /// <summary>
    /// 端到端行为测试：模拟 relay 桥接（host_conn <-> client_conn），
    /// 验证主机 GameRelayHost 和客机 GameClient 经 relay 能双向收发 NetworkMessage。
    /// </summary>
    [Fact]
    public void EndToEnd_RelayBridging_HostClientExchangeMessages()
    {
        // 1. 启动一个"假 relay"：它接受两个连接，收到 'H' 和 'C' 后双向字节桥接
        using var fakeRelay = new EndToEndRelay();

        // 2. 主机端 GameRelayHost
        using var host = new GameRelayHost();
        host.Start("127.0.0.1", fakeRelay.Port);

        // 3. 客机端 GameClient（发 'C'）
        using var client = new GameClient();
        bool clientConnected = false;
        client.Connected += () => clientConnected = true;
        client.Connect("127.0.0.1", fakeRelay.Port, roleByte: (byte)'C');

        // 4. 等待双方都"看到对端"
        SpinWait.SpinUntil(() => host.HasClientConnected, 5000);
        SpinWait.SpinUntil(() => client.IsConnected, 5000);
        Assert.True(host.HasClientConnected, "主机端应看到客机桥接进来");
        Assert.True(clientConnected, "客机端应触发 Connected");

        // 5. 客机 -> 主机
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

        // 6. 主机 -> 客机
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
    /// 端到端用的假 relay：接受两个连接，分别收 'H'/'C'，然后字节桥接。
    /// </summary>
    private sealed class EndToEndRelay : IDisposable
    {
        public readonly int Port;
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private Socket? _host;
        private Socket? _client;
        private readonly object _lock = new();

        public EndToEndRelay()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint?)_listener.Server.LocalEndPoint)!.Port;

            Task.Run(async () =>
            {
                try
                {
                    var c1 = (await _listener.AcceptTcpClientAsync(_cts.Token)).Client;
                    var c2 = (await _listener.AcceptTcpClientAsync(_cts.Token)).Client;
                    byte[] r1 = new byte[1];
                    byte[] r2 = new byte[1];
                    c1.Receive(r1); c2.Receive(r2);
                    bool c1IsHost = r1[0] == 'H';
                    lock (_lock)
                    {
                        _host = c1IsHost ? c1 : c2;
                        _client = c1IsHost ? c2 : c1;
                    }
                    // 双向字节桥接
                    var t1 = Task.Run(() => Pump(_host!, _client!));
                    var t2 = Task.Run(() => Pump(_client!, _host!));
                    await Task.WhenAll(t1, t2);
                }
                catch { }
            });
        }

        private static async Task Pump(Socket src, Socket dst)
        {
            byte[] buf = new byte[65536];
            try
            {
                while (true)
                {
                    int n = await src.ReceiveAsync(buf, SocketFlags.None);
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
        }
    }
}
