"""Revoice the three existing skill studies with recorded upright piano.
Python 3.12, standard library only; preserves every pitch, onset and velocity.
"""
import array
import audioop
import hashlib
import json
import math
import pathlib
import wave

ROOT = pathlib.Path(__file__).resolve().parent
PREVIEWS = ROOT.parents[1] / 'Content/audio/previews'
SOURCE = PREVIEWS / 'skill-studies'
OUT = PREVIEWS / 'piano-studies'
OUT.mkdir(parents=True, exist_ok=True)
RATE = 44100
FRAMES = RATE * 34
SAMPLES = {}
for layer in (1, 2):
    for index, midi in [(24,69),(26,73),(28,77),(30,81),(32,85)]:
        path = ROOT / 'piano-samples' / f'Player_dyn{layer}_rr1_{index:03}.wav'
        with wave.open(str(path), 'rb') as w:
            assert w.getnchannels() == 2 and w.getframerate() == RATE
            data = audioop.lin2lin(w.readframes(w.getnframes()), w.getsampwidth(), 2)
        values = array.array('h', data)
        peak = max(abs(v) for v in values)
        onset = next(i//2 for i in range(0, len(values), 2)
                     if max(abs(values[i]), abs(values[i+1])) > peak*.02)
        onset = max(0, onset - round(RATE*.003))
        SAMPLES[layer,midi] = data[onset*4:]


def render(spec):
    bus = array.array('f', [0]) * (FRAMES*2)
    beat_seconds = 60 / spec['meta']['tempo']
    for event in spec['note_events']:
        note, velocity = event['note'], event['velocity']
        layer = 1 if velocity < 75 else 2
        root = min((69,73,77,81,85), key=lambda n: abs(n-note))
        raw = SAMPLES[layer,root]
        pitched, _ = audioop.ratecv(raw, 2, 2, round(RATE*2**((note-root)/12)), RATE, None)
        pcm = array.array('h', pitched)
        offset = round(event['beat'] * beat_seconds * RATE)
        hold = event['gate'] * beat_seconds
        release = .22
        count = min(len(pcm)//2, round((hold+release)*RATE), FRAMES-offset)
        # Recorded velocity layers supply timbre; gain shapes the phrase.
        gain = (velocity/80)**1.4
        for i in range(count):
            seconds = i/RATE
            envelope = 1 if seconds <= hold else max(0, 1-(seconds-hold)/release)**2
            attack = min(1, i/(RATE*.001))
            bus[2*(offset+i)] += pcm[2*i]/32768*gain*envelope*attack
            bus[2*(offset+i)+1] += pcm[2*i+1]/32768*gain*envelope*attack
    dry = array.array('f', bus)
    for seconds, gain in [(.037,.065),(.061,.045),(.103,.027),(.157,.015)]:
        delay = round(seconds*RATE)
        for i in range(delay, FRAMES):
            bus[2*i] += dry[2*(i-delay)+1]*gain
            bus[2*i+1] += dry[2*(i-delay)]*gain
    peak = max(abs(x) for x in bus)
    rms = math.sqrt(sum(x*x for x in bus)/len(bus))
    return bus, peak, rms


renders = []
for name in 'ABC':
    spec = json.loads((SOURCE/f'{name}.ARR-SPEC.json').read_text(encoding='utf-8'))
    renders.append((name, spec, *render(spec)))
target = min(.06, min(.7*rms/peak for _,_,_,peak,rms in renders))
manifest = []
for name, spec, bus, peak, rms in renders:
    gain = target / rms
    pcm = array.array('h')
    for i in range(FRAMES):
        fade = min(1, (FRAMES-1-i)/(RATE*.25))
        for channel in range(2):
            pcm.append(round(bus[2*i+channel]*gain*fade*32767))
    with wave.open(str(OUT/f'{name}.wav'), 'wb') as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    midi = (SOURCE/f'{name}.mid').read_bytes()
    assert midi.count(b'\0\xc0\x09') == 1
    (OUT/f'{name}.mid').write_bytes(midi.replace(b'\0\xc0\x09', b'\0\xc0\x00'))
    spec['arrangement']['roster'][0]['instrument'] = 'VSCO recorded upright piano, two velocity layers'
    spec['arrangement']['roster'][0]['midi']['program'] = 0
    spec['arrangement']['roster'][0]['note'] = '同一旋律改用立式钢琴；按MIDI gate制音，短释放；无人声与钟声层'
    spec['render']['midi']['instrument_map']['lead']['name'] = 'VSCO Upright Piano'
    spec['render']['midi']['instrument_map']['lead']['program'] = 0
    spec['meta']['upstream']['brief'] = '用户要求改用钢琴；保留三条候选旋律供比较'
    spec['intent']['one_thing'] = '用自然钢琴音色判断主旋律是否值得继续发展'
    spec['intent']['reference_pair'] = [
        dict(track=f'本项目钟琴候选{name}',borrow='保留其全部音高、节奏与句间停顿，作音色对照'),
        dict(track='VSCO Upright Piano 原始单音录音',borrow='保留声学起音与两档力度音色')]
    spec['mix_intent']['mood'] = '自然立式钢琴独奏，清楚的制音与短房间反射'
    (OUT/f'{name}.ARR-SPEC.json').write_text(json.dumps(spec,ensure_ascii=False,indent=2),encoding='utf-8')
    metric = dict(id=name,title=spec['meta']['title'],seconds=34,peak=peak*gain,
                  rms=rms*gain,sha256=hashlib.sha256((OUT/f'{name}.wav').read_bytes()).hexdigest())
    manifest.append(metric)
    print(json.dumps(metric,ensure_ascii=False),flush=True)
(OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
