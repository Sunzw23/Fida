"""Turn ComfyUI renderings into the runtime 3 x 2 portrait atlas.

Pipeline per cell:
  1. chroma-key the magenta backdrop (hue based, safe against navy/ivory),
  2. close pinholes, drop stray islands, erode 1px to kill colour spill,
  3. softly harmonise the palette toward the cold-numeric swatches,
  4. bottom-anchor the bust in a square cell (the runtime draws each cell
     bottom-centred, aspect-fit, so the cut must sit on the cell edge),
  5. emit straight-alpha RGBA PNG. MGCB premultiplies it (PremultiplyAlpha=True),
     so a hard matte edge stays clean on the charcoal background.

Usage:
  python build_atlas.py --out artifacts/portraits-comfy/atlas.png \
      --cell 512 arith.png mara.png sentinel.png king.png warden.png echo.png
"""

from __future__ import annotations

import argparse
import os

import numpy as np
from PIL import Image, ImageFilter

# Cold-numeric swatches (Doc/ColdNumericUI.md: charcoal #0C1015, bone #EDECE5),
# extended to the two paper materials the portrait set is built from.
PALETTE = [
    (243, 235, 217),  # ivory      F3EBD9
    (217, 209, 191),  # ash        D9D1BF
    (176, 168, 152),  # ash deep   B0A898
    (52, 80, 110),    # navy mid   34506E
    (34, 58, 90),     # navy       223A5A
    (21, 38, 58),     # deep navy  15263A
    (10, 20, 32),     # ink        0A1420
]


def load_rgb(path: str) -> np.ndarray:
    return np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)


def _flood_from_border(binary: np.ndarray) -> np.ndarray:
    """Pixels of `binary` reachable from the image border by 4-connectivity."""
    h, w = binary.shape
    reach = np.zeros_like(binary, dtype=bool)
    stack: list[tuple[int, int]] = []
    for x in range(w):
        for y in (0, h - 1):
            if binary[y, x] and not reach[y, x]:
                reach[y, x] = True
                stack.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if binary[y, x] and not reach[y, x]:
                reach[y, x] = True
                stack.append((y, x))
    while stack:
        cy, cx = stack.pop()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = cy + dy, cx + dx
            if 0 <= ny < h and 0 <= nx < w and binary[ny, nx] and not reach[ny, nx]:
                reach[ny, nx] = True
                stack.append((ny, nx))
    return reach


def chroma_key(img: Image.Image, include_enclosed: bool = False,
               min_sat: float = 0.28) -> Image.Image:
    """Key the magenta backdrop out of an RGB image, returning RGBA.

    The backdrop is a magenta gradient (hue ~330 deg, saturation 0.48-0.60) whose value drifts
    from 0.88 down to 0.36, so any single brightness threshold either misses a dark corner or
    eats a warm shadow. Two rules instead:

      * hue 305-358 with saturation above 0.28 is a *candidate* backdrop pixel, and
      * only candidates connected to the frame border are actually removed.

    Connectivity is what makes it safe on portraits: mouth and cheek tones never touch the
    border, so they survive, while every part of the backdrop does. Computed on a quarter-scale
    grid for speed and then dilated back up; leftover 1-3 px spill is repaired by bleed_edges.

    `include_enclosed` also removes strongly saturated backdrop that is NOT border connected.
    A landscape needs this - the opening under an arch is perfectly enclosed by paper, but it
    is still backdrop and must become transparent. Portraits keep the flag off because a lip
    can read as a saturated warm tone.
    """
    hsv = np.asarray(img.convert("HSV"), dtype=np.float32)
    hue = hsv[..., 0] * (360.0 / 255.0)
    sat = hsv[..., 1] / 255.0

    in_range = (hue >= 275.0) & (hue <= 358.0)
    cand = in_range & (sat > min_sat)
    if not cand.any():
        rgba = img.convert("RGBA")
        return rgba

    step = 4
    small = cand[::step, ::step]
    reach = _flood_from_border(small)
    up = Image.fromarray(reach.astype(np.uint8) * 255, mode="L")
    up = up.resize((img.width, img.height), Image.NEAREST)
    up = up.filter(ImageFilter.MaxFilter(9))
    bg = cand & (np.asarray(up) > 127)
    if include_enclosed:
        bg = bg | (in_range & (sat > 0.45))

    subject = Image.fromarray((~bg).astype(np.uint8) * 255, mode="L")
    # Close pinholes and pinhole clusters, then take back a hair to remove spill.
    subject = subject.filter(ImageFilter.MaxFilter(5))
    subject = subject.filter(ImageFilter.MinFilter(5))
    subject = subject.filter(ImageFilter.MaxFilter(3))
    subject = subject.filter(ImageFilter.MinFilter(3))
    subject = subject.filter(ImageFilter.MinFilter(3))   # 1px erosion

    alpha = subject.filter(ImageFilter.GaussianBlur(0.6))
    rgba = img.convert("RGBA")
    rgba.putalpha(alpha)
    return rgba


