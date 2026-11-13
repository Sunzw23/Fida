#nullable enable
using Fida.Logic;

namespace Fida.Audio;

public enum MusicTrack { Silence, Journey, Battle, Boss, Prologue, Ending }
public enum SoundCue
{
    Hover, Confirm, Back, Select, Combine, Hit, HeavyHit, Heal, ShieldForm, ShieldBreak,
    Skill, Fire, Stone, Reflect, Drain, Turn, Victory, Defeat, Reinforcement, Invalid
}

public interface IAudioFeedback
{
    void Play(SoundCue cue);
}

/// <summary>Scene decisions without a sound device, campaign saves, or UI dependencies.</summary>
public static class AudioScenePolicy
{
    public static MusicTrack Resolve(State state, MusicTrack previous, bool campaign,
        bool boss, bool prologue, bool ending, bool won) => state switch
    {
        State.Settings or State.Rules or State.Paused => previous,
        State.Playing or State.OnlinePlaying when campaign && prologue => MusicTrack.Prologue,
        State.Playing or State.OnlinePlaying when campaign && boss => MusicTrack.Boss,
        State.Playing or State.OnlinePlaying => MusicTrack.Battle,
        State.Skills or State.OnlineCampaignSkills when prologue => MusicTrack.Prologue,
        State.GameOver when campaign && prologue => MusicTrack.Silence,
        State.GameOver when campaign && ending && won => MusicTrack.Ending,
        State.GameOver when !won => MusicTrack.Prologue,
        _ => MusicTrack.Journey
    };

    public static float Gain(State state, bool dialogue) => state switch
    {
        State.Paused => .35f,
        State.Settings or State.Rules => .55f,
        _ => dialogue ? .42f : 1f
    };

    public static SoundCue Skill(SkillType skill) => skill switch
    {
        SkillType.Huo => SoundCue.Fire,
        SkillType.Shi => SoundCue.Stone,
        SkillType.Mo => SoundCue.Reflect,
        SkillType.Xue => SoundCue.Drain,
        _ => SoundCue.Skill
    };
}
