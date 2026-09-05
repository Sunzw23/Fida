using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fida.Network
{
    /// <summary>
    /// 中继主机：通过出站连接连入公网 relay，发送角色字节 'H' 标识主机，
    /// 接收 relay 分配的房间码，等待客机加入（0x02 peer-joined 信号）后
    /// 把 relay 流当作"对端"来收发帧消息。
    /// 协议：'H' → 0x01+4位房间码（等待客机）→ 'C'+0x01+房间码 → 0x03 ack / 0x02 peer-joined → 纯字节桥接。
    /// 对外暴露与 <see cref="GameServer"/> 相同的 API（HasClientConnected /
    /// SendToClient / ReceiveMessages / Stop），使 <see cref="NetworkManager"/>
    /// 对 relay 与直连两种模式统一处理。
    /// </summary>
    public class GameRelayHost : IDisposable
    {
        public const byte RoleHost = (byte)'H';
        public const byte MsgCodeAssign = 0x01;
        public const byte MsgPeerJoined = 0x02;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<NetworkMessage> _incomingMessages = new();
        private readonly ConcurrentQueue<NetworkMessage> _outgoingMessages = new();
        private bool _isRunning;
        private bool _clientConnected;
        private bool _disposed;
        private string _roomCode = string.Empty;

        /// <summary>是否正在运行</summary>
        public bool IsRunning => _isRunning;

        /// <summary>是否有客机连入（relay 模式下，连上 relay 即为 true）</summary>
        public bool HasClientConnected => _clientConnected && _tcpClient != null && _tcpClient.Connected;

        /// <summary>relay 分配的房间码（客机用此码加入）</summary>
        public string RoomCode => _roomCode;

        /// <summary>客机连接时触发</summary>
        public event Action ClientConnected;

        /// <summary>客机断开时触发</summary>
        public event Action<string> ClientDisconnected;

        /// <summary>非阻塞启动：连接 relay、接收房间码、等待客机加入（0x02）后桥接。</summary>
        public void Start(string relayAddress, int relayPort)
        {
            if (_isRunning) return;
            _isRunning = true;
            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    // dual-stack：与 GameClient 一致
                    _tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
                    _tcpClient.Client.DualMode = true;
                    await _tcpClient.ConnectAsync(relayAddress, relayPort);

                    _stream = _tcpClient.GetStream();

                    // 发送角色字节 'H'，标识为主机
                    await _stream.WriteAsync(new byte[] { RoleHost }, _cts.Token);
                    await _stream.FlushAsync(_cts.Token);

                    // relay 回 0x01 + 4 位 ASCII 房间码
                    var codeBuf = new byte[5];
                    await ReadFullAsync(_stream, codeBuf, _cts.Token);
                    if (codeBuf[0] != MsgCodeAssign)
                        throw new IOException($"握手失败: 期望 0x{MsgCodeAssign:X2}，收到 0x{codeBuf[0]:X2}");
                    _roomCode = Encoding.ASCII.GetString(codeBuf, 1, 4);

                    // 阻塞等待客机加入（relay 发 0x02）；
                    // 期间不可取消——用户"取消"时 Stop() 直接关闭流使本读失败退出
                    await _stream.ReadAsync(new byte[] { 0 }, 0, 1);
                    if (_cts.Token.IsCancellationRequested) return;

                    _clientConnected = true;
                    ClientConnected?.Invoke();

                    // 启动收发循环
                    var receiveTask = ReceiveLoop(_cts.Token);
                    var sendTask = SendLoop(_cts.Token);
                    await Task.WhenAll(receiveTask, sendTask);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (_clientConnected)
                    {
                        _clientConnected = false;
                        ClientDisconnected?.Invoke(ex.Message);
                    }
                }
            });
        }

        /// <summary>同步读满 count 字节（阻塞直到读满或流断开）</summary>
        private static async Task ReadFullAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int n = await stream.ReadAsync(buffer, offset, buffer.Length - offset, ct);
                if (n <= 0)
                    throw new IOException("relay 连接在握手期间断开");
                offset += n;
            }
        }

        /// <summary>发送消息给客机</summary>
        public void SendToClient(NetworkMessage message)
        {
            if (!_clientConnected) return;
            _outgoingMessages.Enqueue(message);
        }

        /// <summary>获取所有待处理的消息（非阻塞）</summary>
        public List<NetworkMessage> ReceiveMessages()
        {
            var messages = new List<NetworkMessage>();
            while (_incomingMessages.TryDequeue(out NetworkMessage message))
            {
                messages.Add(message);
            }
            return messages;
        }

        /// <summary>停止并释放资源</summary>
        public void Stop()
        {
            _isRunning = false;
            _clientConnected = false;
            _cts?.Cancel();

            try
            {
                _stream?.Close();
                _stream?.Dispose();
                _tcpClient?.Close();
                _tcpClient?.Dispose();
            }
            catch { }

            _stream = null;
            _tcpClient = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _cts?.Dispose();
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _tcpClient != null && _tcpClient.Connected)
                {
                    NetworkMessage message = await Task.Run(() =>
                    {
                        try { return NetworkMessage.ReadFromStream(_stream); }
                        catch { return null; }
                    }, ct);

                    if (message == null)
                        break;

                    _incomingMessages.Enqueue(message);
                }
            }
            catch (OperationCanceledException) { }
            catch { }

            if (_clientConnected)
            {
                _clientConnected = false;
                ClientDisconnected?.Invoke("与 relay 的连接已断开");
            }
        }

        private async Task SendLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _tcpClient != null && _tcpClient.Connected)
                {
                    if (_outgoingMessages.TryDequeue(out NetworkMessage message))
                    {
                        byte[] data = message.Serialize();
                        await _stream.WriteAsync(data, 0, data.Length, ct);
                        await _stream.FlushAsync(ct);
                    }
                    else
                    {
                        await Task.Delay(10, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }
    }
}
