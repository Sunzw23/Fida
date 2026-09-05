using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fida.Logic;

namespace Fida.Network
{
    /// <summary>
    /// 封装所有联机网络逻辑：服务器/客户端生命周期、消息收发、状态管理。
    /// 支持大厅（Lobby）模式：连接保持后进入大厅，由主机选择游戏模式。
    /// </summary>
    public class NetworkManager : IDisposable
    {
        private GameServer _gameServer;
        private GameClient _gameClient;
        private GameRelayHost _relayHost;
        private readonly ServerConfig _serverConfig;
        private bool _isHost;
        private bool _disposed;

        /// <summary>当前是否为主机</summary>
        public bool IsHost => _isHost;

        /// <summary>是否已建立网络连接（主机有客户端连入，或客机已连上服务器）</summary>
        public bool IsConnected =>
            (_isHost && _gameServer != null && _gameServer.HasClientConnected) ||
            (_isHost && _relayHost != null && _relayHost.HasClientConnected) ||
            (!_isHost && _gameClient != null && _gameClient.IsConnected);

        /// <summary>当前状态文本（用于 UI 显示）</summary>
        public string StatusText { get; private set; } = string.Empty;

        /// <summary>主机端：relay 分配的房间码（客机用此码加入）</summary>
        public string HostRoomCode => _relayHost?.RoomCode ?? string.Empty;

        /// <summary>主机端：客户端连入时触发</summary>
        public event System.Action ClientConnected;

        /// <summary>客机端：成功连接到服务器时触发</summary>
        public event System.Action ConnectedToServer;

        /// <summary>连接断开时触发</summary>
        public event System.Action<string> Disconnected;

        /// <summary>收到大厅游戏模式请求时触发（客机端）</summary>
        public event System.Action<int> LobbyGameModeRequested;

        /// <summary>收到大厅游戏模式响应时触发（主机端）</summary>
        public event System.Action<int> LobbyGameModeAccepted;

        /// <summary>收到大厅游戏开始结果时触发（客机端）</summary>
        public event System.Action<int> LobbyGameStarting;

        /// <summary>当前游戏模式（0=PvP, 1=Co-op），在游戏开始时设置</summary>
        public int CurrentGameMode { get; set; } = 0;

        public NetworkManager()
        {
            _serverConfig = ServerConfig.Load("server.csv");
        }

        /// <summary>获取 CSV 中配置的服务器地址</summary>
        public string GetServerAddress() => _serverConfig?.GetServerAddress() ?? "::1";

        /// <summary>获取 CSV 中配置的端口</summary>
        public int GetServerPort() => _serverConfig?.GetServerPort() ?? 8888;

        /// <summary>获取 CSV 中第一行服务器是否为 relay</summary>
        public bool GetServerIsRelay() => _serverConfig?.GetServers()?.FirstOrDefault()?.IsRelay ?? false;

        /// <summary>启动主机模式：直连监听 或 连接 relay 作为主机端</summary>
        public void StartHosting(string ipAddress, int port, bool isRelay = false)
        {
            _isHost = true;

            if (isRelay)
            {
                _relayHost = new GameRelayHost();
                _relayHost.ClientDisconnected += (msg) => Disconnected?.Invoke(msg);
                _relayHost.Start(ipAddress, port);
                StatusText = $"等待对手连接...\nrelay: {ipAddress}:{port}";
                return;
            }

            _gameServer = new GameServer(ipAddress, port);
            _gameServer.ClientDisconnected += (msg) => Disconnected?.Invoke(msg);
            _gameServer.Start();

            StatusText = $"等待对手连接...\n地址: [{ipAddress}]:{port}";
        }

        /// <summary>启动客机模式：直连服务器 或 连接 relay 作为客机端</summary>
        public void StartConnecting(string ipAddress, int port, bool isRelay = false, string roomCode = null)
        {
            _isHost = false;

            _gameClient = new GameClient();
            _gameClient.Connected += () => ConnectedToServer?.Invoke();
            _gameClient.Disconnected += (msg) => Disconnected?.Invoke(msg);
            _gameClient.Connect(ipAddress, port, isRelay ? (byte)'C' : (byte)0, roomCode);

            StatusText = isRelay
                ? $"正在通过 relay 连接 {ipAddress}:{port} (房间码: {roomCode})"
                : $"正在连接到 [{ipAddress}]:{port} ...";
        }

        /// <summary>主机每帧调用：检查是否有客户端连入</summary>
        public bool CheckHostConnection()
        {
            if ((_isHost && _gameServer != null && _gameServer.HasClientConnected) ||
                (_isHost && _relayHost != null && _relayHost.HasClientConnected))
            {
                StatusText = "对手已连接！正在进入大厅...";
                ClientConnected?.Invoke();
                return true;
            }
            return false;
        }


