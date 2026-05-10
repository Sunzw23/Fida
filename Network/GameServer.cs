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
    /// TCP 游戏服务器，使用 IPv6 地址监听
    /// 负责接受客户端连接并转发消息
    /// </summary>
    public class GameServer : IDisposable
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _clientStream;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<NetworkMessage> _incomingMessages = new();
        private readonly ConcurrentQueue<NetworkMessage> _outgoingMessages = new();
        private bool _isRunning;
        private bool _disposed;
        private bool _clientConnected;

        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 是否有客户端连接
        /// </summary>
        public bool HasClientConnected => _clientConnected && _client != null && _client.Connected;

        /// <summary>
        /// 当客户端连接时触发
        /// </summary>
        public event Action ClientConnected;

        /// <summary>
        /// 当客户端断开时触发
        /// </summary>
        public event Action<string> ClientDisconnected;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ipAddress">IPv6 地址字符串</param>
        /// <param name="port">端口号</param>
        public GameServer(string ipAddress, int port)
        {
            IPAddress addr = IPAddress.Parse(ipAddress);
            _listener = new TcpListener(addr, port);
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        /// <summary>
        /// 启动服务器（非阻塞）
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            _cts = new CancellationTokenSource();
            _listener.Start();
            _isRunning = true;

            // 在后台线程接受客户端连接
            Task.Run(async () =>
            {
                try
                {
                    _client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _client.NoDelay = true;
                    _clientStream = _client.GetStream();
                    _clientConnected = true;
                    ClientConnected?.Invoke();

                    // 启动接收和发送循环
                    _ = Task.Run(() => ReceiveLoop(_cts.Token));
                    _ = Task.Run(() => SendLoop(_cts.Token));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    ClientDisconnected?.Invoke($"服务器启动失败: {ex.Message}");
                    Stop();
                }
            });
        }

        /// <summary>
        /// 发送消息给客户端
        /// </summary>
        public void SendToClient(NetworkMessage message)
        {
            if (!HasClientConnected)
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
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _clientConnected = false;
            _cts?.Cancel();

            try
            {
                _clientStream?.Close();
                _clientStream?.Dispose();
                _client?.Close();
                _client?.Dispose();
                _listener?.Stop();
            }
            catch { }

            _clientStream = null;
            _client = null;
            _listener = null;
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _client != null && _client.Connected)
                {
                    NetworkMessage message = await Task.Run(() =>
                    {
                        try
                        {
                            return NetworkMessage.ReadFromStream(_clientStream);
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

            if (_isRunning)
            {
                _isRunning = false;
                _clientConnected = false;
                ClientDisconnected?.Invoke("对手已断开连接");
            }
        }

        private async Task SendLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _client != null && _client.Connected)
                {
                    if (_outgoingMessages.TryDequeue(out NetworkMessage message))
                    {
                        byte[] data = message.Serialize();
                        await _clientStream.WriteAsync(data, 0, data.Length, ct);
                        await _clientStream.FlushAsync(ct);
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
            Stop();
            _cts?.Dispose();
        }
    }
}