using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fida.Logic;

namespace Fida.Network
{
    /// <summary>
    /// A turn-boundary checkpoint for direct reinforcement joins. It contains the live
    /// battle state; it deliberately does not contain an action log or presentation history.
    /// </summary>
    public sealed class BattleSnapshot
    {
        public string SessionId { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public int CurrentPlayerId { get; set; }
        public int JoiningPlayerId { get; set; } = -1;
        public int BattleOwnerPlayerId { get; set; } = -1;
        public int TurnNumber { get; set; }
        public bool IsGameEnded { get; set; }
        public bool AtFirstNumberChoice { get; set; }
        public List<BattlePlayerSnapshot> Players { get; set; } = [];
        public List<BattleGroupSnapshot> Groups { get; set; } = [];

        public static BattleSnapshot Capture(GameManager game, string sessionId, string nodeId = "")
        {
            ArgumentNullException.ThrowIfNull(game);
            return new BattleSnapshot
            {
                SessionId = sessionId ?? string.Empty,
                NodeId = nodeId ?? string.Empty,
                CurrentPlayerId = game.GetCurrentPlayer().PlayerId,
                TurnNumber = game.TurnNumber,
                IsGameEnded = game.IsGameEnded,
                AtFirstNumberChoice = game.CurrentActionType == ActionType.ChooseNumberIndex &&
                    game.Query.GetFirstChosenNumberIndex() < 0,
                Players = game.Players.Select(BattlePlayerSnapshot.FromPlayer).ToList(),
                Groups = game.Groups.Select(group => new BattleGroupSnapshot
                {
                    GroupId = group.GroupId,
                    Name = group.Name,
                    PlayerIds = group.Players.Select(player => player.PlayerId).ToList()
                }).ToList()
            };
        }

        public byte[] Serialize()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(3); // format version
            writer.Write(SessionId ?? string.Empty);
            writer.Write(NodeId ?? string.Empty);
            writer.Write(CurrentPlayerId);
            writer.Write(JoiningPlayerId);
            writer.Write(BattleOwnerPlayerId);
            writer.Write(TurnNumber);
            writer.Write(IsGameEnded);
            writer.Write(AtFirstNumberChoice);
            writer.Write(Players.Count);
            foreach (var player in Players) player.Write(writer);
            writer.Write(Groups.Count);
            foreach (var group in Groups) group.Write(writer);
            return stream.ToArray();
        }

        /// <summary>
        /// Rebuilds the complete roster/groups and optionally begins the saved player's
        /// next turn. Call only for a snapshot captured at a turn boundary.
        /// </summary>
        public void RestoreInto(GameManager game, int localPlayerId, bool resume = true)
        {
            ArgumentNullException.ThrowIfNull(game);
            var players = Players.Select(player => player.CreatePlayer()).ToList();
            var byId = players.ToDictionary(player => player.PlayerId);
            var groups = Groups.Select(group => new Group(group.GroupId, group.Name,
                group.PlayerIds.Where(byId.ContainsKey).Select(id => byId[id]))).ToList();
            if (!groups.SelectMany(group => group.Players).Any())
                throw new InvalidDataException("Snapshot contains no grouped players.");
            if (!byId.ContainsKey(localPlayerId))
                throw new InvalidDataException("Local player is missing from the snapshot roster.");

            game.LocalPlayerId = localPlayerId;
            game.RestoreTurnBoundary(players, groups, CurrentPlayerId, TurnNumber);
            if (IsGameEnded)
                throw new InvalidDataException("Cannot resume an ended battle snapshot.");
            if (resume)
            {
                if (AtFirstNumberChoice) game.ResumeRestoredInputBoundary();
                else game.ResumeRestoredTurn();
            }
        }

        public static BattleSnapshot Deserialize(byte[] payload)
        {
            using var stream = new MemoryStream(payload ?? []);
            using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != 3) throw new InvalidDataException("Unsupported battle snapshot version.");

            var snapshot = new BattleSnapshot
            {
                SessionId = reader.ReadString(),
                NodeId = reader.ReadString(),
                CurrentPlayerId = reader.ReadInt32(),
                JoiningPlayerId = reader.ReadInt32(),
                BattleOwnerPlayerId = reader.ReadInt32(),
                TurnNumber = reader.ReadInt32(),
                IsGameEnded = reader.ReadBoolean(),
                AtFirstNumberChoice = reader.ReadBoolean()
            };

            int playerCount = ReadCount(reader, stream, 128);
            for (int i = 0; i < playerCount; i++) snapshot.Players.Add(BattlePlayerSnapshot.Read(reader));
            int groupCount = ReadCount(reader, stream, 128);
            for (int i = 0; i < groupCount; i++) snapshot.Groups.Add(BattleGroupSnapshot.Read(reader));
            if (stream.Position != stream.Length) throw new InvalidDataException("Unexpected trailing battle snapshot data.");
            if (!snapshot.Players.Any(player => player.PlayerId == snapshot.CurrentPlayerId))
                throw new InvalidDataException("Snapshot current player is missing from its roster.");
            return snapshot;
        }

