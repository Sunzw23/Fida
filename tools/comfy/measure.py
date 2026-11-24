"""Print per-cell geometry so the atlas scale can be calibrated instead of guessed."""

from __future__ import annotations

import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_atlas as B  # noqa: E402


def main() -> None:
    for path in sys.argv[1:]:
        img = Image.open(path).convert("RGB")
        rgba = B.chroma_key(img)
        rgba = B.drop_islands(rgba)
        sub = B.crop_to_subject(rgba)
        unit = B.head_unit(sub)
        print(f"{os.path.basename(path):34s} sub={sub.width:4d}x{sub.height:<4d} "
              f"head_unit={unit:6.1f}  headfrac_of_h={unit / sub.height:5.3f}")


if __name__ == "__main__":
    main()
