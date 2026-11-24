"""Save a zoomed crop so small artifacts can be judged without a huge image read.

  python crop.py <image> --box 300,300,650,650 --zoom 2.5 --out crop.png [--on F0EBDC]
"""

from __future__ import annotations

import argparse

from PIL import Image


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("image")
    ap.add_argument("--box", required=True, help="left,top,right,bottom")
    ap.add_argument("--zoom", type=float, default=2.0)
    ap.add_argument("--out", required=True)
    ap.add_argument("--on", default=None, help="rrggbb composite colour")
    args = ap.parse_args()

    im = Image.open(args.image).convert("RGBA")
    box = tuple(int(v) for v in args.box.split(","))
    im = im.crop(box)
    w = max(1, int(im.width * args.zoom))
    h = max(1, int(im.height * args.zoom))
    im = im.resize((w, h), Image.NEAREST)

    if args.on:
        rgb = tuple(int(args.on[i:i + 2], 16) for i in (0, 2, 4))
        base = Image.new("RGBA", im.size, (*rgb, 255))
        base.alpha_composite(im)
        im = base

    im.convert("RGB").save(args.out)
    print(f"{args.out}  {im.width}x{im.height}")


if __name__ == "__main__":
    main()