        private static int ReadCount(BinaryReader reader, Stream stream, int max)
        {
            if (stream.Position + sizeof(int) > stream.Length) throw new EndOfStreamException();
            int count = reader.ReadInt32();
            if (count < 0 || count > max) throw new InvalidDataException("Invalid collection size in battle snapshot.");
            return count;
        }
    }

    public sealed class BattlePlayerSnapshot
    {
        public int PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PortraitKey { get; set; } = string.Empty;
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Number0 { get; set; }
        public int Number1 { get; set; }
        public int AttacksReceivedThisTurn { get; set; }
        public List<BattleSkillSnapshot> Skills { get; set; } = [];

        internal static BattlePlayerSnapshot FromPlayer(Player player) => new()
        {
            PlayerId = player.PlayerId,
            Name = player.Name,
            PortraitKey = player.PortraitKey,
            Health = player.Health,
            MaxHealth = player.MaxHealth,
            Number0 = player.Numbers[0],
            Number1 = player.Numbers[1],
            AttacksReceivedThisTurn = player.AttacksReceivedThisTurn,
            Skills = player.Skills.Select(BattleSkillSnapshot.FromSkill).ToList()
        };

        public Player CreatePlayer()
        {
            var player = new Player(PlayerId, Name, Math.Max(1, MaxHealth), Skills.Select(skill => skill.CreateSkill()).ToList())
            {
                PortraitKey = PortraitKey,
                MaxHealth = Math.Max(1, MaxHealth),
                Health = Math.Clamp(Health, 0, Math.Max(1, MaxHealth))
            };
            player.SetNumber(0, Number0);
            player.SetNumber(1, Number1);
            player.RestoreTurnCounters(AttacksReceivedThisTurn);
            return player;
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId); writer.Write(Name ?? string.Empty); writer.Write(PortraitKey ?? string.Empty);
            writer.Write(Health); writer.Write(MaxHealth); writer.Write(Number0); writer.Write(Number1);
            writer.Write(AttacksReceivedThisTurn); writer.Write(Skills.Count);
            foreach (var skill in Skills) skill.Write(writer);
        }

        internal static BattlePlayerSnapshot Read(BinaryReader reader)
        {
            var player = new BattlePlayerSnapshot
            {
                PlayerId = reader.ReadInt32(), Name = reader.ReadString(), PortraitKey = reader.ReadString(),
                Health = reader.ReadInt32(), MaxHealth = reader.ReadInt32(), Number0 = reader.ReadInt32(),
                Number1 = reader.ReadInt32(), AttacksReceivedThisTurn = reader.ReadInt32()
            };
            int count = reader.ReadInt32();
            if (count < 0 || count > 64) throw new InvalidDataException("Invalid skill count in battle snapshot.");
            for (int i = 0; i < count; i++) player.Skills.Add(BattleSkillSnapshot.Read(reader));
            return player;
        }
    }

    public sealed class BattleSkillSnapshot
    {
        public SkillType Type { get; set; }
        public int Value { get; set; }
        public SkillTrigger Trigger { get; set; }
        public SkillUsageLimit UsageLimit { get; set; }
        public int MaxUses { get; set; }
        public int RemainingUses { get; set; }
        public bool IsActivated { get; set; }

        internal static BattleSkillSnapshot FromSkill(Skill skill) => new()
        {
            Type = skill.Type, Value = skill.Value, Trigger = skill.Trigger,
            UsageLimit = skill.Usage.UsageLimit, MaxUses = skill.Usage.MaxUses,
            RemainingUses = skill.Usage.RemainingUses, IsActivated = skill.Usage.IsActivated
        };

        public Skill CreateSkill()
        {
            var skill = SkillFactory.CreateSkill(Type);
            skill.Value = Value;
            skill.Trigger = Trigger;
            skill.Usage = UsageLimit switch
            {
                SkillUsageLimit.PerTurn => new SkillUsageState(SkillUsageLimit.PerTurn),
                SkillUsageLimit.PerRound => new SkillUsageState(SkillUsageLimit.PerRound),
                SkillUsageLimit.Limited => new SkillUsageState(Math.Max(0, MaxUses)),
                _ => new SkillUsageState()
            };
            skill.Usage.MaxUses = Math.Max(0, MaxUses);
            skill.Usage.RemainingUses = Math.Clamp(RemainingUses, 0, Math.Max(0, MaxUses));
            skill.Usage.IsActivated = IsActivated;
            return skill;
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write((int)Type); writer.Write(Value); writer.Write((int)Trigger); writer.Write((int)UsageLimit);
            writer.Write(MaxUses); writer.Write(RemainingUses); writer.Write(IsActivated);
        }

        internal static BattleSkillSnapshot Read(BinaryReader reader) => new()
        {
            Type = (SkillType)reader.ReadInt32(), Value = reader.ReadInt32(),
            Trigger = (SkillTrigger)reader.ReadInt32(), UsageLimit = (SkillUsageLimit)reader.ReadInt32(),
            MaxUses = reader.ReadInt32(), RemainingUses = reader.ReadInt32(), IsActivated = reader.ReadBoolean()
        };
    }

    public sealed class BattleGroupSnapshot
    {
        public int GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<int> PlayerIds { get; set; } = [];

        internal void Write(BinaryWriter writer)
        {
            writer.Write(GroupId); writer.Write(Name ?? string.Empty); writer.Write(PlayerIds.Count);
            foreach (int id in PlayerIds) writer.Write(id);
        }

        internal static BattleGroupSnapshot Read(BinaryReader reader)
        {
            var group = new BattleGroupSnapshot { GroupId = reader.ReadInt32(), Name = reader.ReadString() };
            int count = reader.ReadInt32();
            if (count < 0 || count > 128) throw new InvalidDataException("Invalid group size in battle snapshot.");
            for (int i = 0; i < count; i++) group.PlayerIds.Add(reader.ReadInt32());
            return group;
        }
    }
}
