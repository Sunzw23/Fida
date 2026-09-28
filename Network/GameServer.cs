using System;
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
    public class GameServer : MessageTransportBase
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _clientStream;
        private bool _isRunning;
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
        public event System.Action ClientConnected;

        /// <summary>
        /// 当客户端断开时触发
        /// </summary>
        public event Action<string> ClientDisconnected;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ipAddress">IPv6 地址字符串</param>
        /// <param name="port">端口号</param>
        public GameServer(string ipAddress, int port) : base("The opponent disconnected.")
        {
            IPAddress addr = IPAddress.Parse(ipAddress);
            _listener = new TcpListener(addr, port);
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        protected override TcpClient Connection => _client;

        protected override NetworkStream DataStream => _clientStream;

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
                    ClientDisconnected?.Invoke($"Failed to start the server: {ex.Message}");
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
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _clientConnected = false;
            TearDownTransport();

            _listener?.Stop();
            _clientStream = null;
            _client = null;
            _listener = null;
        }

        protected override void NotifyDisconnected(string message)
        {
            if (_isRunning)
            {
                _isRunning = false;
                _clientConnected = false;
                ClientDisconnected?.Invoke(message);
            }
        }

        protected override void OnStop() => Stop();
    }
}