def drop_islands(rgba: Image.Image, min_frac: float = 0.004,
                 keep_frac: float = 0.08, keep_min_cells: int = 0) -> Image.Image:
    """Remove disconnected specks left over from the key.

    `keep_frac` is the share of the largest component a part must reach to be kept, so a
    portrait bust (one big shape) keeps only itself at 0.08. A landscape has legitimate
    separate pieces - a moon disc is a few hundred pixels against a land mass of tens of
    thousands - so scenes pass `keep_min_cells`, an absolute floor in quarter-scale cells,
    which keeps the moon while still dropping one-cell dust.

    An earlier version also kept every pixel the mask did *not* call silhouette in order to
    protect the body, which meant faint crumbs (alpha 10-60) were kept too and showed up as
    grey dust on a flat UI panel. Faint antialiasing next to the bust is safe because the kept
    component is dilated before the alpha is gated.
    """
    arr = np.asarray(rgba)
    alpha = arr[..., 3]
    h, w = alpha.shape
    mask = alpha > 60
    if not mask.any():
        return rgba

    # Label components on a quarter-scale grid; the bust is the only large one.
    step = 4
    grid = mask[::step, ::step]
    seen = np.zeros_like(grid, dtype=bool)
    ys, xs = np.nonzero(grid)
    comps: list[list[tuple[int, int]]] = []
    for sy, sx in zip(ys, xs):
        if seen[sy, sx]:
            continue
        stack = [(sy, sx)]
        seen[sy, sx] = True
        comp = []
        while stack:
            cy, cx = stack.pop()
            comp.append((cy, cx))
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ny, nx = cy + dy, cx + dx
                if 0 <= ny < grid.shape[0] and 0 <= nx < grid.shape[1] \
                        and grid[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    stack.append((ny, nx))
        comps.append(comp)
    if not comps:
        return rgba

    largest = max(len(c) for c in comps)
    floor = max(largest * keep_frac, float(keep_min_cells))
    keep = np.zeros_like(grid, dtype=bool)
    for comp in comps:
        if len(comp) >= floor:      # generous enough for a detached cape corner or a moon
            for cy, cx in comp:
                keep[cy, cx] = True

    keep_full = np.kron(keep, np.ones((step, step), dtype=bool))[:h, :w]
    keep_full = np.asarray(Image.fromarray(keep_full.astype(np.uint8) * 255)
                           .filter(ImageFilter.MaxFilter(13))) > 60
    new_alpha = np.where(keep_full, alpha, 0).astype(np.uint8)
    if new_alpha.sum() < alpha.sum() * min_frac:
        return rgba
    out = arr.copy()
    out[..., 3] = new_alpha
    return Image.fromarray(out, mode="RGBA")


IVORY_TONE = np.array([243, 235, 217], dtype=np.float32)
ASH_TONE = np.array([196, 187, 172], dtype=np.float32)


def deskin(rgba: Image.Image) -> Image.Image:
    """Repaint any peach/skin pixels as ivory paper.

    Only Arith and Mara expose a face, and both sit in the same two-material palette as
    everyone else, so skin must read as carved ivory, not flesh. Peach is separable from
    ivory by saturation (peach ~0.30, warm ivory ~0.11) at a similar hue, so the rule is
    hue 8-48 deg and saturation above 0.17.
    """
    rgb = Image.fromarray(np.asarray(rgba)[..., :3], mode="RGB")
    hsv = np.asarray(rgb.convert("HSV"), dtype=np.float32)
    hue = hsv[..., 0] * (360.0 / 255.0)
    sat = hsv[..., 1] / 255.0
    val = hsv[..., 2] / 255.0
    opaque = np.asarray(rgba)[..., 3] > 8

    skin = (hue >= 8.0) & (hue <= 48.0) & (sat > 0.17) & (sat < 0.62) & (val > 0.30)
    # Lips read as a saturated warm rose just past red; fold them into the same ramp.
    lips = ((hue >= 320.0) | (hue <= 8.0)) & (sat > 0.22) & (val > 0.35)
    skin = (skin | lips) & opaque
    if not skin.any():
        return rgba

    base = np.asarray(rgb, dtype=np.float32)
    lum = (0.299 * base[..., 0] + 0.587 * base[..., 1] + 0.114 * base[..., 2]) / 255.0
    # Map the face's own luminance onto the ivory paper ramp so shading structure survives.
    t = np.clip((lum - 0.42) / 0.52, 0.0, 1.0)[..., None]
    paper = ASH_TONE[None, None, :] * (1.0 - t) + IVORY_TONE[None, None, :] * t

    arr = np.asarray(rgba).astype(np.float32)
    m = skin[..., None]
    arr[..., :3] = np.where(m, paper, arr[..., :3])
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), mode="RGBA")


