# Paper shader feasibility

Question: can the paper material be driven by a shader instead of the current
CPU-baked grain, and is it worth doing?

Short answer: yes, it is technically cheap and low-risk here, but the gain is
mostly engineering (no per-size baking, no cache churn, no visible tiling) plus a
door to edge/ink treatment that the CPU path cannot reach. Grain alone will not
make the UI look more like paper 鈥?the current direction deliberately keeps the
fibres quiet.

## What the UI does today

- `UI/UIPaperGrain.cs` generates one shared 256虏 grain tile.
- `UI/UIPaper.SizedPaper` resamples each nine-slice surface to its destination
  size and adds `(Sample(x,y) - .96) * 255` to every destination pixel, i.e. about
  卤7 levels on ivory. Results are cached per (texture, source, width, height,
  slice) and dropped by `TrimCache` above 8 MB.
- `UI/UIManager.Draw` renders the whole page into `_pageFrame` (`RenderTarget2D`)
  and then blits it to the back buffer with a single `SpriteBatch.Draw`.
- `Doc/PaperUI.md` already records that visible fibres, scratches and repeated
  tiles were removed on purpose.

## Why a shader fits this renderer

The final blit is exactly the insertion point a grain pass wants: the page is
already a texture, so the pass replaces one `SpriteBatch.Draw` with one quad draw
and needs **no extra render target**. The crossfade that currently layers
`_previousPageFrame` over `_pageFrame` can move into the same pass as a second
sampler plus an alpha uniform.

## Prototype

`tools/PaperGrainLab` (standalone, not referenced by the game) applies the shader
to a real captured frame and reports cost:

```
dotnet run --project tools/PaperGrainLab -- artifacts/audio-ui/menu-1280x720.png artifacts/paper-shader
```

Outputs: current CPU grain, procedural shader, fibre-forward variant, baked-tile
variant, 6脳 zoom crops of an ivory sheet and a navy sheet, and an amplified
difference image. `Content/PaperGrain.fx` contains both implementations
(`Procedural` and `Tiled`).

Measured at 1280脳720 (922K pixels), DesktopGL, HiDef, this machine:

| Path | Cost | Resident memory |
|------|------|-----------------|
| CPU bake, full-screen sheet | 10.08 ms per size (UI thread) | 3.5 MiB per cached size + 256 KiB shared tile |
| Shader, analytic noise | 0.075 ms per frame | 0 |
| Shader, baked tile | 0.026 ms per frame | 1.0 MiB at 512虏, 4.0 MiB at 1024虏 |
| Bare pass-through blit | 0.024 ms per frame | 0 |

The CPU number is a cache-miss cost, not a per-frame cost, but a page rebuild or
a window resize pays it for the backdrop plus every sheet, and it is what
`SizedPaper` does on the UI thread today. The 8 MB `SizedPaper` budget and its
`TrimCache` pass exist only to contain that cost.

## Grain character (revision 2)

The first revision read as sandy: its tooth was a per-pixel hash (卤1.75 %), its
fibres were two long coherent bands, and the mottle cloud was large and strong.
On a flat sheet that looks like static with a brushed pattern on top. Revision 2
changes the field, not just the amount:

- **Quintic gradient noise** instead of value noise, with the lattice rotated
  between octaves, so there are no axis-aligned blobs or stair steps.
- **Short, scattered fibres**: three layers at 3鈥? px cells, stretched only 3鈥?脳,
  each with its own direction, plus a slow density mask. The old 9 脳 81 px
  strands were what made it look brushed.
- **Weak warp** (7 px instead of 34 px) keeps strands organic without turning
  them into wood grain.
- **Formation cloud** moved from 120 px to 220 px and made smaller, so it reads
  as uneven pulp rather than dirt.
- **Tooth is gradient noise at ~1.6 px**, not a per-pixel hash: same micro-detail,
  no sand grain, and it survives display scaling far better.
- **Grain follows luminance** (paper on ivory, calm on navy) and carries a slight
  warm tint so it is not grey static.

Verified on flat swatches rather than on screenshots, because the captured PNGs
already contain their own paper texture:

| Row (4脳 zoom) | Ivory luminance 蟽 | Navy luminance 蟽 |
|---|---|---|
| current CPU grain | 4.28 | 4.29 |
| revision 1 shader | 2.97 | 0.15 |
| `soft` | 1.99 | 0.20 |
| `paper` (default) | 2.54 | 0.27 |
| `linen` | 4.11 | 0.44 |

蟽 is the standard deviation of luminance, i.e. the strength the eye reads as
tooth. The CPU path has the same spread on ivory and navy, which is why the navy
areas look woven today; the shader holds navy near flat at every preset.
`paper` matches the old shader's perceived strength with none of its banding,
`linen` matches the CPU strength for large quiet backdrops, and `soft` is for
text-heavy panels.

Presets are just uniform ratios (`ToothAmount`, `FiberAmount`, `MottleAmount`,
`LumaKeep`, `Warmth`), so they can be data-driven per screen later rather than
compiled in.

The `Tiled` technique in the prototype is only a mobile fallback and currently
looks worse than the analytic path: a value-noise bake at ~4 px cells reads as
pebbling on ivory and cyan. A production bake has to come from the same gradient
field (render the pass once offline into a tile with mirrored wrap), not from a
cheap CPU value-noise loop.


## Toolchain reality

Verified, not assumed:

