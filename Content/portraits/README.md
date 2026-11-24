# Portrait Assets

The cold numeric UI uses `paper_roster.png`, a transparent 3-column × 2-row atlas:
Arith, Mara, Sentinel, Withered King, Warden, Echo. The full built-in image generation
prompt is in `paper_roster.prompt.txt`. Runtime regions are selected in `UIPlayerManager`.
See `Doc/ColdNumericUI.md` for integration and motion details. The older portraits below
remain available as source assets.

These portraits were generated as a visually consistent card set. Runtime text and UI frames are intentionally excluded from the images.

## Atlas authoring

`paper_roster.png` is 1536 × 1024 (512 px cells), transparent, straight alpha, straight
row-major order. It was rendered with a local ComfyUI instance running Z-Image Turbo from a
per-character prompt and then assembled by script:

```powershell
python tools/comfy/fida_portraits.py full --variant g_prod --seeds 4001,4002,4003
python tools/comfy/build_atlas.py --out artifacts/portraits-comfy/paper_roster-new.png `
  --cell 512 --strength 0.50 --scale 0.95,1.05,1.10,1.00,1.10,1.02 <six source renders>
python tools/comfy/preview.py <six cells> --sizes 64,320     # check it at real card size
python tools/comfy/compare.py --before <old atlas> --after <new atlas> --out compare.png
```

Two constraints drive the frame: `UIPlayer.Draw` sizes each cell aspect-fit and bottom-centred
against `PortraitBounds`, and MGCB premultiplies alpha, so every bust must be cut by the
bottom edge of its cell and carry a hard matte. `build_atlas.py` keys the render, flattens the
palette onto the shared navy/ivory swatches, and bottom-anchors the bust inside each cell.

Regenerating without the tooling still works: keep the filename, the 3 × 2 order, square cells
and transparency, and both `Content.mgcb` and `Content.Android.mgcb` stay valid.

## Adding a portrait

1. Add a square PNG to this directory.
2. Register it in `Content.mgcb` and `ResourceManager.LoadContent` as `portrait_<key>`.
3. Set the campaign enemy's `portraitKey` to `<key>` in data or code.

Unregistered keys use the cold atlas with a team/name-based fallback. Custom registered
`portrait_<key>` textures and explicit per-player overrides still take precedence.

## `arithmetist.png`

> Use case: stylized-concept. Asset type: square character portrait for a dark fantasy strategy game card. Primary request: create a portrait of The Arithmetist, a solitary scholar-warrior who manipulates numbers as occult laws. Scene/backdrop: plain warm aged-paper field with a subtle circular engraved halo, no scenery. Subject: androgynous adult arithmetist, calm severe expression, half-length frontal portrait, dark academic robe, small brass counting tokens and geometric sigils, no weapons. Style/medium: authentic 18th-century copperplate engraving and woodcut hybrid, dense black cross-hatching, hand-printed ink texture, antique book illustration, restrained ochre accents only. Composition/framing: centered bust, generous margin, readable at small card size, square crop. Lighting/mood: solemn, mysterious, high-contrast etched shading. Color palette: black ink, warm ivory paper, muted sepia and antique gold. Constraints: one character only, no text, no letters, no numerals, no UI frame, no watermark, no photorealism, consistent clean silhouette.

## `withered_king.png`

> Use case: stylized-concept. Asset type: square enemy portrait for a dark fantasy strategy game card. Primary request: create The Withered King, an ancient exhausted monarch repeating the last commands of a vanished crown. Scene/backdrop: plain warm aged-paper field with a cracked circular crown halo, no scenery. Subject: one elderly gaunt king, half-length frontal portrait, hollow but lucid eyes, weathered ceremonial crown, torn royal mantle, frail posture, silent menace rather than gore. Style/medium: authentic 18th-century copperplate engraving and woodcut hybrid, dense black cross-hatching, hand-printed ink texture, antique book illustration, restrained ochre accents only. Composition/framing: centered bust, generous margin, readable at small card size, square crop, visually consistent with a scholarly occult portrait set. Lighting/mood: funereal, dignified, uncanny, high-contrast etched shading. Color palette: black ink, warm ivory paper, muted sepia and tarnished antique gold. Constraints: one character only, no text, no letters, no numerals, no UI frame, no watermark, no blood or gore, no photorealism, clean silhouette.

## `enemy_fallback.png`

> Use case: stylized-concept. Asset type: square fallback enemy portrait for a dark fantasy strategy game card. Primary request: create an anonymous Echo Guard, a spectral armored servant of a dead numerical kingdom, suitable as the default portrait for many enemy units. Scene/backdrop: plain warm aged-paper field with a faint fractured geometric halo, no scenery. Subject: one faceless medieval guard, half-length frontal portrait, worn dark plate and layered cloth, closed mask with narrow shadowed eye slit, restrained supernatural presence. Style/medium: authentic 18th-century copperplate engraving and woodcut hybrid, dense black cross-hatching, hand-printed ink texture, antique book illustration, restrained ochre accents only. Composition/framing: centered bust, generous margin, readable at small card size, square crop, visually consistent with The Arithmetist and Withered King portrait set. Lighting/mood: austere, ominous, impersonal, high-contrast etched shading. Color palette: black ink, warm ivory paper, muted sepia and tarnished antique gold. Constraints: one character only, no text, no letters, no numerals, no UI frame, no watermark, no blood or gore, no photorealism, clean silhouette.
## Companion portraits (Ila, Bram, Sable, Ink Echo)

These keep the paper-cut style and human silhouettes, but the skin is deliberately
not human-toned: Ila is pale ice-blue paper with a white bob and a cold, level stare, Bram slate-blue stone with
pupil-less ivory eyes, Sable ivory page-skin split by ink cracks with one solid ink
eye, and the Ink Echo is faceless ink-black paper with a single horizontal light.
They read as people-shaped outsiders, not as ordinary humans.

`ila.png`, `bram.png`, `sable.png` and `ink_echo.png` are the campaign companion
portraits, generated with the same local ComfyUI / Z-Image Turbo pipeline and cut by
`tools/comfy/build_atlas.py`. One asset serves both the roster art (`artKey`) and the
battle card (`portraitKey`); the registrations live in `ResourceManager` as
`portrait_ila`, `portrait_bram`, `portrait_sable` and `portrait_ink_echo`.

```powershell
python tools/comfy/companion_art.py --seeds 8101          # all four
python tools/comfy/companion_art.py --chars sable --seeds 8102,8103
python tools/comfy/companion_art.py --chars bram --seeds 8101 --reprocess
```

Pick the seed whose render has the clean magenta/transparent background, then copy it
to `<key>.png` and rebuild the game content.
