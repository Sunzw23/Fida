"""Before/after comparison for the portrait atlas, rendered on the UI charcoal.

  python compare.py --before old.png --after new.png --out compare.png --size 96
"""

from __future__ import annotations

import argparse

from PIL import Image

CHARCOAL = (12, 16, 21, 255)
LABEL = (150, 156, 165, 255)


def split(atlas: Image.Image, cols: int = 3, rows: int = 2) -> list[Image.Image]:
    cw = atlas.width // cols
    ch = atlas.height // rows
    return [atlas.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
            for r in range(rows) for c in range(cols)]


def strip(cells: list[Image.Image], size: int, pad: int) -> Image.Image:
    w = pad + len(cells) * (size + pad)
    out = Image.new("RGBA", (w, size + 2 * pad), CHARCOAL)
    for i, c in enumerate(cells):
        t = c.resize((size, size), Image.LANCZOS)
        out.paste(t, (pad + i * (size + pad), pad), t)
    return out


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--before", required=True)
    ap.add_argument("--after", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--sizes", default="64,128")
    args = ap.parse_args()

    before = split(Image.open(args.before).convert("RGBA"))
    after = split(Image.open(args.after).convert("RGBA"))

    sizes = [int(s) for s in args.sizes.split(",")]
    strips = []
    for size in sizes:
        pad = max(10, size // 8)
        strips.append(strip(before, size, pad))
        strips.append(strip(after, size, pad))

    gap = 14
    width = max(s.width for s in strips)
    height = sum(s.height for s in strips) + gap * (len(strips) - 1)
    sheet = Image.new("RGBA", (width, height), CHARCOAL)
    y = 0
    for s in strips:
        sheet.paste(s, ((width - s.width) // 2, y), s)
        y += s.height + gap
    sheet.save(args.out)
    print(f"{args.out}  {sheet.width}x{sheet.height}")


if __name__ == "__main__":
    main()
