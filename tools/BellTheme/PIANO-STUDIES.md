# Piano revoicing

The user requested piano after the bell auditions. These versions preserve the
three melodies' note pitches, onsets, gates, and velocities. No new composition,
accompaniment, or game music replacement is included.

Run `python tools/BellTheme/piano_studies.py` with Python 3.12. Outputs are in
`Content/audio/previews/piano-studies/`: A/B/C WAV, MIDI, ARR-SPEC, and manifest.
The MIDI program is changed from Glockenspiel (9) to Acoustic Grand Piano (0).
Actual audition audio uses an upright piano; program 0 is a portable GM fallback.

Recordings: VSCO 2 CE Upright Piano, sampled by Simon Dalzell of Ivy Audio,
Winter 2015–16, distributed by Versilian Studios. Sources, hashes, original mapping,
recording information, and CC0 license are retained in `piano-samples/`.
The downloaded subset has MIDI roots 69/73/77/81/85 in two recorded velocity
layers. Samples are resampled by at most two semitones, with attack preserved,
note-off damping and a short stereo room. No bell layer or oscillator is mixed in.

All three files are 34-second, 44.1 kHz stereo 16-bit WAVs with matched RMS and
sample peaks at or below 0.70. No compression or limiting is applied. The source
score is 30 seconds; the remaining time allows release/silence for comparison.
Loudness numbers describe RMS, not LUFS. Technical checks do not assess whether
the melody is pleasing; that remains for user audition.
