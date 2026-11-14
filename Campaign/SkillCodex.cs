#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Campaign
{
    /// <summary>
    /// What the player has met: every skill that appeared in a battle roster, plus the skills
    /// their campaign progress implies. Ownership is derived from progress, not stored here.
    /// </summary>
    public sealed class SkillCodexState
    {
        private readonly HashSet<SkillType> _encountered = new();
        private readonly HashSet<SkillType> _read = new();
        private readonly Dictionary<SkillType, string> _firstSeen = new();

        public IReadOnlyCollection<SkillType> Encountered => _encountered;

        public bool IsKnown(SkillType skill) => _encountered.Contains(skill);
        public bool IsRead(SkillType skill) => _read.Contains(skill);
        public bool IsNew(SkillType skill) => _encountered.Contains(skill) && !_read.Contains(skill);

        public string FirstSeen(SkillType skill) =>
            _firstSeen.TryGetValue(skill, out string? source) ? source : "";

        /// <summary>Records a discovery; returns true when the set changed.</summary>
        public bool Record(IEnumerable<SkillType> skills, string source)
        {
            bool changed = false;
            foreach (var skill in skills)
            {
                if (!SkillCatalog.CanCreate(skill)) continue;
                if (_encountered.Add(skill))
                {
                    _firstSeen[skill] = source ?? "";
                    changed = true;
                }
                else if (!_firstSeen.ContainsKey(skill) && !string.IsNullOrEmpty(source))
                {
                    _firstSeen[skill] = source;
                }
            }
            return changed;
        }

        public bool Record(IEnumerable<Skill> skills, string source) =>
            Record(skills.Select(skill => skill.Type), source);

        public bool MarkRead(SkillType skill) => _read.Add(skill);

        public void Merge(SkillCodexData data)
        {
            Record(SkillCodexStore.ParseNames(data.Encountered), "");
            foreach (var pair in data.FirstSeen)
            {
                if (SkillCatalog.FromName(pair.Key) is not { } type) continue;
                if (_encountered.Contains(type) && !string.IsNullOrEmpty(pair.Value))
                    _firstSeen[type] = pair.Value;
            }
            foreach (var type in SkillCodexStore.ParseNames(data.Read))
                _read.Add(type);
        }

        public SkillCodexData Snapshot()
        {
            var ordered = _encountered
                .OrderBy(skill => skill.ToString(), StringComparer.Ordinal)
                .ToArray();
            return new SkillCodexData
            {
                Version = 1,
                Encountered = ordered.Select(SkillCatalog.EnglishName).ToArray(),
                Read = ordered.Where(_read.Contains).Select(SkillCatalog.EnglishName).ToArray(),
                FirstSeen = ordered
                    .Where(_firstSeen.ContainsKey)
                    .ToDictionary(SkillCatalog.EnglishName, skill => _firstSeen[skill])
            };
        }
    }

    /// <summary>
    /// Process-wide codex facade. The store is replaceable so tests and the preview tool never
    /// touch the player's save; a null store keeps discoveries in memory only.
    /// </summary>
    public static class SkillCodex
    {
        private static readonly object Gate = new();
        private static SkillCodexStore? _store = new(SkillCodexStore.DefaultPath());
        private static SkillCodexState _state = new();
        private static bool _loaded;

        public static SkillCodexState State
        {
            get
            {
                lock (Gate)
                {
                    EnsureLoaded();
                    return _state;
                }
            }
        }

        public static IReadOnlyCollection<SkillType> Encountered => State.Encountered;

        public static bool IsNew(SkillType skill) => State.IsNew(skill);

        public static string FirstSeen(SkillType skill) => State.FirstSeen(skill);

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            if (_store != null) _state.Merge(_store.Load());
        }

        /// <summary>Swaps the backing store and reloads. Null disables persistence.</summary>
        public static void UseStore(SkillCodexStore? store)
        {
            lock (Gate)
            {
                _store = store;
                _state = new SkillCodexState();
                _loaded = true;
                if (store != null) _state.Merge(store.Load());
            }
        }

        public static void RecordSkills(IEnumerable<SkillType> skills, string source)
        {
            bool changed;
            lock (Gate)
            {
                EnsureLoaded();
                changed = _state.Record(skills, source);
            }
            if (changed) Save();
        }

        public static void RecordPlayers(IEnumerable<Player> players, string source) =>
            RecordSkills(players.SelectMany(player => player.Skills).Select(skill => skill.Type), source);

        public static void MarkRead(SkillType skill)
        {
            bool changed;
            lock (Gate)
            {
                EnsureLoaded();
                changed = _state.MarkRead(skill);
            }
            if (changed) Save();
        }

        /// <summary>Skills implied by finished campaign nodes; derived on read so old saves need no migration.</summary>
        public static IEnumerable<SkillType> DeriveFromProgress(params CampaignProgress?[] progresses)
        {
            foreach (var progress in progresses)
            {
                if (progress == null) continue;
                foreach (string nodeId in progress.CompletedNodes)
                {
                    var node = CampaignCatalog.GetNode(nodeId);
                    if (node == null) continue;
                    foreach (var reward in node.Rewards) yield return reward;
                    foreach (var enemy in node.Enemies)
                        foreach (var skill in enemy.Skills) yield return skill;
                    foreach (var enemy in node.Reinforcements)
                        foreach (var skill in enemy.Skills) yield return skill;
                }
            }
        }

        private static void Save()
        {
            var store = _store;
            if (store == null) return;
            SkillCodexData snapshot;
            lock (Gate) snapshot = _state.Snapshot();
            store.Save(snapshot);
        }
    }
}
