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
    /// TCP 游戏客户端，使用 IPv6 地址连接服务器
    /// </summary>
    public class GameClient : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<NetworkMessage> _incomingMessages = new();
        private readonly ConcurrentQueue<NetworkMessage> _outgoingMessages = new();
        private bool _isConnected;
        private bool _disposed;

        /// <summary>
        /// 客户端是否已连接
        /// </summary>
        public bool IsConnected => _isConnected && _tcpClient != null && _tcpClient.Connected;

        /// <summary>
        /// 当连接断开时触发
        /// </summary>
        public event Action<string> Disconnected;

        /// <summary>
        /// 当连接成功时触发
        /// </summary>
        public event System.Action Connected;

        /// <summary>
        /// 连接到指定地址和端口的服务器（非阻塞）。
        /// roleByte 用于 relay 模式：客机发 'C' 告诉 relay 自己是客机角色（见 GameRelayHost）。
        /// </summary>
        /// <param name="ipAddress">地址字符串</param>
        /// <param name="port">端口号</param>
        /// <param name="roleByte">可选，relay 角色字节（0 = 无角色）</param>
        public void Connect(string ipAddress, int port, byte roleByte = 0)
        {
            if (_isConnected)
                return;

            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    IPAddress addr = IPAddress.Parse(ipAddress);
                    _tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
                    _tcpClient.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                    _tcpClient.NoDelay = true;

                    await _tcpClient.ConnectAsync(addr, port, _cts.Token);
                    _stream = _tcpClient.GetStream();
                    if (roleByte != 0)
                    {
                        await _stream.WriteAsync(new byte[] { roleByte }, _cts.Token);
                        await _stream.FlushAsync(_cts.Token);
                    }
                    _isConnected = true;

                    Connected?.Invoke();

                    // 启动接收和发送循环
                    _ = Task.Run(() => ReceiveLoop(_cts.Token));
                    _ = Task.Run(() => SendLoop(_cts.Token));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Disconnected?.Invoke($"连接失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 发送消息给服务器
        /// </summary>
        public void SendMessage(NetworkMessage message)
        {
            if (!IsConnected)
                return;

            _outgoingMessages.Enqueue(message);
        }

        /// <summary>
        /// 获取所有待处理的消息（非阻塞）
        /// </summary>
        public List<NetworkMessage> ReceiveMessages()
        {
            var messages = new List<NetworkMessage>();
            while (_incomingMessages.TryDequeue(out NetworkMessage message))
            {
                messages.Add(message);
            }
            return messages;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _isConnected = false;
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

        private async Task ReceiveLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _tcpClient != null && _tcpClient.Connected)
                {
                    NetworkMessage message = await Task.Run(() =>
                    {
                        try
                        {
                            return NetworkMessage.ReadFromStream(_stream);
                        }
                        catch
                        {
                            return null;
                        }
                    }, ct);

                    if (message == null)
                    {
                        break;
                    }

                    _incomingMessages.Enqueue(message);
                }
            }
            catch (OperationCanceledException) { }
            catch { }

            if (_isConnected)
            {
                _isConnected = false;
                Disconnected?.Invoke("与服务器的连接已断开");
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
            _cts?.Dispose();
        }
    }
}