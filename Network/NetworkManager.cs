#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Fida.Logic;
using Fida.Campaign;

namespace Fida.Network
{
    /// <summary>
    /// 封装所有联机网络逻辑：服务器/客户端生命周期、消息收发、状态管理。
    /// 支持大厅（Lobby）模式：连接保持后进入大厅，由主机选择游戏模式。
    /// </summary>
    public class NetworkManager : IDisposable
    {
        private GameServer? _gameServer;
        private GameClient? _gameClient;
        private GameRelayHost? _relayHost;
        private readonly ServerConfig _serverConfig;
        private bool _isHost;
        private bool _disposed;
        private readonly Queue<NetworkMessage> _pendingMessages = new();
        private CampaignPresence? _localCampaignPresence;

        /// <summary>当前是否为主机</summary>
        public bool IsHost => _isHost;

        /// <summary>是否已建立网络连接（主机有客户端连入，或客机已连上服务器）</summary>
        public bool IsConnected =>
            (_isHost && _gameServer != null && _gameServer.HasClientConnected) ||
            (_isHost && _relayHost != null && _relayHost.HasClientConnected) ||
            (!_isHost && _gameClient != null && _gameClient.IsConnected);

        /// <summary>当前状态文本（用于 UI 显示）</summary>
        public string StatusText { get; private set; } = string.Empty;
        public void ReportStatus(string message) => StatusText = message ?? string.Empty;

        /// <summary>主机端：relay 分配的房间码（客机用此码加入）</summary>
        public string HostRoomCode => _relayHost?.RoomCode ?? string.Empty;

        /// <summary>主机端：客户端连入时触发</summary>
        public event System.Action? ClientConnected;

        /// <summary>客机端：成功连接到服务器时触发</summary>
        public event System.Action? ConnectedToServer;

        /// <summary>连接断开时触发</summary>
        public event System.Action<string>? Disconnected;

        /// <summary>收到对方大厅投票时触发；双方拥有同等模式选择权。</summary>
        public event System.Action<int>? LobbyVoteReceived;
        public event System.Action<int, SkillType>? DraftBanReceived;
        public event System.Action<int, IReadOnlyList<SkillType>>? DraftLockReceived;
        public event System.Action<IReadOnlyList<SkillType>, IReadOnlyList<SkillType>>? DraftStarting;
        public event Action<string>? BattleJoinRequested;
        public event Action<BattleSnapshot>? BattleSnapshotReceived;
        public event Action<string>? BattleSnapshotAcknowledged;
        public event Action<CampaignPresence>? CampaignPresenceReceived;
        public event Action<string, string>? CampaignCoopStarting;
        public BattleSnapshot PendingBattleSnapshot { get; private set; } = null!;
        public BattleSnapshot TakePendingBattleSnapshot()
        {
            var snapshot = PendingBattleSnapshot;
            PendingBattleSnapshot = null!;
            return snapshot;
        }
        public CampaignPresence RemoteCampaignPresence { get; private set; } = new(1, CampaignOnlinePhase.Map, "", false);
        public bool IsCampaignSharedBattle { get; set; }
        public bool IsCampaignBattleContinuingLocally { get; set; }
        /// <summary>Host-issued shared session token for a simultaneous co-op campaign start.</summary>
        public string CampaignSessionId { get; set; } = string.Empty;

        /// <summary>当前模式：0=Duel，1=Co-op，2=Campaign Co-op。</summary>
        public int CurrentGameMode { get; set; } = 0;

        /// <summary>本地大厅投票（-1=未选，0=Duel，1=Co-op，2=Campaign Co-op）。</summary>
        public int LocalLobbyVote { get; set; } = -1;
        /// <summary>对方大厅投票（-1=未选）</summary>
        public int RemoteLobbyVote { get; private set; } = -1;

        public void ResetLobbyVotes()
        {
            LocalLobbyVote = -1;
            RemoteLobbyVote = -1;
        }

