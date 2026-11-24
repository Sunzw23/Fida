"""Preview driver: key + normalise cells and render them at real in-game sizes.

  python preview.py <cell.png>... --scale 1,1,1,1,1,1 --sizes 64,120,300
"""

from __future__ import annotations

import argparse
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_atlas as B  # noqa: E402

CHARCOAL = (12, 16, 21, 255)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("sources", nargs="+")
    ap.add_argument("--out-dir", default="artifacts/portraits-comfy/preview")
    ap.add_argument("--cell", type=int, default=512)
    ap.add_argument("--sizes", default="64,120,320")
    ap.add_argument("--strength", type=float, default=0.30)
    ap.add_argument("--scale", default=None)
    args = ap.parse_args()

    factors = ([float(x) for x in args.scale.split(",")]
               if args.scale else [1.0] * len(args.sources))
    if len(factors) == 1:
        factors = factors * len(args.sources)
    if len(factors) != len(args.sources):
        raise SystemExit("--scale needs one value per source")

    os.makedirs(args.out_dir, exist_ok=True)
    cells = []
    for factor, path in zip(factors, args.sources):
        c = B.process(path, args.cell, args.strength, factor)
        stem = os.path.splitext(os.path.basename(path))[0]
        c.save(os.path.join(args.out_dir, f"{stem}-cell.png"))
        cells.append((stem, c))
        print(f"{stem}: factor={factor} bbox={c.getbbox()}")

    for size in [int(s) for s in args.sizes.split(",")]:
        pad = max(8, size // 8)
        w = pad + len(cells) * (size + pad)
        sheet = Image.new("RGBA", (w, size + 2 * pad), CHARCOAL)
        for i, (_, c) in enumerate(cells):
            t = c.resize((size, size), Image.LANCZOS)
            sheet.paste(t, (pad + i * (size + pad), pad), t)
        out = os.path.join(args.out_dir, f"sheet-{size}.png")
        sheet.save(out)
        print(out)


if __name__ == "__main__":
    main()
