using System.Text.Json;
using Fida.Audio;
using Fida.Logic;
using Fida.UI;
using Xunit;

namespace Fida.Tests;

public sealed class AudioTests
{
    [Fact]
    public void Music_SeparatesPrologueBossDuelAndEnding()
    {
        MusicTrack Resolve(State state, bool campaign, bool boss, bool prologue, bool ending, bool won) =>
            AudioScenePolicy.Resolve(state, MusicTrack.Journey, campaign, boss, prologue, ending, won);
        Assert.Equal(MusicTrack.Prologue, Resolve(State.Playing, true, true, true, false, false));
        Assert.Equal(MusicTrack.Boss, Resolve(State.Playing, true, true, false, false, false));
        Assert.Equal(MusicTrack.Battle, Resolve(State.OnlinePlaying, false, true, true, false, false));
        Assert.Equal(MusicTrack.Silence, Resolve(State.GameOver, true, true, true, false, true));
        Assert.Equal(MusicTrack.Ending, Resolve(State.GameOver, true, false, false, true, true));
        Assert.Equal(MusicTrack.Prologue, Resolve(State.GameOver, true, false, false, true, false));
    }

    [Theory]
    [InlineData(State.Paused)]
    [InlineData(State.Settings)]
    [InlineData(State.Rules)]
    public void Overlays_PreserveBattleMusicAndLowerVolume(State state)
    {
        Assert.Equal(MusicTrack.Boss, AudioScenePolicy.Resolve(state, MusicTrack.Boss, true, true, false, false, false));
        Assert.InRange(AudioScenePolicy.Gain(state, false), .1f, .6f);
        Assert.InRange(AudioScenePolicy.Gain(State.Playing, true), .1f, .6f);
    }

    [Fact]
    public void ExistingSettings_ReceiveAudioDefaultsAndCorruptVolumesAreClamped()
    {
        var settings = JsonSerializer.Deserialize<DisplaySettingsData>("{\"ReduceMotion\":true}")!;
        Assert.Equal(65, settings.MusicVolume);
        Assert.Equal(80, settings.EffectsVolume);
        settings.MusicVolume = -8; settings.EffectsVolume = 145;
        DisplaySettingsManager.NormalizeAudio(settings);
        Assert.Equal(0, settings.MusicVolume);
        Assert.Equal(100, settings.EffectsVolume);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShieldBreak_ReplacesDamageSoundAtImpact_AndReducedMotionDoesNotDelay(bool reduced)
    {
        var game = Game();
        var sink = new RecordingAudio();
        using var audio = new BattleAudio(game, sink, () => reduced) { Enabled = true };
        audio.RefreshPlayers();
        var target = game.Players[1];
        target.SetNumber(0, 0);
        sink.Cues.Clear();
        game.OnCardActionStarted(new CardActionEventArgs { SourcePlayerId = 0, TargetPlayerId = 1, Damage = 5 });
        target.SetNumber(0, 1);
        Assert.Empty(sink.Cues);
        audio.Update(.016f);
        if (!reduced)
        {
            Assert.Empty(sink.Cues);
            for (int i = 0; i < 16; i++) audio.Update(.016f);
        }
        Assert.Equal(new[] { SoundCue.ShieldBreak }, sink.Cues);
        audio.Update(.1f);
        Assert.Single(sink.Cues);
    }

    [Fact]
    public void Healing_UsesHealSoundAndCancellingPresentationDropsOldImpacts()
    {
        var game = Game(); var sink = new RecordingAudio();
        using var audio = new BattleAudio(game, sink, () => false) { Enabled = true };
        audio.RefreshPlayers();
        game.OnCardActionStarted(new CardActionEventArgs { TargetPlayerId = 0, Healing = 1 });
        game.Players[0].Heal(1);
        Assert.Empty(sink.Cues);
        for (int i = 0; i < 16; i++) audio.Update(.016f);
        Assert.Equal(new[] { SoundCue.Heal }, sink.Cues);
        sink.Cues.Clear();
        game.OnCardActionStarted(new CardActionEventArgs { TargetPlayerId = 1, Damage = 2 });
        audio.Enabled = false; audio.Update(.016f); audio.Enabled = true;
        for (int i = 0; i < 25; i++) audio.Update(.016f);
        Assert.Empty(sink.Cues);
    }

    [Fact]
    public void Stone_OnlyReplacesTheAttackItActuallyBlocks()
    {
        var game = Game(); var sink = new RecordingAudio();
        using var audio = new BattleAudio(game, sink, () => true) { Enabled = true };
        audio.RefreshPlayers();
        game.Players[1].ReplaceSkills([new StoneSkill()]);
        for (int attack = 0; attack < 2; attack++)
        {
            var state = new TargetSelectionState(game, new SkillContext(0, 2));
            state.Enter();
            game.StateStack.Push(new HoldState(game));
            state.Handle(new GameAction(ActionType.ChooseTarget) { TargetPlayerId = 1 });
            audio.Update(.016f);
        }
        Assert.Equal(new[] { SoundCue.Hit, SoundCue.Stone }, sink.Cues);
        Assert.Equal(9, game.Players[1].Health);
    }

    [Fact]
    public void DisposingAudio_UnsubscribesFromBattleAndPlayers()
    {
        var game = Game(); var sink = new RecordingAudio();
        var audio = new BattleAudio(game, sink, () => true) { Enabled = true };
        audio.RefreshPlayers(); audio.Dispose();
        game.Players[0].SetNumber(0, 0);
        game.OnNumberOperationResolved(new NumberOperationEventArgs());
        game.OnSkillActivated(new SkillActivatedEventArgs { SkillType = SkillType.Mo });
        Assert.Empty(sink.Cues);
    }

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1600, 1000)]
    [InlineData(720, 1280)]
    public void AudioSettings_FitLandscapeAndPortraitScreens(int width, int height)
    {
        var layout = new UIEditorialLayout(width, height);
        var viewport = new Microsoft.Xna.Framework.Rectangle(0, 0, width, height);
        for (int row = 0; row < 5; row++)
        {
            Assert.True(viewport.Contains(layout.SettingControl(row)));
            if (row > 0) Assert.False(layout.SettingControl(row).Intersects(layout.SettingControl(row - 1)));
            for (int segment = 1; segment < 4; segment++)
                Assert.False(layout.SettingControl(row, segment, 4).Intersects(layout.SettingControl(row, segment - 1, 4)));
        }
    }

    private static GameManager Game()
    {
        var game = new GameManager();
        var a = new Player(0, "A", 11, []); var b = new Player(1, "B", 11, []);
        game.Initialize([a, b], [new Group(0, "A", [a]), new Group(1, "B", [b])]);
        return game;
    }

    private sealed class RecordingAudio : IAudioFeedback
    {
        public List<SoundCue> Cues { get; } = [];
        public void Play(SoundCue cue) => Cues.Add(cue);
    }

    private sealed class HoldState(GameManager game) : GameState(game)
    {
        public override void Enter() { }
    }
}
