# Cold numeric UI

Implemented on `codex/cold-numeric-ui`, continuing the existing working tree.

## Visual system

- Matte charcoal `#0C1015`, bone white `#EDECE5`, steel-gray rules, ice cyan for selection.
- The shared theme styles menus, settings, rules, online screens, dialogue, campaign, loadout and results.
- No background grid or moving edge ornaments. Quiet borders and a light primary Start button.
- Original cut-angle 0–9 numeral masks are generated once per graphics device, with antialiasing and no font dependency.
- Six transparent hard-edge character busts share a 3 × 2 atlas at `Content/portraits/cold_roster.png`.
  Both desktop and Android MGCB manifests include it. Existing portrait files are preserved.

## Battle interaction

Desktop places enemies above allies, with up to four units per row. Every unit has two large square number buttons,
a small portrait, name and health bar. The command appears between teams. Extra network rosters wrap into compact rows.
Android retains its portrait-oriented roster geometry and uses the same palette, artwork and numeral renderer.

Only the portrait moves during idle breathing, subtle sway, anticipation, strike, hit stop and recoil.
Number buttons, focus bounds, labels and health bars remain in their fixed layout positions.
The number-flow trace lasts 420 ms; combat presentation lasts 480 ms, including roughly 58 ms of hit stop.
Reduced Motion settles these effects immediately and disables idle movement.

Skills remain passive/contextual according to the existing rules. Compact skill codes are shown under health;
hover over the portrait/name for skill names and costs (long press on Android).
Turns continue to advance through the existing state machine.

## Verification

```powershell
dotnet build Fida.csproj --no-restore -m:1 -p:BuildInParallel=false
dotnet test Fida.Tests/Fida.Tests.csproj --no-restore -m:1 -p:BuildInParallel=false
dotnet build Fida.Android/Fida.Android.csproj --no-restore -m:1 -p:BuildInParallel=false
dotnet build tools/UiPreview/UiPreview.csproj -m:1 -p:BuildInParallel=false
dotnet tools/UiPreview/bin/Debug/net8.0/UiPreview.dll 1600 1000 artifacts/cold-ui
dotnet tools/UiPreview/bin/Debug/net8.0/UiPreview.dll 1280 720 artifacts/cold-ui
```

The render harness saves real MonoGame screenshots of menu, settings, rules, campaign, loadout and 3v3/4v4 battles.
It checks complete number clicks, verifies that idle portrait pixels change while number-button pixels do not,
checks stable input bounds during combat presentation, and confirms that Reduced Motion stops the action.
It does not write user settings or campaign progress. It requires a local graphics-capable desktop session.
The layout regression tests cover standard rosters, narrow windows and extra roster members.

## Generated asset provenance

The character atlas was re-created with a local ComfyUI instance running Z-Image Turbo, one
render per character, then keyed and assembled by `tools/comfy/build_atlas.py`.
Its complete generation prompt is saved beside it as `Content/portraits/paper_roster.prompt.txt`.
The atlas cells are Arith, Mara, Sentinel, Withered King, Warden and Echo in row-major order.
They are static illustrations animated through runtime transforms.

The previous pass flattened every character into the same faceless ivory shape, so the six busts
were not distinguishable at the size the battle screen draws them. This pass keeps the paper-fold
planes and the navy/ivory palette but gives each unit its own silhouette and value structure:
asymmetric swept hair for Arith, a stiff bob, chunky scarf and two hollow almond eye sockets for
Mara, a crested slit helm for the Sentinel, a five-point crown and beard for the Withered King, the
widest shoulders plus a chest chain for the Warden, and a tall pointed hood over a dark void for
Echo. Faces are carved from ivory paper rather than skin so the set stays on two materials.

Mara is the one character with deliberately empty eyes: the eye sockets are cut clean through the
ivory paper with a thin pale rim, no pupil, iris or catchlight. Readable eyes made her the most
ordinary face in the cast, and the guide is more unsettling when you cannot tell what she is
looking at. It also supplies the head's value contrast on the ivory dialogue card.

The atlas is 1536 × 1024 (512 px cells). `UIPlayer` sizes each cell aspect-fit and
bottom-centred, so every bust is cut by the bottom edge of its cell, and MGCB premultiplies the
alpha, so the matte is hard and spill-free on the charcoal background.