def bleed_edges(rgba: Image.Image, band_px: int = 4) -> Image.Image:
    """Decontaminate the silhouette edge by pushing interior colour outward.

    Chroma keys leave the backdrop hue smeared into the outermost 1-3 pixels of the subject,
    which survives every later pass as a dirty pink rim. Rather than desaturate that rim,
    overwrite it: take the solid interior as truth and flood its colour outward across the
    antialiased band. The alpha channel is untouched, so the matte stays as keyed.
    """
    arr = np.asarray(rgba).astype(np.float32)
    alpha = arr[..., 3]
    if not (alpha > 0).any():
        return rgba

    solid = Image.fromarray(((alpha > 200) * 255).astype(np.uint8), mode="L")
    for _ in range(max(0, band_px - 1)):
        solid = solid.filter(ImageFilter.MinFilter(3))
    filled = np.asarray(solid) > 127

    rgb = arr[..., :3].copy()
    work = rgb.copy()
    for _ in range(band_px * 2 + 2):
        need = (alpha > 0) & ~filled
        if not need.any():
            break
        acc = np.zeros_like(work)
        cnt = np.zeros(filled.shape, dtype=np.float32)
        for shift in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            acc += np.roll(work, shift, axis=(0, 1)) * np.roll(filled, shift, axis=(0, 1))[..., None]
            cnt += np.roll(filled, shift, axis=(0, 1))
        take = need & (cnt > 0)
        if not take.any():
            break
        work[take] = acc[take] / cnt[take][:, None]
        filled = filled | take

    out = arr.copy()
    out[..., :3] = work
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), mode="RGBA")


def harmonise(rgba: Image.Image, strength: float = 0.30) -> Image.Image:
    """Nudge every colour toward the shared paper palette so six renders read as one set.

    The blend is luminance preserving: the nearest swatch is rescaled to the pixel's own
    brightness before mixing. Without that, a mid-grey antialiased edge is nearest to the
    near-black swatch and gets dragged dark, which shows up as grit on the ivory panels.
    """
    arr = np.asarray(rgba).astype(np.float32)
    rgb = arr[..., :3]
    pal = np.array(PALETTE, dtype=np.float32)
    flat = rgb.reshape(-1, 3)
    d = ((flat[:, None, :] - pal[None, :, :]) ** 2).sum(axis=2)
    nearest = pal[d.argmin(axis=1)]

    lum_w = np.array([0.299, 0.587, 0.114], dtype=np.float32)
    lum_px = flat @ lum_w
    lum_pal = np.maximum(nearest @ lum_w, 1.0)
    matched = np.clip(nearest * (lum_px / lum_pal)[:, None], 0, 255)

    blended = flat * (1.0 - strength) + matched * strength
    out = arr.copy()
    out[..., :3] = blended.reshape(rgb.shape)
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), mode="RGBA")


def crop_to_subject(rgba: Image.Image, thresh: int = 24) -> Image.Image:
    arr = np.asarray(rgba)
    ys, xs = np.nonzero(arr[..., 3] > thresh)
    if len(ys) == 0:
        return rgba
    return rgba.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))


