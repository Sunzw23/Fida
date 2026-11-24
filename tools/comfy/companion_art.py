"""Generate the four new Fida companion portraits with the local ComfyUI.

Reuses the portrait pipeline in fida_portraits.py (Z-Image Turbo, magenta chroma
backdrop, paper-cut style) and the post-processing in build_atlas.py, so the new
busts sit in the same family as the existing roster atlas.

Usage:
  python companion_art.py                          # all four, seed 5101
  python companion_art.py --chars ila,bram         # a subset
  python companion_art.py --seeds 5101,5102        # contact sheet per seed
  python companion_art.py --out Content/portraits  # where the cut PNGs go
"""

from __future__ import annotations

import argparse
import os
import sys
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)

from fida_portraits import STYLE_PROD, build_t2i, fetch_images, submit, wait_for  # noqa: E402
from build_atlas import (bleed_edges, chroma_key, crop_to_subject, deskin,  # noqa: E402
                         drop_islands, harmonise, place_in_cell, _flood_from_border)
from PIL import Image  # noqa: E402
import numpy as np  # noqa: E402

# STYLE_PROD locks the face and skin to ivory paper. These four are human-shaped but
# not human-toned, so the skin clause is swapped for a per-character paper tone.
STYLE_COMPANION = STYLE_PROD.replace(
    "The face and skin are the same ivory paper as the costume, carved with navy creases:",
    "The face and skin are matte paper in the character's own tone (indigo blue, slate "
    "blue or ink black, described below), carved with darker creases:")

CHARACTERS = {
    "ila": (
        "Ila, an adult woman, a frost beauty. Skin: deep indigo-blue paper, flawless, "
        "no cracks and no scars. Hair: ink-black paper, long, one heavy lock falling "
        "over the left shoulder. Eyes: NO eyeballs, NO pupils, NO iris - both eyes are "
        "smooth blank pale ice slits, like frosted glass, with nothing inside them. "
        "Expression: cold, level, unreadable, mouth closed and flat, no smile. "
        "OUTFIT (deliberately simple and NOT severe): a navy off-shoulder bodice with a "
        "clean horizontal neckline below the collarbones, leaving both shoulders and "
        "the collarbones bare; fitted ivory sleeves starting on the upper arm; a narrow "
        "ivory band at the waist; no high collar, no jewellery, no cape, no lacing. "
        "Tasteful, fully clothed, nothing revealing beyond bare shoulders. "
        "FRAMING: plain chest-up bust, front-on, shoulders square, cut at the chest. "
        "MEDIUM: hand-cut paper planes with hard cut edges, visible paper fibre, layered "
        "flat colour, matte screen-print texture, not smooth vector art. "
        "SIGNATURE: deep indigo skin, long ink-black hair over one shoulder, bare "
        "shoulders, blank ice eyes, cold stillness."
    ),    "bram": (
        "Bram, an adult man the shape of a person but not human-toned: a broad square "
        "head, close-cropped iron-grey paper hair, a short squared beard of pale ash, "
        "heavy brows, a weathered angular face. His skin is pale slate-blue paper, "
        "like cold stone, with fine darker creases around the eyes and mouth. His eyes "
        "are flat ivory lamps with no visible pupil, calm and old. He wears a heavy "
        "navy gate cloak with a tall folded collar over slab shoulders, a plain iron "
        "chain hanging once across the chest, a scarred leather strap. Immovable, "
        "faintly tired, human-shaped and clearly not made of flesh. "
        "SIGNATURE: slate-blue stone skin, pupil-less ivory eyes, grey beard, the gate "
        "cloak and chain."
    ),
    "sable": (
        "Sable, a young androgynous archivist the shape of a person but not "
        "human-toned: a narrow angular face, a straight ink-black bob with a long "
        "fringe over one eye, an unreadable calm mouth, one eye visible and the other "
        "in shadow. Their skin is warm ivory paper veined with fine ink-blue cracks "
        "that spread from the temples and the backs of the hands, as if the page were "
        "cracking; the visible eye is solid ink with a pale rim. They wear an "
        "oversized navy hooded coat open over a high ivory collar, the hood folded "
        "back, fingers stained with ink. Human-shaped, still, watchful, and clearly "
        "not flesh. "
        "SIGNATURE: ivory paper skin with ink-crack veins, one solid ink eye, the "
        "folded hood."
    ),
    "ink_echo": (
        "The Ink Echo, a summon with a human silhouette and no face at all: a slender "
        "hooded adult figure whose skin is solid ink-black paper, matte and "
        "light-absorbing, with a few faint navy-blue highlights along the folds. The "
        "hood is drawn forward; the face is a smooth blank black oval with one thin "
        "horizontal ivory light floating where the eyes would be, no nose, no mouth. "
        "Its hands are black paper with thin brush-tip fingers; the lower edge of the "
        "coat frays into a few ink strands. Human-shaped but an absence wearing a "
        "shape. "
        "SIGNATURE: a faceless ink-black human silhouette with a single horizontal "
        "light."
    ),
}


