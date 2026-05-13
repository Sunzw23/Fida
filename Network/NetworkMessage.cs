using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Fida.Logic;

namespace Fida.Network
{
    /// <summary>
    /// 网络消息类型枚举
    /// </summary>
    public enum MessageType : byte
    {
        ConnectRequest = 0x01,
        ConnectAccept = 0x02,
        ConnectReject = 0x03,
        GameStart = 0x10,
        TurnStart = 0x11,
        PlayerAction = 0x12,
        StateSync = 0x13,
        NumberChanged = 0x14,
        AttackExecuted = 0x15,
        HealExecuted = 0x16,
        GameEnd = 0x17,
        GameAction = 0x18,
        LobbyJoin = 0x30,
        LobbyUpdate = 0x31,
        LobbyStartGameRequest = 0x32,
        LobbyStartGameResponse = 0x33,
        LobbyStartGameResult = 0x34,
        LobbyLeave = 0x35,
        StartGamePvP = 0x36,
        StartGameCoop = 0x37,
        ChatMessage = 0x20,
        Disconnect = 0xFF,
    }

    /// <summary>
    /// 网络消息，包含消息类型和负载数据
    /// </summary>
    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        public NetworkMessage() { }

        public NetworkMessage(MessageType type)
        {
            Type = type;
        }

        public NetworkMessage(MessageType type, byte[] payload)
        {
            Type = type;
            Payload = payload;
        }

        /// <summary>
        /// 将消息序列化为字节数组
        /// 格式: [MessageType(1)] [PayloadLength(2)] [Payload(N)]
        /// </summary>
        public byte[] Serialize()
        {
            byte[] payload = Payload ?? Array.Empty<byte>();
            byte[] result = new byte[3 + payload.Length];
            result[0] = (byte)Type;
            result[1] = (byte)(payload.Length & 0xFF);
            result[2] = (byte)((payload.Length >> 8) & 0xFF);
            Array.Copy(payload, 0, result, 3, payload.Length);
            return result;
        }

        /// <summary>
        /// 从流中读取一条完整消息
        /// </summary>
        public static NetworkMessage ReadFromStream(Stream stream)
        {
            int typeByte = stream.ReadByte();
            if (typeByte < 0) return null;

            MessageType type = (MessageType)typeByte;

            byte[] lengthBytes = new byte[2];
            int read = stream.Read(lengthBytes, 0, 2);
            if (read < 2) return null;

            int payloadLength = lengthBytes[0] | (lengthBytes[1] << 8);
            byte[] payload = new byte[payloadLength];

            int totalRead = 0;
            while (totalRead < payloadLength)
            {
                read = stream.Read(payload, totalRead, payloadLength - totalRead);
                if (read <= 0) return null;
                totalRead += read;
            }

            return new NetworkMessage(type, payload);
        }

        /// <summary>
        /// 将 Logic.GameAction 序列化为字节数组
        /// </summary>
        public static byte[] SerializeAction(Logic.GameAction action)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write((byte)action.Type);
            writer.Write(action.TargetPlayerId);
            writer.Write(action.NumberIndex);
            writer.Write(action.Number);
            writer.Write(action.IsApply);
            return ms.ToArray();
        }

        /// <summary>
        /// 从字节数组反序列化为 Logic.GameAction
        /// </summary>
        public static Logic.GameAction DeserializeAction(byte[] payload)
        {
            using var ms = new MemoryStream(payload);
            using var reader = new BinaryReader(ms);
            return new Logic.GameAction
            {
                Type = (ActionType)reader.ReadByte(),
                TargetPlayerId = reader.ReadInt32(),
                NumberIndex = reader.ReadInt32(),
                Number = reader.ReadInt32(),
                IsApply = reader.ReadBoolean(),
            };
        }
    }
}