        /// <summary>双方投票状态文案（-1=未选）</summary>
        public static string GetLobbyVoteStatusText(int local, int remote)
        {
            static string Name(int mode) => mode switch
            {
                0 => "DUEL", 1 => "CO-OP", 2 => "CAMPAIGN CO-OP", _ => "NONE"
            };
            if (local == -1 && remote == -1) return "CHOOSE A MODE";
            if (local == -1) return $"ALLY CHOSE: {Name(remote)} - YOUR TURN";
            if (remote == -1) return $"VOTED: {Name(local)} - WAITING FOR ALLY";
            if (local == remote) return $"MATCH: {Name(local)} - STARTING...";
            return $"VOTE MISMATCH - CHOOSE A MODE";
        }

        public void SendCampaignPresence(CampaignPresence presence)
        {
            if (CurrentGameMode == 2)
                presence = presence with
                {
                    CompletedNodes = CampaignRuntime.OnlineProgress.CompletedNodes
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray()
                };
            _localCampaignPresence = presence;
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
            var completedNodes = presence.CompletedNodes ?? Array.Empty<string>();
            writer.Write(completedNodes.Count);
            foreach (var nodeId in completedNodes) writer.Write(nodeId ?? string.Empty);
            SendMessage(new NetworkMessage(MessageType.CampaignPresence, stream.ToArray()));
        }

        /// <summary>Re-sends the current activity along with the now-merged online progress.</summary>
        public void RepublishCampaignPresence()
        {
            if (_localCampaignPresence is { } presence) SendCampaignPresence(presence);
        }

        public void SendCampaignReady(string nodeId) => SendMessage(
            new NetworkMessage(MessageType.CampaignReady, WriteStringPayload(nodeId)));