def head_unit(rgba: Image.Image) -> float:
    """Width of the widest silhouette row inside the top 45% - a practical 'head size'.

    Scale each cell by this rather than by the bounding box: a tall crown or a long hood
    would otherwise shrink the whole character and the set would read at different sizes.
    """
    alpha = np.asarray(rgba)[..., 3]
    ys, xs = np.nonzero(alpha > 24)
    if len(ys) == 0:
        return float(rgba.width)
    y0, y1 = int(ys.min()), int(ys.max())
    h = max(1, y1 - y0 + 1)
    widths = []
    for y in range(y0, min(y1 + 1, y0 + int(h * 0.45))):
        idx = np.nonzero(alpha[y] > 24)[0]
        if len(idx):
            widths.append(int(idx.max() - idx.min() + 1))
    if not widths:
        return float(rgba.width)
    return float(np.percentile(widths, 92))


def place_in_cell(rgba: Image.Image, cell: int, factor: float = 1.0,
                  body_frac: float = 0.985, max_width: float = 1.20) -> Image.Image:
    """Bottom-anchor the bust in a square cell, matching how UIPlayer draws it.

    UIPlayer draws each cell aspect-fit and bottom-centred, so the art must be cut by the
    bottom edge. Every bust is therefore scaled to the same *body span* (bbox height) and
    then corrected by a small per-slot factor, because a five-point crown or a tall hood
    makes the same body span read as a different head size. Shoulders may run past the
    side edges - the original atlas did the same - but nothing is ever clipped at the top.
    """
    sub = crop_to_subject(rgba)
    scale = (cell * body_frac / max(sub.height, 1)) * factor
    # Never crop the top (a crown, a hood point), but let wide shoulders leave the frame.
    scale = min(scale, cell / max(sub.height, 1))
    scale = min(scale, (cell * max_width) / max(sub.width, 1))
    w = max(1, int(round(sub.width * scale)))
    h = max(1, int(round(sub.height * scale)))
    sub = sub.resize((w, h), Image.LANCZOS)
    cell_img = Image.new("RGBA", (cell, cell), (0, 0, 0, 0))
    cell_img.paste(sub, ((cell - w) // 2, cell - h), sub)
    return cell_img


def process(path: str, cell: int, strength: float, factor: float = 1.0) -> Image.Image:
    img = Image.open(path).convert("RGB")
    rgba = chroma_key(img)
    rgba = bleed_edges(rgba)
    rgba = drop_islands(rgba)
    rgba = crop_to_subject(rgba)
    rgba = deskin(rgba)
    rgba = harmonise(rgba, strength)
    return place_in_cell(rgba, cell, factor)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("sources", nargs="+")
    ap.add_argument("--out", required=True, help="atlas PNG path")
    ap.add_argument("--cell", type=int, default=512)
    ap.add_argument("--strength", type=float, default=0.30)
    ap.add_argument("--scale", default=None,
                    help="six comma separated per-slot scale factors, atlas order")
    ap.add_argument("--single", action="store_true",
                    help="process just the first source into one --cell canvas (dialogue portrait)")
    ap.add_argument("--cells-dir", default=None, help="also write individual cells here")
    args = ap.parse_args()

    if args.single:
        factor = float(args.scale.split(",")[0]) if args.scale else 1.0
        out = process(args.sources[0], args.cell, args.strength, factor)
        os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
        out.save(args.out)
        print(f"{args.out}  {out.width}x{out.height}  {os.path.getsize(args.out)} bytes")
        return

    if len(args.sources) != 6:
        raise SystemExit(f"need exactly 6 sources in atlas order, got {len(args.sources)}")

    factors = [float(x) for x in args.scale.split(",")] if args.scale else [1.0] * 6
    if len(factors) != 6:
        raise SystemExit("--scale needs six values")

    cells = []
    for factor, path in zip(factors, args.sources):
        c = process(path, args.cell, args.strength, factor)
        cells.append(c)
        if args.cells_dir:
            os.makedirs(args.cells_dir, exist_ok=True)
            stem = os.path.splitext(os.path.basename(path))[0]
            c.save(os.path.join(args.cells_dir, f"{stem}.png"))

    atlas = Image.new("RGBA", (args.cell * 3, args.cell * 2), (0, 0, 0, 0))
    for i, c in enumerate(cells):
        atlas.paste(c, ((i % 3) * args.cell, (i // 3) * args.cell), c)
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    atlas.save(args.out)
    print(f"{args.out}  {atlas.width}x{atlas.height}  {os.path.getsize(args.out)} bytes")


if __name__ == "__main__":
    main()