- MGCB 3.8.5.1 (game's DesktopGL pipeline) compiles `PaperGrain.fx` to MGFX.
- MGCB 3.8.4.1 (the Android project's pipeline) compiles the same file for
  `platform:Android`, `profile:Reach`. The output embeds **GLSL ES 1.0** with
  `precision mediump float` in the pixel shader.
- `EffectImporter`/`EffectProcessor` add roughly 0.2 s to a content build and
  become a hard build dependency: a broken `.fx` fails the build, not the frame.

## Android caveat (this is the real constraint)

`mediump` has a 10-bit mantissa, so a pixel coordinate near 1280 has a spacing of
about 1.25. The finest layer is therefore the first to break: the ~1.6 px tooth
needs sub-pixel coordinates and will band or moir茅, while the 3鈥? px fibre cells
survive but jitter. The coarse mottle is effectively safe.

Consequences:

- If the analytic path ships on Android, keep only the finest layer in a small
  tiled texture (or force `highp` for the pixel shader) and leave the coarse
  layers analytic.
- A full bake is the simple fallback, but it has to come from the gradient field
  (see above), otherwise the paper pebbles.
- Scale grain with screen size instead of assuming 720p:
  `Zoom = RenderSize.y / 720`, otherwise a 4K desktop gets grain that is
  invisible and a phone gets grain that is too coarse relative to the UI.

## Three levels of use

1. **Screen-space grain pass** (what the prototype does). Replaces the final
   blit. Removes the CPU bake, the per-size cache, the 8 MB budget and the 256 px
   tile repetition, and lets grain strength follow luminance so navy stays calm
   and ivory keeps its tooth. Limit: the grain field is fixed to the screen, so a
   sheet that slides or tilts during a page transition carries its grain only
   visually 鈥?irrelevant at 卤2 % amplitude, wrong at 卤8 %.
2. **Sheet-space shading with a distance field**. Give each of the three cut
   shapes a small signed-distance mask (the CPU already builds the masks) and draw
   sheets with a `SpriteBatch`-compatible effect (`MatrixTransform` +
   premultiplied alpha). That unlocks a soft cut edge, a paper-thickness
   highlight and ink bleed 鈥?the part a baked grain can never do. Cost: per-sheet
   draw calls, a second effect variant, and it interacts with the nine-slice
   batching, so it is the expensive half.
3. **Secondary motion**: light drift across the stack, page-turn shading. Only
   worth it after level 2, and it must respect `UIMotion.Reduced`.

## Implemented

The first level is now the only path, on both targets, with no fallback:

- `Content/paper_grain.fx` is built for DesktopGL and Android by the two existing
  content projects. `UI/PaperPass.cs` draws `_pageFrame` (plus `_previousPageFrame`
  during the 200 ms crossfade) through it instead of the plain blit, so there is
  still exactly one render target per page and no extra full-screen buffer.
- `UI/UIPaper.cs` no longer bakes anything per size. Sheets are drawn as the cut
  mask tinted by the atlas palette it already computed; `SizedPaper`, its cache and
  `TrimCache` are gone, and `UI/UIPaperGrain.cs` is deleted.
- `UI/UIPaperFrame.cs` draws its silhouettes directly, so its per-size surface
  cache and the `MaskPixels` byte arrays are gone too.
- The three cut masks are generated at half the atlas resolution. A full mask is
  6 MiB each; at 2× reduction the largest sheet edge is 2 px softer and the smaller
  widgets are unaffected because their masks were already heavily downscaled.
- The flat silhouettes (`MenuPaper`, `Facet`, `Seal`) keep their 0.91/0.94 base
  luminance but no longer carry baked noise, so nothing is textured twice.

Memory and cost removed (1280×720, 1254² atlas):

| Item | Before | After |
|------|--------|-------|
| `UIPaper` per-size cache | up to 8 MiB VRAM, refilled per size | none |
| `UIPaperFrame` per-size cache | up to 8 MiB VRAM | none |
| Cut masks (3) | 18 MiB VRAM | 4.5 MiB |
| Grain tile (`UIPaperGrain`) | 256 KiB managed | none |
| Fallback paper texture | 64 KiB VRAM | 1×1 solid |
| Frame mask bytes | ~1.2 MiB managed | none |
| CPU bake | 9–10 ms per full-screen size | 0 |
| GPU | — | 0.075 ms per frame |

That is roughly 30 MiB of video memory and 1.5 MiB of managed memory, plus the
resize and page-change hitches that came from rebuilding the caches.

`tools/UiPreview` still renders every screen; the screenshots in
`artifacts/paper-baseline` (old pipeline) and `artifacts/paper-procedural` (shader)
differ by a mean of 1–11 levels per pixel, almost all of it the removed baked
grain rather than layout.

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Android GLSL precision | the ~1.6 px tooth is the first layer to band on `mediump` | grain scales with viewport height; if a device shows moiré, raise the finest cell size rather than adding a texture path |
| Fill cost on mobile GPUs | one full-screen pass at 1440p+ | 0.075 ms measured on desktop; measure on device |
| Effect is a build dependency | a broken `.fx` fails the content build | it lives in the two existing content projects, and both were rebuilt to verify |
| Grain no longer moves with sheets | subtle during transitions | amplitude is ±2 %, invisible in motion |
| Mask half-resolution | 2 px softer edge on the largest sheets | verified on the Rules poster at 4× |

Level 2 (soft cut edge, paper thickness, ink bleed) still needs a sheet-space
effect and a device test; level 1 does not block it.