        public void SendCampaignCoopStart(string nodeId, string sessionId)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(nodeId ?? string.Empty);
            writer.Write(sessionId ?? string.Empty);
            SendMessage(new NetworkMessage(MessageType.CampaignCoopStart, stream.ToArray()));
        }

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
                    if (message.Type == MessageType.GameAction && CurrentGameMode == 2)
                    {
                        // Actions from another independent route must never be replayed
                        // into this player's later battle.
                        continue;
                    }
                    if (message.Type == MessageType.BattleSnapshot)
                    {
                        PendingBattleSnapshot = BattleSnapshot.Deserialize(message.Payload);
                        BattleSnapshotReceived?.Invoke(PendingBattleSnapshot);
                        continue;
                    }
                    if (!HandleCampaignProtocolMessage(message))
                        unhandled.Add(message);
                }
                catch (Exception ex) { StatusText = $"Invalid Campaign message: {ex.Message}"; }
            }
            RetainMessages(unhandled);
        }

        /// <summary>
        /// Shared campaign-protocol dispatch used by both <see cref="ProcessCampaignMessages"/> and
        /// <see cref="ProcessMessages"/>. Each handler body is written once here. Returns true when the
        /// message was handled; false when it belongs to a different protocol and should be retained.
        /// </summary>
        private bool HandleCampaignProtocolMessage(NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.CampaignPresence:
                {
                    using var stream = new MemoryStream(message.Payload);
                    using var reader = new BinaryReader(stream);
                    var presence = ReadCampaignPresence(reader, stream);
                    RemoteCampaignPresence = presence;
                    CampaignPresenceReceived?.Invoke(presence);
                    return true;
                }
                case MessageType.CampaignReady:
                {
                    string nodeId = ReadStringPayload(message.Payload);
                    RemoteCampaignPresence = RemoteCampaignPresence with
                    { Phase = CampaignOnlinePhase.Loadout, NodeId = nodeId, Ready = true };
                    CampaignPresenceReceived?.Invoke(RemoteCampaignPresence);
                    return true;
                }
                case MessageType.CampaignCoopStart:
                {
                    var start = ReadCampaignCoopStart(message.Payload);
                    CampaignSessionId = start.SessionId;
                    CampaignCoopStarting?.Invoke(start.NodeId, start.SessionId);
                    return true;
                }
                case MessageType.BattleSnapshotAck:
                    BattleSnapshotAcknowledged?.Invoke(ReadStringPayload(message.Payload));
                    return true;
                default:
                    return false;
            }
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
        public void StartConnecting(string ipAddress, int port, bool isRelay = false, string? roomCode = null)
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
        /// 处理大厅消息（双方对称：任一投票消息都视为对方的一次投票）
        /// </summary>
        public void ProcessLobbyMessages()
        {
            var messages = TakePendingMessages();
            var unhandled = new List<NetworkMessage>();

            foreach (var msg in messages)
            {
                // 三种投票消息（请求/响应/结果）语义相同：payload 为对方所选模式。
                switch (msg.Type)
                {
                    case MessageType.LobbyStartGameRequest:
                    case MessageType.LobbyStartGameResponse:
                    case MessageType.LobbyStartGameResult:
                        if (msg.Payload.Length >= 4)
                        {
                            int mode = BitConverter.ToInt32(msg.Payload, 0);
                            RemoteLobbyVote = mode;
                            LobbyVoteReceived?.Invoke(mode);
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

        private List<NetworkMessage>? ReceiveAvailableMessages()
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

        private static CampaignPresence ReadCampaignPresence(BinaryReader reader, MemoryStream stream)
        {
            int playerId = reader.ReadInt32();
            var phase = (CampaignOnlinePhase)reader.ReadByte();
            string nodeId = reader.ReadString();
            bool ready = reader.ReadBoolean();
            var skills = ReadSkillList(reader, stream);
            string sessionId = reader.ReadString();
            var completedNodes = new List<string>();
            if (stream.Position + sizeof(int) <= stream.Length)
            {
                int count = reader.ReadInt32();
                if (count < 0 || count > 4096)
                    throw new InvalidDataException("Invalid campaign progress count.");
                for (int i = 0; i < count; i++) completedNodes.Add(reader.ReadString());
            }
            if (stream.Position != stream.Length)
                throw new InvalidDataException("Unexpected campaign presence data.");
            return new CampaignPresence(playerId, phase, nodeId, ready, skills, sessionId, completedNodes);
        }

        /// <summary>
        /// 发送大厅模式投票（双方对称：主机经 server/relay 下发，客机经 client 上行）
        /// </summary>
        public void SendLobbyVote(int mode)
        {
            var payload = BitConverter.GetBytes(mode);
            var message = new NetworkMessage(MessageType.LobbyStartGameRequest, payload);

            if (_isHost && _gameServer != null)
                _gameServer.SendToClient(message);
            else if (_isHost && _relayHost != null)
                _relayHost.SendToClient(message);
            else if (!_isHost && _gameClient != null)
                _gameClient.SendMessage(message);
        }

        /// <summary>接收远程消息并应用到 GameManager</summary>
        public void ProcessMessages(GameManager gameManager)
        {
            var messages = TakePendingMessages();
            var unhandled = new List<NetworkMessage>();

            foreach (var msg in messages)
            {
                try
                {
                    if (msg.Type == MessageType.GameAction)
                    {
                        if (CurrentGameMode == 2 && !IsCampaignSharedBattle) continue;
                        var action = NetworkMessage.DeserializeAction(msg.Payload);
                        gameManager.UpdateGame(action, GameActionOrigin.RemoteNetwork);
                        continue;
                    }
                    if (msg.Type == MessageType.BattleJoinRequest)
                    {
                        BattleJoinRequested?.Invoke(ReadStringPayload(msg.Payload));
                        continue;
                    }
                    if (msg.Type == MessageType.BattleSnapshot)
                    {
                        BattleSnapshotReceived?.Invoke(BattleSnapshot.Deserialize(msg.Payload));
                        continue;
                    }
                    if (!HandleCampaignProtocolMessage(msg))
                        unhandled.Add(msg);
                }
                catch (Exception ex) { StatusText = $"Invalid message: {ex.Message}"; }
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

        private static (string NodeId, string SessionId) ReadCampaignCoopStart(byte[] payload)
        {
            using var stream = new MemoryStream(payload ?? []);
            using var reader = new BinaryReader(stream);
            string nodeId = reader.ReadString();
            string sessionId = reader.ReadString();
            if (stream.Position != stream.Length)
                throw new InvalidDataException("Unexpected trailing co-op start data.");
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new InvalidDataException("Co-op start is missing its shared session ID.");
            return (nodeId, sessionId);
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
            CampaignSessionId = string.Empty;
            _localCampaignPresence = null;
            IsCampaignSharedBattle = false;
            IsCampaignBattleContinuingLocally = false;
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
