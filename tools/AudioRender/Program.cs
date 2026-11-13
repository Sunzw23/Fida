// Original Fida score and effects. Offline synthesis; no samples or external packages.
using System.Text;
using System.Text.Json;

string output = Path.GetFullPath(args.FirstOrDefault() ?? "Content/audio");
Directory.CreateDirectory(Path.Combine(output, "music"));
Directory.CreateDirectory(Path.Combine(output, "sfx"));
var report = new List<object>();

// Four-note identity: D, A, E, F. Each arrangement leaves room for thought.
int[][] chords = [[50, 57, 60, 64], [46, 53, 57, 60], [41, 53, 57, 64], [48, 55, 62, 65]];
foreach (var (name, bpm, beats) in new[] {
    ("Journey", 64, 64), ("Battle", 90, 96), ("Boss", 96, 96),
    ("Prologue", 48, 48), ("Ending", 64, 64) })
{
    double beat = 60.0 / bpm;
    var song = new Canvas(beats * beat, loop: true, seed: 1977);
    bool battle = name is "Battle" or "Boss";
    bool boss = name == "Boss";
    for (int bar = 0; bar < beats / 4; bar++)
    {
        int[] chord = chords[(bar / 2) % 4];
        double start = bar * 4 * beat;
        if (name == "Prologue")
        {
            if (bar % 2 == 0)
            {
                song.Note(start, chord[0], beat * 7.5, .12, "pad", -.2);
                song.Note(start + beat * .3, chord[2] + 12, beat * 6, .085, "felt", .2);
            }
            if (bar % 4 == 3) song.Note(start + beat, 76, beat * 3, .045, "bell", .4);
            continue;
        }
        foreach (int n in chord.Skip(1))
            song.Note(start, n, beat * 4.7, battle ? .035 : .055, "pad", (n % 5 - 2) * .23);
        song.Note(start, chord[0] - (boss ? 12 : 0), beat * 3.3, battle ? .19 : .11, "bass", -.12);
        if (battle)
        {
            for (int step = 0; step < 8; step++)
            {
                int n = chord[1 + step % 3] + (step % 4 == 3 ? 12 : 0);
                song.Note(start + step * beat / 2, n, beat * 1.1,
                    step % 2 == 0 ? .095 : .055, "pluck", step % 2 == 0 ? -.28 : .28);
                if (step % 2 == 0 || boss)
                    song.Paper(start + step * beat / 2, .085, boss ? .045 : .028, step % 2 == 0 ? -.4 : .4);
            }
            song.Note(start, 38, .24, .18, "thump", 0);
            song.Note(start + beat * 2, 45, .16, .08, "wood", .15);
            if (boss)
            {
                song.Note(start + beat * 2.75, chord[0], beat * 1.2, .12, "bass", .1);
                if (bar % 4 == 3) song.Note(start + beat * 3, 63, beat * 2, .035, "pad", -.4);
            }
        }
        else
        {
            song.Note(start + beat * .2, chord[1] + 12, beat * 3, .13, "felt", -.25);
            if (bar % 2 == 1) song.Note(start + beat * 2.5, chord[2] + 12, beat * 2.7, .095, "felt", .28);
            if (name == "Ending")
            {
                song.Note(start + beat, chord[1], beat * 1.8, .07, "pluck", -.35);
                song.Note(start + beat * 3, chord[3], beat * 1.5, .06, "pluck", .35);
            }
        }
        if (bar % 4 == 0 || (name == "Ending" && bar % 4 == 2))
        {
            int[] motif = name == "Ending" ? [74, 81, 76, 77, 76, 74] : [74, 81, 76, 77];
            double spacing = battle ? 1.5 : 1.7;
            for (int i = 0; i < motif.Length; i++)
                song.Note(start + (1 + i * spacing) * beat, motif[i], beat * 2.8,
                    battle ? .085 : .14, "felt", (i % 2 == 0 ? -.16 : .2));
        }
    }
    song.Space(battle ? .16 : .28);
    Save(song, "music", name, .65);
}

