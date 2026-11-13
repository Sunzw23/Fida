# The Bell Road — Fida theme study

Independent 30-second audition, made after feedback on the first synthesized score.
The game's existing music is not replaced by this study.

Original melody: D minor, 3/4, 60 BPM; a one-beat pickup, eight bars, and a final
decay. The first four bars ask a descending question; the second phrase opens
more brightly, reaches D again, and returns through A7 to D minor. Rests, dotted
rhythms, phrase dynamics, and small timing offsets are explicitly scored.

All instrumental sound comes from four recorded glockenspiel samples from
[VSCO 2 Community Edition](https://github.com/sgossner/VSCO-2-CE), recorded by Sam
Gossner and Simon Dalzell, sample cutting by Elan Hickler/Soundemote, CC0 1.0.
The original license is saved in `samples/LICENSE`; exact source URLs and SHA256
hashes are in `samples/sources.json`. No accounts or paid services are needed.

The renderer tunes/resamples the recordings (the filenames are written pitches,
an octave below sounding pitch), damps the highest partials, and adds restrained
stereo reflections. Low accompaniment is pitched-down glockenspiel; it is not a
recording of a church bell. There are no synthesized piano, pad, or oscillator
layers. The output is an assembled sample performance, not a live performance.

Run using Python 3.12 (standard library; `audioop` was removed in Python 3.13):

```powershell
python tools/BellTheme/render.py
```

Outputs in `Content/audio/previews/`:

- `BellTheme-v2.wav`: melody, sparse lower bells, room reflections.
- `BellTheme-v2-melody.wav`: melody and room reflections only.
- `BellTheme-v2.json`: note events and render measurements.

Both WAVs are 30 seconds, stereo, 44.1 kHz/16-bit PCM with a tapered ending.
They are intentionally outside the game content manifests until the musical
direction has been auditioned. No runtime code or existing score assets change.
