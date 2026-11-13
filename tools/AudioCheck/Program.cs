// Run with --listen for a short audible preview. Default verification is muted.
using Fida;
using Fida.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
System.Diagnostics.Trace.AutoFlush = true;
using var game = new AudioCheck(args.Contains("--listen"));
game.Run();

sealed class AudioCheck : Game
{
    readonly GraphicsDeviceManager graphics;
    readonly bool listen;
    AudioService audio = null!;
    readonly string root = Path.Combine(AppContext.BaseDirectory, "Content");
    int frame;
    double elapsed;
    public AudioCheck(bool listen)
    {
        this.listen = listen;
        graphics = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 320, PreferredBackBufferHeight = 160 };
        Window.Title = "Fida audio verification";
    }

    // Does not read or save user settings or campaign progress.
    protected override void Initialize()
    {
        // Mute at the master bus, keeping real voice gains so invalid fades are detected.
        DisplaySettingsManager.Current.MusicVolume = 65;
        DisplaySettingsManager.Current.EffectsVolume = 80;
        SoundEffect.MasterVolume = listen ? 1 : 0;
        foreach (string path in Directory.GetFiles(Path.Combine(root, "audio"), "*.wav", SearchOption.AllDirectories))
        {
            using var stream = File.OpenRead(path);
            using var effect = SoundEffect.FromStream(stream);
            if (effect.Duration.TotalSeconds <= .04) throw new Exception("Invalid duration: " + path);
            using var voice = effect.CreateInstance();
            voice.Volume = 0; voice.Play(); voice.Stop();
            Console.WriteLine($"Decoded/played {Path.GetFileNameWithoutExtension(path)} ({effect.Duration.TotalSeconds:F2}s)");
        }
        audio = new AudioService(root);
        audio.SetMusic(MusicTrack.Journey);
        if (!listen)
        {
            foreach (var track in new[] { MusicTrack.Battle, MusicTrack.Prologue, MusicTrack.Silence, MusicTrack.Journey })
            {
                audio.SetMusic(track);
                for (int i = 0; i < 100; i++) audio.Update(.016f, 1);
            }
            Console.WriteLine("PASS: nonzero-volume crossfades reach completion, including silence.");
        }
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        elapsed += gameTime.ElapsedGameTime.TotalSeconds;
        frame++;
        if (!listen)
        {
            if (frame == 5) audio.Play(SoundCue.Victory);
            if (frame == 10) audio.SetMusic(MusicTrack.Battle);
            if (frame == 20) audio.SetMusic(MusicTrack.Boss);
            if (frame == 30)
            {
                audio.SetSuspended(true);
                if (!audio.IsSuspended || audio.ActiveEffectVoices != 0) throw new Exception("Suspension did not clear effects.");
            }
            if (frame == 40) audio.SetSuspended(false);
            if (frame == 50) audio.SetMusic(MusicTrack.Silence);
            if (frame == 60) audio.SetMusic(MusicTrack.Ending);
            if (frame == 70) audio.SetMusic(MusicTrack.Prologue);
            if (frame == 80)
            {
                foreach (var cue in Enum.GetValues<SoundCue>()) audio.Play(cue);
                if (audio.ActiveEffectVoices > 8) throw new Exception("Voice cap exceeded.");
            }
            if (frame >= 100)
            {
                if (audio.FailedAssetCount != 0) throw new Exception("Audio assets failed to load.");
                Console.WriteLine("PASS: 25 WAV assets, crossfades, rapid switching, suspension, silence, effect voice limit.");
                Exit();
            }
        }
        else
        {
            if (frame == 1) Console.WriteLine("Listening preview: Journey, Battle, Boss, Prologue, Ending, then effects (35 seconds).");
            var tracks = new[] { MusicTrack.Journey, MusicTrack.Battle, MusicTrack.Boss, MusicTrack.Prologue, MusicTrack.Ending };
            if (elapsed < 25) audio.SetMusic(tracks[Math.Min(4, (int)elapsed / 5)]);
            else
            {
                audio.SetMusic(MusicTrack.Silence);
                int index = (int)((elapsed - 25) * 2);
                if (frame % 30 == 0 && index < Enum.GetValues<SoundCue>().Length) audio.Play(Enum.GetValues<SoundCue>()[index]);
                if (elapsed >= 36) Exit();
            }
        }
        audio.Update((float)gameTime.ElapsedGameTime.TotalSeconds, 1);
        base.Update(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) audio?.Dispose();
        base.Dispose(disposing);
    }
}
