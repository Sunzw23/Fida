#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.Audio;

/// <summary>Listens to committed game events; never changes rules or sends network messages.</summary>
public sealed class BattleAudio : IDisposable
{
    private readonly GameManager _game;
    private readonly IAudioFeedback _audio;
    private readonly Func<bool> _reducedMotion;
    private readonly List<Player> _players = new();
    private readonly List<PendingImpact> _impacts = new();
    public bool Enabled { get; set; }

    public BattleAudio(GameManager game, IAudioFeedback audio, Func<bool> reducedMotion)
    {
        _game = game; _audio = audio; _reducedMotion = reducedMotion;
        game.GameStarted += OnStarted;
        game.RosterChanged += OnRosterChanged;
        game.TurnStarted += OnTurn;
        game.ActionSubmitted += OnAction;
        game.NumberOperationResolved += OnCombine;
        game.CardActionStarted += OnCard;
        game.SkillActivated += OnSkill;
        game.GameStateChanged += OnState;
    }

    public void RefreshPlayers(bool reinforcement = false)
    {
        bool added = _game.Players.Any(p => !_players.Contains(p));
        foreach (var player in _players)
        { player.NumberChanged -= OnNumber; player.HealthChanged -= OnHealth; }
        _players.Clear();
        _players.AddRange(_game.Players);
        foreach (var player in _players)
        { player.NumberChanged += OnNumber; player.HealthChanged += OnHealth; }
        if (Enabled && added && reinforcement) _audio.Play(SoundCue.Reinforcement);
    }

    private void OnStarted(object? sender, GameStartEventArgs e) { Clear(); RefreshPlayers(); }
    private void OnRosterChanged(object? sender, EventArgs e) => RefreshPlayers(reinforcement: true);
    private void OnTurn(object? sender, TurnStartEventArgs e)
    {
        if (Enabled && e.SourcePlayerId == _game.LocalPlayerId) _audio.Play(SoundCue.Turn);
    }

    private void OnAction(GameAction action, GameActionOrigin origin)
    {
        if (!Enabled || action.Type != _game.CurrentActionType) return;
        if (_game.CurrentStepValidTargets.Count > 0 && !_game.CurrentStepValidTargets.Contains(action.TargetPlayerId)) return;
        if (action.Type == ActionType.ChooseNumber && _game.CurrentStepValidNumbers.Count > 0 &&
            !_game.CurrentStepValidNumbers.Contains(action.Number)) return;
        if (action.Type == ActionType.ChooseNumberIndex)
        {
            if (action.NumberIndex is < 0 or > 1) return;
            int first = _game.Query.GetFirstChosenNumberIndex();
            if (first < 0) _audio.Play(SoundCue.Select);
            else if (first == action.NumberIndex && action.TargetPlayerId == _game.GetCurrentPlayer().PlayerId)
                _audio.Play(SoundCue.Invalid);
            // Successful second choices receive Combine from NumberOperationResolved.
        }
        else if (_game.CurrentState is MoSkillState && action.Type == ActionType.ChooseTarget)
            _audio.Play(SoundCue.Reflect);
        else if (action.Type == ActionType.ChooseNumber || action.Type == ActionType.DecideApply && action.IsApply)
            _audio.Play(SoundCue.Skill);
        else if (action.Type is ActionType.DecideApply or ActionType.ChooseTarget)
            _audio.Play(action.Type == ActionType.DecideApply ? SoundCue.Back : SoundCue.Confirm);
    }

    private void OnCombine(object? sender, NumberOperationEventArgs e)
    { if (Enabled) _audio.Play(SoundCue.Combine); }

    private void OnCard(object? sender, CardActionEventArgs e)
    {
        if (!Enabled) return;
        // Same fractions as UICardCombatAnimator: attack hit-stop at .56, heal at .50.
        _impacts.Add(new PendingImpact(e.TargetPlayerId,
            e.IsDamaging ? (e.Damage >= 3 ? SoundCue.HeavyHit : SoundCue.Hit) : SoundCue.Heal,
            e.IsDamaging ? .48f * .56f : .48f * .5f));
    }

    private void OnNumber(Player player, int index, int oldValue, int newValue)
    {
        if (!Enabled) return;
        if (newValue == 0 && oldValue != 0) _audio.Play(SoundCue.ShieldForm);
        if (oldValue == 0 && newValue == 1)
        {
            var pending = _impacts.LastOrDefault(i => i.PlayerId == player.PlayerId && i.Cue is SoundCue.Hit or SoundCue.HeavyHit);
            if (pending != null) pending.Cue = SoundCue.ShieldBreak;
        }
    }

    private void OnHealth(Player player)
    {
        if (Enabled && !_impacts.Any(i => i.PlayerId == player.PlayerId))
            _audio.Play(_game.CurrentState is TrueDamageState ? SoundCue.Reflect :
                _game.CurrentState is HealingState ? SoundCue.Heal : SoundCue.Hit);
    }

    private void OnState(object? sender, GameStateEventArgs e)
    {
        if (!Enabled) return;
        if (e.NewState is XueSkillState) _audio.Play(SoundCue.Drain);
        if (e.NewState is StoneSkillState)
        {
            var defender = _game.Players.Find(p => p.PlayerId == e.NewState.ActorId);
            if (defender?.AttacksReceivedThisTurn >= 1)
                foreach (var impact in _impacts.Where(i => i.PlayerId == defender.PlayerId)) impact.Cue = SoundCue.Stone;
        }
    }

    private void OnSkill(object? sender, SkillActivatedEventArgs e)
    {
        if (!Enabled) return;
        _audio.Play(AudioScenePolicy.Skill(e.SkillType));
        if (e.SkillType == SkillType.Shi)
            foreach (var impact in _impacts.Where(i => i.PlayerId == e.SourcePlayerId)) impact.Cue = SoundCue.Stone;
    }

    public void Update(float seconds)
    {
        if (!Enabled) { Clear(); return; }
        if (!_players.SequenceEqual(_game.Players)) RefreshPlayers();
        for (int i = _impacts.Count - 1; i >= 0; i--)
        {
            _impacts[i].Remaining -= Math.Max(seconds, 0);
            if (_reducedMotion() || _impacts[i].Remaining <= 0)
            { _audio.Play(_impacts[i].Cue); _impacts.RemoveAt(i); }
        }
    }

    public void Clear() => _impacts.Clear();

    public void Dispose()
    {
        _game.GameStarted -= OnStarted; _game.RosterChanged -= OnRosterChanged;
        _game.TurnStarted -= OnTurn; _game.ActionSubmitted -= OnAction;
        _game.NumberOperationResolved -= OnCombine; _game.CardActionStarted -= OnCard;
        _game.SkillActivated -= OnSkill;
        _game.GameStateChanged -= OnState;
        foreach (var player in _players)
        { player.NumberChanged -= OnNumber; player.HealthChanged -= OnHealth; }
        _players.Clear(); Clear();
    }

    private sealed class PendingImpact(int playerId, SoundCue cue, float remaining)
    {
        public int PlayerId = playerId;
        public SoundCue Cue = cue;
        public float Remaining = remaining;
    }
}
