using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fida.Logic;

namespace Fida.Network
{
    /// <summary>
    /// 封装所有联机网络逻辑：服务器/客户端生命周期、消息收发、状态管理。
    /// </summary>
    public class NetworkManager : IDisposable
    {
        private GameServer _gameServer;
        private GameClient _gameClient;
        private readonly ServerConfig _serverConfig;
        private bool _isHost;
        private bool _disposed;

        /// <summary>当前是否为主机</summary>
        public bool IsHost => _isHost;

        /// <summary>是否已建立网络连接（主机有客户端连入，或客机已连上服务器）</summary>
        public bool IsConnected =>
            (_isHost && _gameServer != null && _gameServer.HasClientConnected) ||
            (!_isHost && _gameClient != null && _gameClient.IsConnected);

        /// <summary>当前状态文本（用于 UI 显示）</summary>
        public string StatusText { get; private set; } = string.Empty;

        /// <summary>主机端：客户端连入时触发</summary>
        public event System.Action ClientConnected;

        /// <summary>客机端：成功连接到服务器时触发</summary>
        public event System.Action ConnectedToServer;

        /// <summary>连接断开时触发</summary>
        public event System.Action<string> Disconnected;

        public NetworkManager()
        {
            _serverConfig = ServerConfig.Load("server.csv");
        }

        /// <summary>获取 CSV 中配置的服务器地址</summary>
        public string GetServerAddress() => _serverConfig?.GetServerAddress() ?? "::1";

        /// <summary>获取 CSV 中配置的端口</summary>
        public int GetServerPort() => _serverConfig?.GetServerPort() ?? 8888;

        /// <summary>启动主机模式：创建服务器并开始监听</summary>
        public void StartHosting(string ipAddress, int port)
        {
            _isHost = true;

            _gameServer = new GameServer(ipAddress, port);
            _gameServer.Start();

            StatusText = $"等待对手连接...\n地址: [{ipAddress}]:{port}";

            // 轮询客户端连接（由外部每帧调用 CheckHostConnection）
        }

        /// <summary>启动客机模式：连接到远程服务器</summary>
        public void StartConnecting(string ipAddress, int port)
        {
            _isHost = false;

            _gameClient = new GameClient();
            _gameClient.Connected += () => ConnectedToServer?.Invoke();
            _gameClient.Disconnected += (msg) => Disconnected?.Invoke(msg);
            _gameClient.Connect(ipAddress, port);

            StatusText = $"正在连接到 [{ipAddress}]:{port} ...";
        }

        /// <summary>主机每帧调用：检查是否有客户端连入</summary>
        public bool CheckHostConnection()
        {
            if (_isHost && _gameServer != null && _gameServer.HasClientConnected)
            {
                StatusText = "对手已连接！正在开始游戏...";
                ClientConnected?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>接收远程消息并应用到 GameManager</summary>
        public void ProcessMessages(GameManager gameManager)
        {
            List<NetworkMessage> messages = null;

            if (_isHost && _gameServer != null)
                messages = _gameServer.ReceiveMessages();
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