def key_border(rgba: Image.Image, tolerance: float = 20.0) -> Image.Image:
    """Fallback for renders whose backdrop came out beige/ivory instead of magenta.

    Samples the frame, treats pixels within `tolerance` as backdrop candidates and
    removes only the candidates reachable from the border, so an ivory costume is
    never punched through.
    """
    arr = np.asarray(rgba).copy()
    if arr[0, 0, 3] == 0:
        return rgba
    rgb = arr[..., :3].astype(np.float32)
    h, w = rgb.shape[:2]
    # Renders sometimes put an inset beige panel inside a white margin: sample both
    # the frame and a 5% inset, then key every candidate colour reachable from the
    # border. A costume that shares the ivory tone never touches the border.
    samples = [rgb[2, 2], rgb[2, w - 3], rgb[h - 3, 2], rgb[h - 3, w - 3]]
    inset = max(4, int(min(h, w) * 0.05))
    samples += [rgb[inset, inset], rgb[inset, w - inset - 1],
                rgb[h - inset - 1, inset], rgb[h - inset - 1, w - inset - 1]]
    mask = np.zeros((h, w), dtype=bool)
    for colour in samples:
        mask |= np.linalg.norm(rgb - colour, axis=2) < tolerance
    reach = _flood_from_border(mask)
    arr[..., 3] = np.where(reach, 0, arr[..., 3])
    return Image.fromarray(arr, "RGBA")


def cut_out(raw_path: str, cell: int, strength: float) -> Image.Image:
    rgba = chroma_key(Image.open(raw_path).convert("RGB"))
    rgba = key_border(rgba)
    rgba = bleed_edges(rgba)
    rgba = drop_islands(rgba)
    rgba = crop_to_subject(rgba)
    rgba = deskin(rgba)
    rgba = harmonise(rgba, strength)
    return place_in_cell(rgba, cell, 1.0)

def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--chars", default="ila,bram,sable,ink_echo")
    ap.add_argument("--seeds", default="5101")
    ap.add_argument("--out", default=os.path.join("Content", "portraits"))
    ap.add_argument("--cell", type=int, default=1024)
    ap.add_argument("--strength", type=float, default=0.30)
    ap.add_argument("--raw", default=os.path.join("artifacts", "companions-comfy", "raw"))
    ap.add_argument("--reprocess", action="store_true", help="cut out existing raws instead of generating")
    args = ap.parse_args()

    out_dir = os.path.join(ROOT, args.out)
    raw_dir = os.path.join(ROOT, args.raw)
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(raw_dir, exist_ok=True)
    seeds = [int(part) for part in args.seeds.split(",") if part.strip()]
    client = uuid.uuid4().hex

    for key in [part.strip() for part in args.chars.split(",") if part.strip()]:
        design = CHARACTERS.get(key)
        if design is None:
            raise SystemExit(f"unknown character: {key}")
        prompt = STYLE_COMPANION + " THE CHARACTER: " + design
        for seed in seeds:
            stem = f"{key}_s{seed}"
            if args.reprocess:
                raw = os.path.join(raw_dir, f"{stem}.png")
                if not os.path.exists(raw):
                    raise SystemExit(f"missing raw render: {raw}")
            else:
                graph = build_t2i(prompt, seed, prefix=f"fida_{key}")
                prompt_id = submit(graph, client)
                entry = wait_for(prompt_id)
                raws = fetch_images(entry, raw_dir, stem)
                if not raws:
                    raise RuntimeError(f"no image returned for {stem}")
                raw = raws[0]
            cut = cut_out(raw, args.cell, args.strength)
            target = os.path.join(out_dir, f"{key}.png" if len(seeds) == 1
                                  else f"{key}_s{seed}.png")
            cut.save(target)
            print(f"{stem}: {raw} -> {target}")


if __name__ == "__main__":
    main()
