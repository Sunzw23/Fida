# Music skill trial — Fida bell melodies

Three LLM-authored melody auditions, following the installed third-party
[music-composition-skills](https://github.com/jtydhr88/music-composition-skills).
No text-to-audio music model, cloud music API, or new synthetic instrument is used.

Installed using the system skill-installer: mc-workflow, mc-development, mc-melody,
mc-form, mc-harmony, mc-orchestration, mc-symbolic-score, mc-render-compile,
mc-ai-tell-audit. Local instructions and relevant sections were read this turn;
automatic skill availability begins on the next turn. The spec records choices
and deliberate exemptions for these short, solo instrumental auditions.

| Audition | Key | Melodic approach |
| --- | --- | --- |
| A — 归途 | C major | Rising third, stepwise return; recurring dotted rhythm; a higher continuation |
| B — 石阶 | D minor | Short pickup and longer answering notes; a narrower, lower register |
| C — 同行 | F major | Rising fourth, uneven durations and more forward movement |

All three: ten bars in 4/4 at quarter-note = 80 BPM; 30 seconds of score plus
four seconds of ringing decay. A1 is bars 1–4; A2 is bars 5–8, retaining part of
the original motif; a two-bar coda reuses and closes material. Each highest note
appears in bar 7. Velocities relax toward the end, and written rests remain.

These are distinct composition candidates, not a single-variable A/B experiment.
The same VSCO recorded glockenspiel, rendering chain, length, and near-equal RMS
make it easier to judge the melodic differences. No accompaniment is performed:
the harmony fields in ARR-SPEC are plans for a later arrangement, not audible
chords or verified audio harmony. MIDI note-off times describe editable gates;
the audition renderer treats the bells as one-shot strikes with natural tails.

Run with Python 3.12:

```powershell
python tools/BellTheme/skill_studies.py
```

Outputs: `Content/audio/previews/skill-studies/{A,B,C}.wav`, matching `.mid`,
matching `.ARR-SPEC.json` (JSON is valid YAML), and a render manifest. Both MIDI
and sampled WAV derive from the same note-event list. Instrument program is
GM Glockenspiel; actual imported MIDI playback depends on the user's sound bank.

Technical checks cover file format, sample peaks, ending silence, equal-RMS
comparison, and an independent readback of MIDI note events/timing against the
specification. No listening, aesthetic score, LUFS measurement, or user preference
is inferred from those checks. Human listening checks in mc-ai-tell-audit remain
pending. Existing game music and content manifests are unchanged.

Sample sources/license: `samples/sources.json`, `samples/LICENSE`; see README.md.
