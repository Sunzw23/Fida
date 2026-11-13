# First audio pass

Fida's original, offline-rendered score combines soft additive piano, plucked tones,
quiet sustained harmonies, and dry wood/paper percussion. The shared motif is
D–A–E–F. These are synthesized instruments, not recorded performances. There are
no third-party samples, generated-service dependencies, downloads, or runtime synthesis.

## Music

| File in `Content/audio/music` | Length | Use |
| --- | --- | --- |
| Journey.wav | 60 s | Menu, map, loadout, lobby, ordinary victory aftermath |
| Battle.wav | 64 s | Ordinary battles and online duels |
| Boss.wav | 60 s | Campaign nodes with `isBoss: true` |
| Prologue.wav | 60 s | Withered King's loadout/battle and subdued defeat aftermath |
| Ending.wav | 60 s | Cleared campaign nodes offering endings |

The prologue is identified by `selfTargetingEnemy`, taking precedence over boss
music. Its result uses silence with no victory sting. PvP ignores the selected
campaign node. Overlays preserve the current track; pause reduces its gain to 35%,
settings/rules to 55%, and visible dialogue to 42%. Scene changes crossfade over
1.25 s. Music does not react to health thresholds, since health wraps in Fida.

Music is rendered circularly: notes and reverberation continue across the loop
boundary rather than fading out on every repeat. Assets are 32 kHz, stereo,
16-bit PCM WAVs, copied by both MGCB manifests. All 25 WAVs total about 39.2 MiB.
Only the current and outgoing music tracks remain decoded (at most about 16 MiB
of music buffers during a transition); effects are small and cached on demand.

## Feedback

`Audio/BattleAudio.cs` subscribes to gameplay events without changing game rules
or replicating sound messages over the network. Local, automated, and received
actions use the same feedback. It distinguishes selection, number combination,
attack strength, healing, new shields, shield breaks, Stone blocking, reflection,
Blood recovery, local turns, and added reinforcements. Menu buttons also support
mouse/touch and controller confirmation, with quiet hover/focus feedback.

Attack sounds follow the animator's hit-stop at 268.8 ms; healing follows its
240 ms midpoint. Timing is frame-based (within a frame of the visual). Reduced
motion plays results immediately. Shield break replaces the queued damage sound.
Cancelled scenes clear queued impacts, and disposal removes event subscriptions.

There are 20 effect assets: Hover, Confirm, Back, Select, Combine, Hit, HeavyHit,
Heal, ShieldForm, ShieldBreak, Skill, Fire, Stone, Reflect, Drain, Turn, Victory,
Defeat, Reinforcement, Invalid. Fire is reserved: the current Fire skill has no
gameplay implementation. General supported skills share the Skill cue; special
implemented reactions have dedicated textures.

Playback caps effects at eight simultaneous voices and rate-limits repeated cues.
Losing application focus pauses music and clears effect tails. Music resumes at
its previous position on activation. Missing assets or unavailable audio hardware
are logged and permit silent play.

## Settings and replacement

Settings has independent music/effect volume controls, persisted in the existing
local `settings.json`. Defaults are 65% and 80%; old settings receive these defaults.
Use −/+ for ten-point changes, click the percentage for mute/default volume,
Reset for default music volume, and Test to preview the healing effect.

Replace a WAV while retaining its name to revise the sound without changing game
logic. Keep music looping seamlessly and effects short. To regenerate everything:

```powershell
dotnet run --project tools/AudioRender/AudioRender.csproj -- Content/audio
```

The deterministic score, synthesis, stereo placement, and reverb are all in that
tool. `Content/audio/manifest.json` records duration, peak, RMS, and loop flags.
The WAV files are shipped assets; building the game does not rerun the synthesizer.

## Verification

```powershell
dotnet build Fida.csproj --no-restore -m:1 -p:BuildInParallel=false
dotnet test Fida.Tests/Fida.Tests.csproj --no-restore -m:1 -p:BuildInParallel=false
dotnet build Fida.Android/Fida.Android.csproj --no-restore -m:1 -p:BuildInParallel=false
dotnet run --project tools/AudioCheck/AudioCheck.csproj
dotnet run --project tools/AudioCheck/AudioCheck.csproj -- --listen
```

The default AudioCheck is muted, opens a small temporary MonoGame window, decodes
and plays all 25 assets, then checks transitions, rapid switching, suspension,
silence, and the effect voice limit. `--listen` provides a 36-second audible tour.
Neither mode loads or saves user settings or campaign progress.

Desktop playback and landscape/portrait settings renders were verified. Android
compiles and packages the audio; physical Android playback is not yet verified.
The normal testhost could not access its parent Windows process in this environment.
`dotnet run --project tools/AudioValidation/AudioValidation.csproj` directly executed
all 224 current Fact/InlineData cases sequentially, including the new audio tests,
with zero failures. This fallback is intended for the current suite, not as a
replacement for xUnit's fixture or parallelization support.