        /// <summary>
        /// 处理大厅消息（在 OnlineLobby 状态时每帧调用）
        /// </summary>
        public void ProcessLobbyMessages()
        {
            List<NetworkMessage> messages = null;

            if (_isHost && _gameServer != null)
                messages = _gameServer.ReceiveMessages();
            else if (_isHost && _relayHost != null)
                messages = _relayHost.ReceiveMessages();
            else if (!_isHost && _gameClient != null)
                messages = _gameClient.ReceiveMessages();


            if (messages == null)
                return;

            foreach (var msg in messages)
            {
                switch (msg.Type)
                {
                    case MessageType.LobbyStartGameResponse:
                        // 客机接受了游戏模式请求
                        if (_isHost && msg.Payload.Length >= 4)
                        {
                            int mode = BitConverter.ToInt32(msg.Payload, 0);
                            LobbyGameModeAccepted?.Invoke(mode);
                        }
                        break;

                    case MessageType.LobbyStartGameRequest:
                        // 主机发来了游戏模式请求
                        if (!_isHost && msg.Payload.Length >= 4)
                        {
                            int mode = BitConverter.ToInt32(msg.Payload, 0);
                            LobbyGameModeRequested?.Invoke(mode);
                        }
                        break;

                    case MessageType.LobbyStartGameResult:
                        // 主机确认游戏开始
                        if (!_isHost && msg.Payload.Length >= 4)
                        {
                            int mode = BitConverter.ToInt32(msg.Payload, 0);
                            LobbyGameStarting?.Invoke(mode);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 主机发送游戏模式请求给客机
        /// </summary>
        public void SendLobbyGameModeRequest(int mode)
        {
            var payload = BitConverter.GetBytes(mode);
            var message = new NetworkMessage(MessageType.LobbyStartGameRequest, payload);

            if (_isHost && _gameServer != null)
                _gameServer.SendToClient(message);
            else if (_isHost && _relayHost != null)
                _relayHost.SendToClient(message);

        }

        /// <summary>
        /// 客机发送游戏模式接受响应给主机
        /// </summary>
        public void SendLobbyGameModeAccept(int mode)
        {
            var payload = BitConverter.GetBytes(mode);
            var message = new NetworkMessage(MessageType.LobbyStartGameResponse, payload);

            if (!_isHost && _gameClient != null)
                _gameClient.SendMessage(message);
        }

        /// <summary>
        /// 主机发送游戏开始确认给客机
        /// </summary>
        public void SendLobbyGameStartResult(int mode)
        {
            var payload = BitConverter.GetBytes(mode);
            var message = new NetworkMessage(MessageType.LobbyStartGameResult, payload);

            if (_isHost && _gameServer != null)
                _gameServer.SendToClient(message);
            else if (_isHost && _relayHost != null)
                _relayHost.SendToClient(message);

        }

        /// <summary>接收远程消息并应用到 GameManager</summary>
        public void ProcessMessages(GameManager gameManager)
        {
            List<NetworkMessage> messages = null;

            if (_isHost && _gameServer != null)
                messages = _gameServer.ReceiveMessages();
            else if (_isHost && _relayHost != null)
                messages = _relayHost.ReceiveMessages();
            else if (!_isHost && _gameClient != null)
                messages = _gameClient.ReceiveMessages();


            if (messages == null)
                return;

            foreach (var msg in messages)
            {
                if (msg.Type == MessageType.GameAction)
                {
                    var action = NetworkMessage.DeserializeAction(msg.Payload);
                    gameManager.UpdateGame(action);
                }
            }
        }

        /// <summary>将本地 GameAction 发送给远程玩家</summary>
        public void SendAction(Fida.Logic.GameAction action)
        {
            var payload = NetworkMessage.SerializeAction(action);
            var message = new NetworkMessage(MessageType.GameAction, payload);

            if (_isHost && _gameServer != null)
                _gameServer.SendToClient(message);
            else if (_isHost && _relayHost != null)
                _relayHost.SendToClient(message);
            else if (!_isHost && _gameClient != null)
                _gameClient.SendMessage(message);

        }

        /// <summary>获取本地玩家 ID（主机=0，客机=1）</summary>
        public int GetLocalPlayerId() => _isHost ? 0 : 1;

        /// <summary>清理所有网络资源</summary>
        public void Cleanup()
        {
            _gameServer?.Stop();
            _gameServer = null;

            _gameClient?.Disconnect();
            _gameClient = null;

            _relayHost?.Stop();
            _relayHost = null;

            _isHost = false;
            StatusText = string.Empty;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Cleanup();
        }
    }
}