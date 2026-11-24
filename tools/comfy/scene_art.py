"""Landscape strips for the outcome screens.

The victory and defeat screens each paste a soft, painterly rectangle into the card: the
victory one is `paper/victory_road_concept.png`, the defeat one is a cell of the chapter
atlas. Neither matches the flat paper-cut language used everywhere else, and both read as
rectangles sitting on a panel rather than scenery in the card.

These strips are generated the same way the portraits are - flat planes, two materials,
chroma backdrop - and keyed to transparency so the empty sky lets the panel show through
and the silhouette merges into the card instead of ending on a hard edge.

  python scene_art.py generate --scenes victory_road,defeat_ground --seeds 1001,1002
  python scene_art.py build --sources v1.png v1b.png ... 
"""

from __future__ import annotations

import argparse
import os
import sys
import time
import uuid

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_atlas as B  # noqa: E402
import fida_portraits as F  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT_ROOT = os.path.join(ROOT, "artifacts", "scene-art")
RAW_DIR = os.path.join(OUT_ROOT, "raw")

WIDTH, HEIGHT = 1536, 384   # the Outcome/Scene band is 1350 x 330 in design space, ~4.1:1

STYLE_SCENE = (
    "Flat cut-paper landscape, a paper silhouette diorama built from large angular planes. "
    "Every shape is one absolutely flat matte fill; no gradient inside a shape, no atmospheric haze, "
    "no glow, no bloom, no airbrush, no photographic depth of field. Depth comes only from three flat "
    "tonal steps between layers. Hard straight cut edges, the crisp edge of a blade through paper. "
    "Palette strictly deep desaturated navy blue, mid slate navy, near-black and warm ivory cream. "
    "Large shapes, but three or four overlapping silhouette layers stepping back into distance, each "
    "layer a single flat tone, depth carried only by the flat step between layers - never by haze, "
    "fog, gradient or soft focus. Quiet, monumental, empty of people. "
    "Composed as a wide panoramic band about four times wider than it is tall. The dark land masses "
    "on the left and right rise toward the top edge of the frame and meet it only in a few sharp "
    "peaks, never in a long flat plateau, so the top edge of the silhouette is always jagged. The "
    "middle stays low and open. Nothing is cropped by a border. "
    "The entire sky is one single uniform flat pure magenta chroma-key field, exactly #FF00FF, "
    "covering the whole upper area behind the land, with no gradient, no clouds, no stars, no texture. "
    "No text, no letters, no numerals, no frame, no border, no vignette, no people, no animals, "
    "no photorealism, no painting, no oil paint, no watercolour."
)

SCENES = {
    "victory_road": (
        "A road still ahead after a won battle. Three overlapping layers of dark navy folded paper "
        "ridges step back into the distance, each layer one flat tone lighter or darker than the one "
        "in front. The front layer rises to the full height of the frame at the left and right edges; "
        "the far layers sit lower in the middle so a narrow gap of sky shows above the saddle. A "
        "single smooth pale ivory paper road winds in a shallow S curve from the bottom centre away to "
        "the horizon, with no markings of any kind on it. On the far ridge stands a small pale ivory "
        "castle of five thin spires. One flat pale ivory moon disc sits in the sky gap above the saddle. "
        "Calm, cold, open."
    ),
    "defeat_ground": (
        "The ground where the duel was lost. A tall half-collapsed pale ivory paper arch stands just "
        "left of centre and reaches nearly to the top edge, its top broken off at an angle. Two snapped "
        "navy columns lean beside it and one tall leaning tower stump fills the right edge of the frame "
        "from top to bottom. Dark rubble planes stack across the bottom of the frame in three flat steps. "
        "No road, no bright light, no moon. Most masses are nearly black. Silent, broken, absolute."
    ),
}


def prompt_for(name: str) -> str:
    return f"Paper-cut landscape illustration: {SCENES[name]} {STYLE_SCENE}"


