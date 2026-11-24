"""Contact sheet / atlas helpers for the Fida portrait pipeline.

  python sheet.py contact <glob-or-dir> --out sheet.png
  python sheet.py atlas   --cells a.png b.png ... --out atlas.png --cell 512
  python sheet.py test    --cells a.png ... --out test.png --cell 64
"""

from __future__ import annotations

import argparse
import glob
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont


def _font(size: int = 18):
    for name in ("consola.ttf", "arial.ttf", "segoeui.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def contact(paths, out, cols=3, cell=420, bg=(28, 30, 34)):
    if not paths:
        raise SystemExit("no input images")
    rows = (len(paths) + cols - 1) // cols
    pad, cap = 10, 26
    sheet = Image.new("RGB", (cols * (cell + pad) + pad,
                              rows * (cell + cap + pad) + pad), bg)
    draw = ImageDraw.Draw(sheet)
    font = _font(16)
    for i, path in enumerate(paths):
        im = Image.open(path).convert("RGB")
        im.thumbnail((cell, cell), Image.LANCZOS)
        x = pad + (i % cols) * (cell + pad)
        y = pad + (i // cols) * (cell + cap + pad)
        sheet.paste(im, (x + (cell - im.width) // 2, y + (cell - im.height) // 2))
        draw.text((x + 4, y + cell + 4), os.path.basename(path)[:52],
                  fill=(235, 232, 225), font=font)
    sheet.save(out)
    return out


def atlas(cells, out, cell=512, bg=(0, 0, 0, 0)):
    """Compose 6 RGBA cells into the runtime's 3 x 2 atlas."""
    if len(cells) != 6:
        raise SystemExit(f"atlas needs exactly 6 cells, got {len(cells)}")
    sheet = Image.new("RGBA", (cell * 3, cell * 2), bg)
    for i, path in enumerate(cells):
        im = Image.open(path).convert("RGBA")
        im = im.resize((cell, cell), Image.LANCZOS)
        sheet.paste(im, ((i % 3) * cell, (i // 3) * cell), im)
    sheet.save(out)
    return out


def small_test(cells, out, cell=64, bg=(12, 16, 21, 255)):
    """What the portraits actually look like at in-game size, on the UI charcoal."""
    if len(cells) != 6:
        raise SystemExit(f"test sheet needs exactly 6 cells, got {len(cells)}")
    gap = 26
    sheet = Image.new("RGBA", (6 * (cell + gap) + gap, cell + 2 * gap), bg)
    for i, path in enumerate(cells):
        im = Image.open(path).convert("RGBA")
        im = im.resize((cell, cell), Image.LANCZOS)
        sheet.paste(im, (gap + i * (cell + gap), gap), im)
    sheet.save(out)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("mode", choices=["contact", "atlas", "test"])
    ap.add_argument("inputs", nargs="*")
    ap.add_argument("--glob", default=None)
    ap.add_argument("--out", required=True)
    ap.add_argument("--cell", type=int, default=0)
    ap.add_argument("--cols", type=int, default=3)
    ap.add_argument("--bg", default=None, help="rrggbb background, e.g. F0EBDC")
    args = ap.parse_args()

    paths = list(args.inputs)
    if args.glob:
        paths = sorted(glob.glob(args.glob))
    paths = [p for p in paths if os.path.isfile(p)]

    if args.bg:
        rgb = tuple(int(args.bg[i:i + 2], 16) for i in (0, 2, 4))
        bg = (*rgb, 255)
    else:
        bg = (28, 30, 34)

    if args.mode == "contact":
        contact(paths, args.out, cols=args.cols, cell=args.cell or 420, bg=bg)
    elif args.mode == "atlas":
        atlas(paths, args.out, cell=args.cell or 512)
    else:
        small_test(paths, args.out, cell=args.cell or 64)
    print(args.out)


if __name__ == "__main__":
    main()
