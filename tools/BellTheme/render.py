"""Recorded glockenspiel study for Fida. Python 3.12 standard library only.
Original score; VSCO 2 CE recorded samples (CC0), sources in samples/sources.json.
Run: python tools/BellTheme/render.py
"""
import array
import audioop
import json
import math
import pathlib
import random
import wave

ROOT = pathlib.Path(__file__).resolve().parent
OUT = ROOT.parents[1] / 'Content/audio/previews'
OUT.mkdir(parents=True, exist_ok=True)
RATE = 44100
SECONDS = 30
N = RATE * SECONDS
random.seed(2718)

# Glockenspiel sounds one octave above the library's written note labels.
# Measured sounding fundamentals, not filename octaves, determine pitch shifting.
SOURCES = [(789.0, 'G4'), (1052.0, 'C5'), (1580.0, 'G5'), (2113.0, 'C6')]
samples = []
for frequency, label in SOURCES:
    with wave.open(str(ROOT / 'samples' / f'glock_medium_{label}.wav'), 'rb') as wav:
        assert wav.getnchannels() == 2 and wav.getsampwidth() == 2
        raw = wav.readframes(wav.getnframes())
        sr = wav.getframerate()
    values = array.array('h', raw)
    peak = max(abs(v) for v in values)
    # Trim room tone before the actual strike; retain the recorded attack.
    onset = next(i // 2 for i in range(0, len(values), 2)
                 if max(abs(values[i]), abs(values[i + 1])) > peak * .055)
    onset = max(0, onset - int(.002 * sr))
    samples.append((frequency, raw[onset * 4:], sr, peak / 32768))

# Eight bars in 3/4 at 60 BPM, with a one-beat pickup and five-second decay.
# Question (bars 1-4), varied answer (5-8); unequal note lengths and real rests.
# D minor, with a brighter F-major opening to the answer and A7 -> Dm cadence.
bars = [
    [(86, 1.5), (84, .5), (81, 1)],
    [(79, 1), (77, .5), (76, .5), (77, .75), (None, .25)],
    [(81, 1), (79, .5), (77, .5), (76, .75), (None, .25)],
    [(74, 2), (None, 1)],
    [(77, .5), (79, .5), (81, 1.5), (84, .5)],
    [(86, 1), (84, .5), (81, .5), (79, .75), (None, .25)],
    [(81, .75), (77, .75), (76, .5), (73, .5), (76, .5)],
    [(74, 2.5), (None, .5)],
]
assert all(abs(sum(length for _, length in bar) - 3) < .001 for bar in bars)
score = [(0.15, 79, .46), (.65, 81, .54)]
beat = 1.15
for bar_index, bar in enumerate(bars):
    for note, length in bar:
        if note is not None:
            # Shape the apex and soften phrase endings; no fixed interval sequencer.
            strength = .62 if note == 86 else .49
            if bar_index in (3, 7): strength = .43
            score.append((beat, note, strength * random.uniform(.94, 1.035)))
        beat += length


def empty():
    return array.array('f', [0]) * (N * 2)


def strike(bus, at, midi, gain, pan=0, maximum=7.5):
    hz = 440 * 2 ** ((midi - 69) / 12)
    frequency, raw, source_rate, peak = min(samples, key=lambda s: abs(math.log2(hz / s[0])))
    converted, _ = audioop.ratecv(raw, 2, 2, round(source_rate * hz / frequency), RATE, None)
    pcm = array.array('h', converted)
    frames = min(len(pcm) // 2, round(maximum * RATE))
    offset = round(at * RATE)
    # Gentle high-frequency damping tames hard mallets; preserves sampled resonances.
    cutoff = 3700 if midi >= 81 else 2900
    alpha = 1 - math.exp(-2 * math.pi * cutoff / RATE)
    l = r = 0
    left_gain, right_gain = math.sqrt(1 - pan), math.sqrt(1 + pan)
    scale = gain * .52 / max(.01, peak)
    for i in range(min(frames, N - offset)):
        l += alpha * (pcm[2 * i] / 32768 - l)
        r += alpha * (pcm[2 * i + 1] / 32768 - r)
        tail = min(1, (frames - i) / (RATE * .3))
        bus[2 * (offset + i)] += l * scale * left_gain * tail
        bus[2 * (offset + i) + 1] += r * scale * right_gain * tail


def room(dry):
    wet = array.array('f', dry)
    # Soft, diffuse stereo reflections; no synthetic oscillators/pads underneath.
    for delay, level in [(.071, .13), (.127, .105), (.193, .085), (.283, .065),
                         (.397, .047), (.557, .033), (.719, .023), (.947, .015)]:
        shift = round(delay * RATE)
        for i in range(shift, N):
            wet[2 * i] += dry[2 * (i - shift) + 1] * level
            wet[2 * i + 1] += dry[2 * (i - shift)] * level
    return wet


def save(name, bus):
    peak = max(abs(x) for x in bus)
    scale = .65 / max(peak, .001)
    pcm = array.array('h')
    power = 0
    for i in range(N):
        fade = min(1, i / (RATE * .025), (N - 1 - i) / (RATE * 2.7))
        for ch in range(2):
            v = bus[2 * i + ch] * scale * fade
            power += v * v
            pcm.append(round(max(-1, min(1, v)) * 32767))
    path = OUT / name
    with wave.open(str(path), 'wb') as wav:
        wav.setnchannels(2); wav.setsampwidth(2); wav.setframerate(RATE)
        wav.writeframes(pcm.tobytes())
    metrics = dict(file=name, seconds=SECONDS, peak=peak * scale, rms=math.sqrt(power / (N * 2)))
    print(json.dumps(metrics))
    return metrics


if __name__ == '__main__':
    melody = empty()
    for at, note, gain in score:
        strike(melody, at + random.uniform(-.012, .012), note, gain, -.07)

    full = array.array('f', melody)
    anchors = [(62, 69), (58, 65), (60, 67), (62, 69),
               (65, 72), (55, 62), (57, 64), (62, 69)]
    for bar, (root, fifth) in enumerate(anchors):
        start = 1.15 + bar * 3
        strike(full, start + .027, root, .19 if bar != 7 else .24, -.25, 8)
        if bar in (0, 2, 4, 6, 7):
            strike(full, start + .17, fifth, .095, .27, 7)

    report = [save('BellTheme-v2.wav', room(full)), save('BellTheme-v2-melody.wav', room(melody))]
    (OUT / 'BellTheme-v2.json').write_text(json.dumps(dict(
        title='Fida — The Bell Road (study)', meter='3/4', bpm=60,
        key='D minor', score=score, metrics=report), indent=2))
