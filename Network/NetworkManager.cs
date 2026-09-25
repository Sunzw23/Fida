using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
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
        private readonly Queue<NetworkMessage> _pendingMessages = new();

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
        public event System.Action<int, SkillType> DraftBanReceived;
        public event System.Action<int, IReadOnlyList<SkillType>> DraftLockReceived;
        public event System.Action<IReadOnlyList<SkillType>, IReadOnlyList<SkillType>> DraftStarting;
        public event Action<string> BattleJoinRequested;
        public event Action<BattleSnapshot> BattleSnapshotReceived;
        public event Action<string> BattleSnapshotAcknowledged;
        public event Action<CampaignPresence> CampaignPresenceReceived;
        public event Action<string> CampaignCoopStarting;
        public BattleSnapshot PendingBattleSnapshot { get; private set; }
        public BattleSnapshot TakePendingBattleSnapshot()
        {
            var snapshot = PendingBattleSnapshot;
            PendingBattleSnapshot = null;
            return snapshot;
        }
        public CampaignPresence RemoteCampaignPresence { get; private set; } = new(1, CampaignOnlinePhase.Map, "", false);
        public bool IsCampaignSharedBattle { get; set; }

        /// <summary>当前游戏模式（0=PvP, 2=Campaign Co-op），在游戏开始时设置</summary>
        public int CurrentGameMode { get; set; } = 0;

        public void SendCampaignPresence(CampaignPresence presence)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(presence.PlayerId);
            writer.Write((byte)presence.Phase);
            writer.Write(presence.NodeId ?? string.Empty);
            writer.Write(presence.Ready);
            var skills = presence.Skills ?? Array.Empty<SkillType>();
            writer.Write(skills.Count);
            foreach (var skill in skills) writer.Write((int)skill);
            writer.Write(presence.SessionId ?? string.Empty);
            SendMessage(new NetworkMessage(MessageType.CampaignPresence, stream.ToArray()));
        }

        public void SendCampaignReady(string nodeId) => SendMessage(
            new NetworkMessage(MessageType.CampaignReady, WriteStringPayload(nodeId)));

        public void SendCampaignCoopStart(string nodeId) => SendMessage(
            new NetworkMessage(MessageType.CampaignCoopStart, WriteStringPayload(nodeId)));

        /// <summary>Drains map/loadout protocol messages. Call only outside battle action processing.</summary>
        public void ProcessCampaignMessages()
        {
            var messages = TakePendingMessages();
            if (messages.Count == 0) return;
            var unhandled = new List<NetworkMessage>();
            foreach (var message in messages)
            {
                try
                {
                    if (message.Type == MessageType.CampaignPresence)
                    {
                        using var stream = new MemoryStream(message.Payload);
                        using var reader = new BinaryReader(stream);
                        var presence = new CampaignPresence(reader.ReadInt32(), (CampaignOnlinePhase)reader.ReadByte(),
                            reader.ReadString(), reader.ReadBoolean(),
                            ReadSkillList(reader, stream), reader.ReadString());
                        RemoteCampaignPresence = presence;
                        CampaignPresenceReceived?.Invoke(presence);
                    }
                    else if (message.Type == MessageType.CampaignReady)
                    {
                        string nodeId = ReadStringPayload(message.Payload);
                        RemoteCampaignPresence = RemoteCampaignPresence with
                        { Phase = CampaignOnlinePhase.Loadout, NodeId = nodeId, Ready = true };
                        CampaignPresenceReceived?.Invoke(RemoteCampaignPresence);
                    }
                    else if (message.Type == MessageType.CampaignCoopStart)
                    {
                        CampaignCoopStarting?.Invoke(ReadStringPayload(message.Payload));
                    }
                    else if (message.Type == MessageType.GameAction && CurrentGameMode == 2)
                    {
                        // Actions from another independent route must never be replayed
                        // into this player's later battle.
                    }
                    else if (message.Type == MessageType.BattleSnapshot)
                    {
                        PendingBattleSnapshot = BattleSnapshot.Deserialize(message.Payload);
                        BattleSnapshotReceived?.Invoke(PendingBattleSnapshot);
                    }
                    else if (message.Type == MessageType.BattleSnapshotAck)
                        BattleSnapshotAcknowledged?.Invoke(ReadStringPayload(message.Payload));
                    else unhandled.Add(message);
                }
                catch (Exception ex) { StatusText = $"Invalid Campaign message: {ex.Message}"; }
            }
            RetainMessages(unhandled);
        }

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
                StatusText = $"Waiting for opponent...\nrelay: {ipAddress}:{port}";
                return;
            }

            _gameServer = new GameServer(ipAddress, port);
            _gameServer.ClientDisconnected += (msg) => Disconnected?.Invoke(msg);
            _gameServer.Start();

            StatusText = $"Waiting for opponent...\naddress: [{ipAddress}]:{port}";
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
                ? $"Connecting via relay {ipAddress}:{port} (room code: {roomCode})"
                : $"Connecting to [{ipAddress}]:{port} ...";
        }

        /// <summary>主机每帧调用：检查是否有客户端连入</summary>
        public bool CheckHostConnection()
        {
            if ((_isHost && _gameServer != null && _gameServer.HasClientConnected) ||
                (_isHost && _relayHost != null && _relayHost.HasClientConnected))
            {
                StatusText = "Opponent connected! Entering lobby...";
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
            var messages = TakePendingMessages();
            var unhandled = new List<NetworkMessage>();

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
                    default: unhandled.Add(msg); break;
                }
            }
            RetainMessages(unhandled);
        }

        public void ProcessDraftMessages()
        {
            var messages = TakePendingMessages();
            var unhandled = new List<NetworkMessage>();
            if (messages.Count == 0) return;
            foreach (var msg in messages)
            {
                using var stream = new MemoryStream(msg.Payload ?? []);
                using var reader = new BinaryReader(stream);
                if (msg.Type == MessageType.DraftBan && stream.Length >= 8)
                    DraftBanReceived?.Invoke(reader.ReadInt32(), (SkillType)reader.ReadInt32());
                else if (msg.Type == MessageType.DraftLock && stream.Length >= 8)
                {
                    int playerId = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    var skills = new List<SkillType>();
                    for (int i = 0; i < count && stream.Position + 4 <= stream.Length; i++)
                        skills.Add((SkillType)reader.ReadInt32());
                    DraftLockReceived?.Invoke(playerId, skills);
                }
                else if (msg.Type == MessageType.DraftStart && stream.Length >= 8)
                {
                    var first = ReadSkillList(reader, stream);
                    var second = ReadSkillList(reader, stream);
                    DraftStarting?.Invoke(first, second);
                }
                else unhandled.Add(msg);
            }
            RetainMessages(unhandled);
        }

        public void SendDraftBan(int playerId, SkillType skill) =>
            SendMessage(BuildDraftMessage(MessageType.DraftBan, writer =>
            {
                writer.Write(playerId);
                writer.Write((int)skill);
            }));

        public void SendDraftLock(int playerId, IReadOnlyCollection<SkillType> skills) =>
            SendMessage(BuildDraftMessage(MessageType.DraftLock, writer =>
            {
                writer.Write(playerId);
                WriteSkillList(writer, skills);
            }));

        public void SendDraftStart(IReadOnlyCollection<SkillType> first,
            IReadOnlyCollection<SkillType> second) =>
            SendMessage(BuildDraftMessage(MessageType.DraftStart, writer =>
            {
                WriteSkillList(writer, first);
                WriteSkillList(writer, second);
            }));

        private List<NetworkMessage> ReceiveAvailableMessages()
        {
            if (_isHost && _gameServer != null) return _gameServer.ReceiveMessages();
            if (_isHost && _relayHost != null) return _relayHost.ReceiveMessages();
            if (!_isHost && _gameClient != null) return _gameClient.ReceiveMessages();
            return null;
        }

        private List<NetworkMessage> TakePendingMessages()
        {
            var incoming = ReceiveAvailableMessages();
            var result = new List<NetworkMessage>(_pendingMessages.Count + (incoming?.Count ?? 0));
            while (_pendingMessages.Count > 0) result.Add(_pendingMessages.Dequeue());
            if (incoming != null) result.AddRange(incoming);
            return result;
        }

        private void RetainMessages(IEnumerable<NetworkMessage> messages)
        {
            foreach (var message in messages) _pendingMessages.Enqueue(message);
        }

        private void SendMessage(NetworkMessage message)
        {
            if (_isHost && _gameServer != null) _gameServer.SendToClient(message);
            else if (_isHost && _relayHost != null) _relayHost.SendToClient(message);
            else if (!_isHost && _gameClient != null) _gameClient.SendMessage(message);
        }

        private static NetworkMessage BuildDraftMessage(MessageType type, Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return new NetworkMessage(type, stream.ToArray());
        }

        private static void WriteSkillList(BinaryWriter writer, IEnumerable<SkillType> skills)
        {
            var list = (skills ?? []).ToList();
            writer.Write(list.Count);
            foreach (var skill in list) writer.Write((int)skill);
        }

        private static IReadOnlyList<SkillType> ReadSkillList(BinaryReader reader, Stream stream)
        {
            if (stream.Position + 4 > stream.Length) return [];
            int count = reader.ReadInt32();
            var result = new List<SkillType>();
            for (int i = 0; i < count && stream.Position + 4 <= stream.Length; i++)
                result.Add((SkillType)reader.ReadInt32());
            return result;
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
            var messages = TakePendingMessages();
            var unhandled = new List<NetworkMessage>();

            foreach (var msg in messages)
            {
                if (msg.Type == MessageType.GameAction)
                {
                    if (CurrentGameMode == 2 && !IsCampaignSharedBattle) continue;
                    var action = NetworkMessage.DeserializeAction(msg.Payload);
                    gameManager.UpdateGame(action);
                }
                else if (msg.Type == MessageType.BattleJoinRequest)
                {
                    BattleJoinRequested?.Invoke(ReadStringPayload(msg.Payload));
                }
                else if (msg.Type == MessageType.BattleSnapshot)
                {
                    try { BattleSnapshotReceived?.Invoke(BattleSnapshot.Deserialize(msg.Payload)); }
                    catch (Exception ex) { StatusText = $"Invalid battle snapshot: {ex.Message}"; }
                }
                else if (msg.Type == MessageType.BattleSnapshotAck)
                {
                    BattleSnapshotAcknowledged?.Invoke(ReadStringPayload(msg.Payload));
                }
                else if (msg.Type == MessageType.CampaignPresence)
                {
                    using var stream = new MemoryStream(msg.Payload);
                    using var reader = new BinaryReader(stream);
                    var presence = new CampaignPresence(reader.ReadInt32(), (CampaignOnlinePhase)reader.ReadByte(),
                        reader.ReadString(), reader.ReadBoolean(), ReadSkillList(reader, stream), reader.ReadString());
                    RemoteCampaignPresence = presence;
                    CampaignPresenceReceived?.Invoke(presence);
                }
                else if (msg.Type == MessageType.CampaignReady)
                {
                    string nodeId = ReadStringPayload(msg.Payload);
                    RemoteCampaignPresence = RemoteCampaignPresence with
                    { Phase = CampaignOnlinePhase.Loadout, NodeId = nodeId, Ready = true };
                    CampaignPresenceReceived?.Invoke(RemoteCampaignPresence);
                }
                else if (msg.Type == MessageType.CampaignCoopStart)
                    CampaignCoopStarting?.Invoke(ReadStringPayload(msg.Payload));
                else unhandled.Add(msg);
            }
            RetainMessages(unhandled);
        }

        /// <summary>Requests a direct battle-state transfer; no historical actions are sent.</summary>
        public void RequestBattleJoin(string sessionId) => SendMessage(
            new NetworkMessage(MessageType.BattleJoinRequest, WriteStringPayload(sessionId)));

        /// <summary>Sends a point-in-time state checkpoint for a reinforcement join.</summary>
        public void SendBattleSnapshot(BattleSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            SendMessage(new NetworkMessage(MessageType.BattleSnapshot, snapshot.Serialize()));
        }

        public void AcknowledgeBattleSnapshot(string sessionId) => SendMessage(
            new NetworkMessage(MessageType.BattleSnapshotAck, WriteStringPayload(sessionId)));

        private static byte[] WriteStringPayload(string value)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(value ?? string.Empty);
            return stream.ToArray();
        }

        private static string ReadStringPayload(byte[] payload)
        {
            using var stream = new MemoryStream(payload ?? []);
            using var reader = new BinaryReader(stream);
            return reader.ReadString();
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
