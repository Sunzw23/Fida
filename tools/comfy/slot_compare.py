"""Three-way comparison for a single atlas slot.

  python slot_compare.py --labels old,mid,new --cell 200 --out c.png old.png mid.png new.png
"""

from __future__ import annotations

import argparse
import os

from PIL import Image, ImageDraw, ImageFont

CHARCOAL = (12, 16, 21, 255)
INK = (200, 204, 210)


def font(size: int):
    for name in ("consola.ttf", "arial.ttf", "segoeui.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def crop_cell(atlas_path: str, index: int) -> Image.Image:
    """Pull one cell out of a 3 x 2 atlas."""
    im = Image.open(atlas_path).convert("RGBA")
    cw, ch = im.width // 3, im.height // 2
    return im.crop(((index % 3) * cw, (index // 3) * ch, (index % 3) * cw + cw,
                    (index // 3) * ch + ch))


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("images", nargs="+")
    ap.add_argument("--labels", default="")
    ap.add_argument("--out", required=True)
    ap.add_argument("--cell", type=int, default=200)
    ap.add_argument("--cell-index", type=int, default=-1,
                    help="slice this slot out of each 3x2 atlas instead of using whole files")
    args = ap.parse_args()

    labels = args.labels.split(",") if args.labels else [""] * len(args.images)
    cell = args.cell
    pad, cap = 16, 26

    tiles = []
    for path in args.images:
        raw = Image.open(path).convert("RGBA")
        is_atlas = abs(raw.width / raw.height - 1.5) < 0.05
        im = crop_cell(path, args.cell_index) if (args.cell_index >= 0 and is_atlas) else raw
        tiles.append(im.resize((cell, cell), Image.LANCZOS))

    w = pad + len(tiles) * (cell + pad)
    h = pad + cell + cap + pad
    sheet = Image.new("RGBA", (w, h), CHARCOAL)
    draw = ImageDraw.Draw(sheet)
    f = font(17)
    for i, t in enumerate(tiles):
        x = pad + i * (cell + pad)
        sheet.paste(t, (x, pad), t)
        if i < len(labels) and labels[i]:
            draw.text((x + 2, pad + cell + 4), labels[i], fill=INK, font=f)
    sheet.save(args.out)
    print(f"{args.out}  {sheet.width}x{sheet.height}")


if __name__ == "__main__":
    main()
