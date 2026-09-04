using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fida.Network
{
    /// <summary>
    /// 中继主机：通过出站连接连入公网 relay，发送角色字节 'H' 标识主机，
    /// 之后把 relay 流当作"对端"来收发帧消息。
    /// 对外暴露与 <see cref="GameServer"/> 相同的 API（HasClientConnected /
    /// SendToClient / ReceiveMessages / Stop），使 <see cref="NetworkManager"/>
    /// 对 relay 与直连两种模式统一处理。
    /// </summary>
    public class GameRelayHost : IDisposable
    {
        public const byte RoleHost = (byte)'H';

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<NetworkMessage> _incomingMessages = new();
        private readonly ConcurrentQueue<NetworkMessage> _outgoingMessages = new();
        private bool _isRunning;
        private bool _clientConnected;
        private bool _disposed;

        /// <summary>是否正在运行</summary>
        public bool IsRunning => _isRunning;

        /// <summary>是否有客机连入（relay 模式下，连上 relay 即为 true）</summary>
        public bool HasClientConnected => _clientConnected && _tcpClient != null && _tcpClient.Connected;

        /// <summary>客机连接时触发</summary>
        public event Action ClientConnected;

        /// <summary>客机断开时触发</summary>
        public event Action<string> ClientDisconnected;

        /// <summary>非阻塞启动：连接 relay 并等待客机桥接进来。</summary>
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