foreach (string name in new[] { "Hover", "Confirm", "Back", "Select", "Combine", "Hit", "HeavyHit", "Heal",
    "ShieldForm", "ShieldBreak", "Skill", "Fire", "Stone", "Reflect", "Drain", "Turn", "Victory", "Defeat", "Reinforcement", "Invalid" })
{
    double duration = name switch { "Victory" => 2.7, "Defeat" => 2.6, "Heal" or "Reinforcement" => 1.3,
        "Combine" => .42, "Hover" => .065, "Select" or "Invalid" => .16, _ => .7 };
    var sound = new Canvas(duration, false, 87 + name.Length);
    switch (name)
    {
        case "Hover": sound.Paper(0, .055, .18, 0); break;
        case "Select": sound.Paper(0, .07, .18, 0); sound.Note(0, 76, .13, .26, "wood", 0); break;
        case "Confirm": sound.Paper(0, .08, .2, 0); sound.Note(0, 69, .2, .25, "wood", 0); sound.Note(.045, 76, .25, .18, "pluck", .1); break;
        case "Back": sound.Paper(0, .11, .18, 0); sound.Note(0, 64, .18, .19, "wood", 0); break;
        case "Combine": sound.Paper(0, .3, .17, -.1); sound.Note(.285, 76, .13, .34, "wood", .1); sound.Note(.3, 81, .12, .1, "bell", .2); break;
        case "Hit": case "HeavyHit":
            sound.Paper(0, .12, .42, 0); sound.Note(0, name == "Hit" ? 43 : 31, .34, .65, "thump", 0);
            sound.Note(.008, 57, .13, .28, "wood", .08);
            if (name == "HeavyHit") sound.Paper(.035, .23, .3, -.1);
            break;
        case "Heal": case "ShieldForm": case "Skill": case "Reinforcement":
            int[] tones = name == "Heal" ? [74, 77, 81] : name == "Reinforcement" ? [62, 69, 74] : [62, 69];
            for (int i = 0; i < tones.Length; i++) sound.Note(i * .1, tones[i], duration - i * .1, .25, "bell", (i - 1) * .2);
            sound.Paper(0, .13, .1, 0); break;
        case "ShieldBreak": sound.Note(0, 57, .24, .43, "wood", 0); sound.Note(.01, 86, .5, .22, "bell", -.2); sound.Paper(.025, .24, .3, .2); break;
        case "Fire": sound.Paper(0, .55, .43, 0); sound.Note(.02, 38, .35, .32, "thump", 0); break;
        case "Stone": sound.Note(0, 38, .4, .55, "wood", 0); sound.Paper(.005, .14, .25, 0); break;
        case "Reflect": sound.Paper(0, .35, .22, 0); sound.Note(.08, 81, .25, .24, "bell", -.2); sound.Note(.17, 69, .35, .28, "wood", .2); break;
        case "Drain": sound.Note(0, 57, .45, .24, "pad", -.2); sound.Note(.14, 69, .5, .3, "bell", .2); break;
        case "Turn": sound.Note(0, 74, .22, .18, "pluck", -.1); sound.Note(.12, 81, .32, .16, "pluck", .1); break;
        case "Victory":
            for (int i = 0; i < 4; i++) sound.Note(i * .21, new[] { 62, 69, 76, 77 }[i], 2.1 - i * .2, .23, "felt", (i - 1.5) * .15);
            break;
        case "Defeat": sound.Note(0, 50, 2.1, .27, "felt", -.2); sound.Note(.24, 57, 2.1, .2, "felt", .2); break;
        case "Invalid": sound.Note(0, 48, .12, .22, "wood", 0); break;
    }
    sound.Space(name is "Victory" or "Defeat" or "Heal" ? .2 : .06);
    Save(sound, "sfx", name, name == "Hover" ? .18 : .72);
}
File.WriteAllText(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

void Save(Canvas canvas, string folder, string name, double maxPeak)
{
    string path = Path.Combine(output, folder, name + ".wav");
    var metrics = canvas.Write(path, maxPeak);
    report.Add(new { name, folder, seconds = canvas.Length / (double)Canvas.Rate, peak = metrics.Peak, rms = metrics.Rms, loop = canvas.Loop });
    Console.WriteLine($"{folder}/{name}: {canvas.Length / (double)Canvas.Rate:F2}s, peak {metrics.Peak:F3}, RMS {metrics.Rms:F3}");
}

sealed class Canvas(double seconds, bool loop, int seed)
{
    public const int Rate = 32000;
    readonly float[] left = new float[(int)Math.Round(seconds * Rate)];
    readonly float[] right = new float[(int)Math.Round(seconds * Rate)];
    readonly Random random = new(seed);
    public int Length => left.Length;
    public bool Loop => loop;

    void Add(int index, double value, double pan)
    {
        if (loop) index = ((index % Length) + Length) % Length;
        else if (index < 0 || index >= Length) return;
        left[index] += (float)(value * Math.Sqrt((1 - pan) * .5));
        right[index] += (float)(value * Math.Sqrt((1 + pan) * .5));
    }

    public void Note(double start, int midi, double duration, double gain, string kind, double pan)
    {
        double frequency = 440 * Math.Pow(2, (midi - 69) / 12.0);
        int offset = (int)(start * Rate), count = (int)(duration * Rate);
        for (int i = 0; i < count; i++)
        {
            double t = i / (double)Rate, p = 2 * Math.PI * frequency * t;
            double release = Math.Min(1, (duration - t) / Math.Min(.2, duration * .25));
            double value = kind switch
            {
                "felt" => (Math.Sin(p) * Math.Exp(-t * 1.2) + .26 * Math.Sin(p * 2.002) * Math.Exp(-t * 3.2)
                    + .07 * Math.Sin(p * 3.01) * Math.Exp(-t * 5)) * (1 - Math.Exp(-t * 160)),
                "pad" => (Math.Sin(p) + .23 * Math.Sin(p * 1.002) + .15 * Math.Sin(p * 2))
                    * Math.Min(1, t / .65) * Math.Min(1, (duration - t) / .8) * .6,
                "bass" => (Math.Sin(p) + .19 * Math.Sin(2 * p)) * Math.Exp(-t * 1.6) * (1 - Math.Exp(-t * 65)),
                "bell" => (Math.Sin(p) + .2 * Math.Sin(p * 2.76) * Math.Exp(-t * 4)) * Math.Exp(-t * 3) * (1 - Math.Exp(-t * 200)),
                "wood" => (Math.Sin(p) + .4 * Math.Sin(p * 1.58)) * Math.Exp(-t * 35) * (1 - Math.Exp(-t * 1300)),
                "thump" => Math.Sin(p * (.65 + .35 * Math.Exp(-t * 30))) * Math.Exp(-t * 20) * (1 - Math.Exp(-t * 900)),
                _ => (Math.Sin(p) + .35 * Math.Sin(p * 2) * Math.Exp(-t * 9)) * Math.Exp(-t * 6) * (1 - Math.Exp(-t * 350))
            };
            Add(offset + i, value * gain * release, pan);
        }
    }

    public void Paper(double start, double duration, double gain, double pan)
    {
        double low = 0;
        for (int i = 0; i < duration * Rate; i++)
        {
            double noise = random.NextDouble() * 2 - 1;
            low += .13 * (noise - low);
            double envelope = Math.Sin(Math.PI * i / (duration * Rate)) * Math.Exp(-i / (duration * Rate) * 4);
            Add((int)(start * Rate) + i, low * gain * envelope, pan);
        }
    }

    public void Space(double wet)
    {
        var dryL = (float[])left.Clone(); var dryR = (float[])right.Clone();
        for (int tap = 0; tap < 6; tap++)
        {
            int delay = (int)((.113 + tap * .137) * Rate);
            double level = wet * Math.Pow(.62, tap);
            for (int i = 0; i < Length; i++)
            {
                int source = i - delay;
                if (source < 0) { if (!loop) continue; source += Length; }
                left[i] += (float)((tap % 2 == 0 ? dryR[source] : dryL[source]) * level);
                right[i] += (float)((tap % 2 == 0 ? dryL[source] : dryR[source]) * level);
            }
        }
    }

    public (double Peak, double Rms) Write(string path, double ceiling)
    {
        double peak = Math.Max(left.Max(v => Math.Abs(v)), right.Max(v => Math.Abs(v)));
        double scale = Math.Min(1, ceiling / Math.Max(peak, .001)), sum = 0;
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + Length * 4);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
        writer.Write((short)2); writer.Write(Rate); writer.Write(Rate * 4); writer.Write((short)4); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(Length * 4);
        for (int i = 0; i < Length; i++)
        {
            // Circular rendering carries decays/reverb across music loop boundaries.
            // One-shot effects taper to zero so truncating a tail never clicks.
            double fade = loop ? 1 : Math.Min(1, (Length - 1 - i) / (Rate * .025));
            double l = left[i] * scale * fade, r = right[i] * scale * fade;
            writer.Write((short)Math.Round(Math.Clamp(l, -1, 1) * 32767));
            writer.Write((short)Math.Round(Math.Clamp(r, -1, 1) * 32767));
            sum += l * l + r * r;
        }
        return (peak * scale, Math.Sqrt(sum / (Length * 2)));
    }
}
