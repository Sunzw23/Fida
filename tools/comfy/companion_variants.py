"""Generate labelled variant sheets for companion redesigns (Ila, Sable).

Each entry renders one candidate at 1024, cuts it out with the shared paper-cut
pipeline, then a 2x2 contact sheet with letter labels is assembled so a human can
pick a direction without opening eight files.

  python companion_variants.py --chars ila,sable --out artifacts/companion-variants
"""

from __future__ import annotations

import argparse
import os
import sys
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)

from PIL import Image, ImageDraw, ImageFont  # noqa: E402

from companion_art import STYLE_COMPANION, cut_out  # noqa: E402
from fida_portraits import build_t2i, fetch_images, submit, wait_for  # noqa: E402

ILA_COMMON = (
    "Ila, an adult woman, a frost beauty. Skin: deep indigo-blue paper, flawless, "
    "absolutely no cracks, no scars, no repair lines on the face. Hair: long, past the "
    "shoulders, completely still. Eyes: NO eyeballs, NO pupils, NO iris - both eyes are "
    "smooth blank pale ice slits, like frosted glass, with nothing inside them. "
    "Expression: cold, level, unreadable, mouth closed and flat, no smile. "
    "FRAMING: plain chest-up bust, front-on, shoulders square, cut at the chest. "
    "MEDIUM: hand-cut paper planes with hard cut edges, visible paper fibre, layered "
    "flat colour, matte screen-print texture, not smooth vector art. "
)

SABLE_COMMON = (
    "Sable, an archive wraith in human shape, hooded, with ONE eye only. "
    "Hood: an oversized soft navy paper hood drawn up over the head, casting a deep "
    "shadow. Face: exactly one eye - no second eye at all, no eyebrows over the empty "
    "half. Expression: still, watchful, neither friendly nor hostile. "
    "FRAMING: plain chest-up bust, front-on, cut at the chest. "
    "MEDIUM: hand-cut paper planes with hard cut edges, visible paper fibre, layered "
    "flat colour, matte screen-print texture, not smooth vector art. "
)

VARIANTS = {
    "ila-a": ILA_COMMON + (
        "Hair colour: warm ivory white, long and straight, parted in the middle. "
        "Outfit: a high navy paper collar with one dark round clasp, over a plain ivory "
        "shoulder yoke. Simple, three shapes only."
    ),
    "ila-b": ILA_COMMON + (
        "Hair colour: silver-blue, long and smooth, tucked behind both ears. "
        "Outfit: a deep navy high-necked gown with a narrow ivory standing collar, no "
        "clasp, no jewellery. Simple, two shapes only."
    ),
    "ila-c": ILA_COMMON + (
        "Hair colour: ink-black, long, one heavy lock falling over the left shoulder. "
        "Outfit: an ivory high collar and a plain deep-navy bodice, one thin seam. "
        "Simple, uncluttered."
    ),
    "ila-d": ILA_COMMON + (
        "Hair colour: frost white, very long and straight, centre-parted, falling on "
        "both sides of the neck. Outfit: a plain navy high-neck dress with an ivory "
        "collar band, nothing else. Simple and austere."
    ),
    "sable-a": SABLE_COMMON + (
        "Skin: solid ink-black paper. Eye: one large pale ivory eye centred in the face, "
        "calm and luminous; a thin flat mouth below. Collar: a ruff of fanned ivory "
        "paper pages around the shoulders."
    ),
    "sable-b": SABLE_COMMON + (
        "Skin: warm ivory page paper. Eye: one long narrow eye on the left side of the "
        "face, glowing pale; the right half of the face is smooth blank paper. Collar: "
        "a simple navy cowl with two page corners."
    ),
    "sable-c": SABLE_COMMON + (
        "Skin: deep navy ink paper. Eye: one single vertical slit of pale ivory light in "
        "the centre of the face, no round eye at all; no mouth. Collar: a high navy "
        "collar, no ruff."
    ),
    "sable-d": SABLE_COMMON + (
        "Skin: ink-black paper with faint blue sheen. Eye: one round pale eye floating "
        "in the hood shadow, no other features. Collar: a loose ivory page collar, one "
        "corner torn."
    ),
}


def label_font(size: int = 28):
    for name in ("arial.ttf", "segoeui.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def sheet(paths: list[tuple[str, str]], out: str, cell: int = 512) -> None:
    """2x2 contact sheet with a letter under each bust."""
    gap = 12
    label_h = 44
    cols = 2
    rows = (len(paths) + cols - 1) // cols
    width = cols * cell + (cols + 1) * gap
    height = rows * (cell + label_h) + (rows + 1) * gap
    canvas = Image.new("RGB", (width, height), (18, 24, 32))
    draw = ImageDraw.Draw(canvas)
    font = label_font()
    for index, (label, path) in enumerate(paths):
        image = Image.open(path).convert("RGBA")
        image.thumbnail((cell, cell), Image.LANCZOS)
        x = gap + (index % cols) * (cell + gap)
        y = gap + (index // cols) * (cell + label_h + gap)
        px = x + (cell - image.width) // 2
        py = y + cell - image.height
        canvas.paste(image, (px, py), image)
        draw.text((x + cell // 2, y + cell + 6), label, fill=(237, 232, 219), font=font,
                  anchor="ma")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    canvas.save(out)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--chars", default="ila,sable")
    ap.add_argument("--out", default=os.path.join("artifacts", "companion-variants"))
    ap.add_argument("--cell", type=int, default=1024)
    ap.add_argument("--seed-base", type=int, default=9400)
    args = ap.parse_args()

    out_dir = os.path.join(ROOT, args.out)
    raw_dir = os.path.join(out_dir, "raw")
    os.makedirs(raw_dir, exist_ok=True)
    client = uuid.uuid4().hex
    groups: dict[str, list[tuple[str, str]]] = {}

    for prefix in [part.strip() for part in args.chars.split(",") if part.strip()]:
        keys = [key for key in VARIANTS if key.startswith(prefix + "-")]
        groups[prefix] = []
        for offset, key in enumerate(sorted(keys)):
            seed = args.seed_base + offset
            graph = build_t2i(STYLE_COMPANION + " THE CHARACTER: " + VARIANTS[key], seed,
                              prefix=f"fida_{key}")
            prompt_id = submit(graph, client)
            entry = wait_for(prompt_id)
            raws = fetch_images(entry, raw_dir, key)
            if not raws:
                raise RuntimeError(f"no image for {key}")
            cut = cut_out(raws[0], args.cell, 0.30)
            path = os.path.join(out_dir, f"{key}.png")
            cut.save(path)
            letter = key.rsplit("-", 1)[1].upper()
            groups[prefix].append((letter, path))
            print(f"{key}: {path}")

    for prefix, items in groups.items():
        sheet_path = os.path.join(out_dir, f"{prefix}-sheet.png")
        sheet(items, sheet_path)
        print(f"{prefix} sheet: {sheet_path}")


if __name__ == "__main__":
    main()
