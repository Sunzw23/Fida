"""Report backdrop colour statistics so the chroma key threshold is set from data."""

from __future__ import annotations

import sys

import numpy as np
from PIL import Image


def main() -> None:
    for path in sys.argv[1:]:
        im = Image.open(path).convert("HSV")
        arr = np.asarray(im, dtype=np.float32)
        h, w = arr.shape[:2]
        band = np.concatenate([
            arr[:24].reshape(-1, 3), arr[-24:].reshape(-1, 3),
            arr[:, :24].reshape(-1, 3), arr[:, -24:].reshape(-1, 3),
        ])
        hue = band[:, 0] * 360.0 / 255.0
        sat = band[:, 1] / 255.0
        val = band[:, 2] / 255.0
        print(f"{path.split(chr(92))[-1]}")
        print(f"  backdrop hue  p10/p50/p90 = {np.percentile(hue,10):6.1f} "
              f"{np.percentile(hue,50):6.1f} {np.percentile(hue,90):6.1f}")
        print(f"  backdrop sat  p10/p50/p90 = {np.percentile(sat,10):6.3f} "
              f"{np.percentile(sat,50):6.3f} {np.percentile(sat,90):6.3f}")
        print(f"  backdrop val  p10/p50/p90 = {np.percentile(val,10):6.3f} "
              f"{np.percentile(val,50):6.3f} {np.percentile(val,90):6.3f}")


if __name__ == "__main__":
    main()
