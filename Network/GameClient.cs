using System;
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
    /// TCP 游戏客户端，使用 IPv6 地址连接服务器
    /// </summary>
    public class GameClient : MessageTransportBase
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private bool _isConnected;

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

        public GameClient() : base("Connection to the server was lost.")
        {
        }

        protected override TcpClient Connection => _tcpClient;

        protected override NetworkStream DataStream => _stream;

        /// <summary>
        /// 连接到指定地址和端口的服务器（非阻塞）。
        /// relay 模式：发 'C' + 0x01 + 4 位房间码，等 relay 回 0x03 才视为连接成功。
        /// 直连模式：roleByte=0 且无房间码，行为与旧版一致。
        /// </summary>
        /// <param name="ipAddress">地址字符串</param>
        /// <param name="port">端口号</param>
        /// <param name="roleByte">可选，relay 角色字节（0 = 无角色）</param>
        /// <param name="roomCode">可选，4 位房间码（relay 模式必填）</param>
        public void Connect(string ipAddress, int port, byte roleByte = 0, string roomCode = null)
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
                        if (string.IsNullOrEmpty(roomCode))
                            throw new IOException("Relay mode requires a four-digit room code.");

                        // 发 'C' + 0x01 + 4 位 ASCII 房间码（大写归一化）
                        string code = roomCode.Trim().ToUpperInvariant();
                        if (code.Length != 4)
                            throw new IOException("Invalid room code. Enter four digits.");

                        var payload = new byte[6];
                        payload[0] = roleByte;
                        payload[1] = 0x01;
                        Encoding.ASCII.GetBytes(code, 0, 4, payload, 2);

                        await _stream.WriteAsync(payload, 0, payload.Length, _cts.Token);
                        await _stream.FlushAsync(_cts.Token);

                        // relay 回 0x03 = join OK；错误码/房间不存在 → relay 直接断开
                        int oldTimeout = _tcpClient.ReceiveTimeout;
                        _tcpClient.ReceiveTimeout = 10_000;
                        var ack = new byte[] { 0 };
                        int n = _stream.Read(ack, 0, 1);
                        _tcpClient.ReceiveTimeout = oldTimeout;
                        if (n <= 0 || ack[0] != 0x03)
                            throw new IOException("The room code is invalid or the room is full.");
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
                    Disconnected?.Invoke($"Connection failed: {ex.Message}");
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
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _isConnected = false;
            TearDownTransport();

            _stream = null;
            _tcpClient = null;
        }

        protected override void NotifyDisconnected(string message)
        {
            if (_isConnected)
            {
                _isConnected = false;
                Disconnected?.Invoke(message);
            }
        }

        protected override void OnStop() => Disconnect();
    }
}