def generate(names: list[str], seeds: list[int]) -> int:
    client_id = str(uuid.uuid4())
    results = []
    total = len(names) * len(seeds)
    n = 0
    for name in names:
        prompt = prompt_for(name)
        for seed in seeds:
            n += 1
            stem = f"{name}-s{seed}"
            sys.stdout.write(f"[{n}/{total}] {stem} ... ")
            sys.stdout.flush()
            t0 = time.time()
            try:
                graph = F.build_t2i(prompt, seed, width=WIDTH, height=HEIGHT,
                                    prefix=f"fida_scene_{name}")
                pid = F.submit(graph, client_id)
                entry = F.wait_for(pid)
                files = F.fetch_images(entry, RAW_DIR, stem)
                print(f"ok {time.time() - t0:.1f}s")
                results.append({"stem": stem, "files": files})
            except Exception as exc:  # noqa: BLE001
                print(f"FAILED: {exc}")
                results.append({"stem": stem, "error": str(exc)})
    return 0 if all("error" not in r for r in results) else 1


def build(name: str, source: str, out: str, strength: float) -> None:
    img = Image.open(source).convert("RGB")
    # Landscapes need enclosed backdrop removed (an arch opening is fully surrounded by paper),
    # a low island floor (a moon disc is a legitimate separate shape) and no real saturation
    # floor: the model sometimes renders the key colour as a very pale pink (~0.10 saturation),
    # which is no more saturated than the ivory paper. Hue alone is safe here because a
    # navy/ivory landscape contains no pink at all - unlike a portrait, where lips do.
    rgba = B.chroma_key(img, include_enclosed=True, min_sat=0.05)
    rgba = B.bleed_edges(rgba)
    rgba = B.drop_islands(rgba, keep_frac=0.0, keep_min_cells=6)
    rgba = B.crop_to_subject(rgba)
    rgba = B.harmonise(rgba, strength)
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    rgba.save(out)
    print(f"{out}  {rgba.width}x{rgba.height}  {os.path.getsize(out)} bytes")


def preview(sources: list[str], out: str, card: tuple[int, int, int],
            band: tuple[int, int] = (1350, 330)) -> None:
    """Simulate UIPaperOutcome: trim, uniform fit, bottom centred inside the Scene band."""
    bw, bh = band
    sheet = Image.new("RGB", (bw + 40, (bh + 40) * len(sources)), card)
    for i, path in enumerate(sources):
        im = Image.open(path).convert("RGBA")
        bbox = im.getbbox()
        if bbox:
            im = im.crop(bbox)
        fit = min(bw / im.width, bh / im.height)
        im = im.resize((max(1, int(im.width * fit)), max(1, int(im.height * fit))),
                       Image.LANCZOS)
        x = (bw - im.width) // 2 + 20
        y = (i + 1) * (bh + 40) - 20 - im.height
        sheet.paste(im, (x, y), im)
    sheet.save(out)
    print(f"{out}  {sheet.width}x{sheet.height}")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("mode", choices=["generate", "build", "preview"])
    ap.add_argument("--scenes", default=",".join(SCENES))
    ap.add_argument("--seeds", default="1001,1002,1003")
    ap.add_argument("--source", default=None, help="build: one raw render")
    ap.add_argument("--sources", nargs="*", default=[], help="preview: keyed strips")
    ap.add_argument("--out", default=None, help="build/preview destination png")
    ap.add_argument("--strength", type=float, default=0.35)
    args = ap.parse_args()

    if args.mode == "generate":
        names = [s for s in args.scenes.split(",") if s]
        seeds = [int(s) for s in args.seeds.split(",") if s]
        return generate(names, seeds)

    if args.mode == "preview":
        if not args.out or not args.sources:
            raise SystemExit("preview needs --sources and --out")
        preview(args.sources, args.out, (21, 45, 69))
        return 0

    if not args.source or not args.out:
        raise SystemExit("build needs --source and --out")
    build(args.scenes, args.source, args.out, args.strength)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
