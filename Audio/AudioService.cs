#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Fida.Audio;

/// <summary>Two music voices, bounded effect voices, independent buses, and optional silent fallback.</summary>
public sealed class AudioService : IDisposable, IAudioFeedback
{
    private readonly string _root;
    private readonly Dictionary<SoundCue, SoundEffect> _effects = new();
    private readonly HashSet<string> _failed = new();
    private readonly Dictionary<SoundCue, double> _lastPlayed = new();
    private readonly List<(SoundEffectInstance Voice, SoundCue Cue)> _voices = new();
    private MusicVoice? _current, _outgoing;
    private MusicTrack _requested = MusicTrack.Silence;
    private double _time;
    private float _fade = 1f, _outgoingStart, _sceneGain = 1f;
    private bool _suspended, _disposed;
    public static AudioService? Current { get; private set; }
    public bool IsSuspended => _suspended;
    public int ActiveEffectVoices => _voices.Count;
    public MusicTrack RequestedTrack => _requested;
    public int FailedAssetCount => _failed.Count;

    public AudioService(string contentRoot)
    {
        _root = contentRoot;
        Current = this;
    }

    public static void Feedback(SoundCue cue) => Current?.Play(cue);

    public void SetMusic(MusicTrack track)
    {
        if (_disposed || track == _requested) return;
        _requested = track;
        // A rapid third scene switch still retains at most two decoded tracks.
        _outgoing?.Dispose();
        _outgoing = _current;
        _outgoingStart = _outgoing?.Instance.Volume ?? 0f;
        _current = null;
        _fade = 0;
        if (track == MusicTrack.Silence) return;
        var effect = Load("music/" + track + ".wav");
        if (effect == null) return;
        try
        {
            var instance = effect.CreateInstance();
            instance.IsLooped = true;
            instance.Volume = 0;
            _current = new MusicVoice(effect, instance);
            instance.Play();
            if (_suspended) instance.Pause();
        }
        catch (Exception ex) when (AudioError(ex))
        {
            _current?.Dispose(); _current = null;
            effect.Dispose();
            Report("music/" + track, ex);
        }
    }

    private SoundEffect? Load(string path)
    {
        if (_failed.Contains(path)) return null;
        try
        {
            string filename = Path.Combine(_root, "audio", path);
            using var stream = Path.IsPathRooted(filename) ? File.OpenRead(filename) :
                TitleContainer.OpenStream(filename.Replace('\\', '/'));
            return SoundEffect.FromStream(stream);
        }
        catch (Exception ex) when (AudioError(ex)) { Report(path, ex); return null; }
    }

    private static bool AudioError(Exception ex) => ex is NoAudioHardwareException or
        IOException or UnauthorizedAccessException or InvalidOperationException or
        ArgumentException or NotSupportedException or TypeInitializationException;

    private void Report(string path, Exception ex)
    {
        if (_failed.Add(path)) Trace.WriteLine($"Fida audio unavailable ({path}): {ex.Message}");
    }

    public void Play(SoundCue cue)
    {
        if (_disposed || _suspended || DisplaySettingsManager.Current.EffectsVolume <= 0) return;
        double interval = cue == SoundCue.Hover ? .09 : cue is SoundCue.Hit or SoundCue.HeavyHit ? .045 : .075;
        if (_lastPlayed.TryGetValue(cue, out double last) && _time - last < interval) return;
        _lastPlayed[cue] = _time;
        CollectVoices();
        if (cue == SoundCue.Hover && _voices.Count >= 5) return;
        if (!_effects.TryGetValue(cue, out var effect))
        {
            effect = Load("sfx/" + cue + ".wav");
            if (effect == null) return;
            _effects.Add(cue, effect);
        }
        SoundEffectInstance? voice = null;
        try
        {
            if (_voices.Count >= 8)
            {
                int expendable = _voices.FindIndex(v => v.Cue == SoundCue.Hover);
                if (expendable < 0) expendable = 0;
                _voices[expendable].Voice.Dispose();
                _voices.RemoveAt(expendable);
            }
            voice = effect.CreateInstance();
            voice.Volume = DisplaySettingsManager.Current.EffectsVolume / 100f;
            voice.Play();
            _voices.Add((voice, cue));
        }
        catch (Exception ex) when (AudioError(ex))
        {
            voice?.Dispose();
            Report("sfx/" + cue, ex);
        }
    }

    public void Update(float seconds, float sceneGain)
    {
        if (_disposed || _suspended) return;
        float dt = Math.Clamp(seconds, 0, .1f);
        _time += dt;
        _fade = Math.Min(1, _fade + dt / 1.25f);
        _sceneGain += (sceneGain - _sceneGain) * (1 - MathF.Exp(-dt * 5));
        float music = DisplaySettingsManager.Current.MusicVolume / 100f * _sceneGain;
        if (_current != null) _current.Instance.Volume = music * MathF.Sin(_fade * MathF.PI / 2);
        if (_outgoing != null)
        {
            // Cos(PI/2) rounds slightly below zero in single precision. Stop the
            // completed voice before assigning that invalid volume to MonoGame.
            if (_fade >= 1) { _outgoing.Dispose(); _outgoing = null; }
            else _outgoing.Instance.Volume = Math.Clamp(
                Math.Min(music, _outgoingStart) * MathF.Cos(_fade * MathF.PI / 2), 0f, 1f);
        }
        CollectVoices();
        foreach (var voice in _voices) voice.Voice.Volume = DisplaySettingsManager.Current.EffectsVolume / 100f;
    }

    private void CollectVoices()
    {
        for (int i = _voices.Count - 1; i >= 0; i--)
            if (_voices[i].Voice.State == SoundState.Stopped)
            { _voices[i].Voice.Dispose(); _voices.RemoveAt(i); }
    }

    public void SetSuspended(bool suspended)
    {
        if (_disposed || _suspended == suspended) return;
        _suspended = suspended;
        if (suspended)
        {
            _current?.Instance.Pause(); _outgoing?.Instance.Pause();
            foreach (var voice in _voices) voice.Voice.Dispose();
            _voices.Clear();
        }
        else
        {
            _current?.Instance.Resume(); _outgoing?.Instance.Resume();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (Current == this) Current = null;
        _current?.Dispose(); _outgoing?.Dispose();
        foreach (var voice in _voices) voice.Voice.Dispose();
        foreach (var effect in _effects.Values) effect.Dispose();
        _voices.Clear(); _effects.Clear();
    }

    private sealed class MusicVoice(SoundEffect effect, SoundEffectInstance instance) : IDisposable
    {
        public SoundEffectInstance Instance => instance;
        public void Dispose() { instance.Dispose(); effect.Dispose(); }
    }
}
