"""Skill icon lab.

`UI/Campaign/UISkillArt.cs` builds one 100x100 antialiased polygon mask per SkillType at
runtime and tints it, so an icon can only be judged by rebuilding the game. This script
reproduces that rasteriser exactly (same 100x100 space, same fill/cut ordering, same 2x2
supersampling) and lays the icons out like the loadout cards, so shapes can be iterated here
and then pasted back into the C# switch.

  python skill_icon_lab.py --out icons.png [--size 64] [--cols 5]
"""

from __future__ import annotations

import argparse

import numpy as np
from PIL import Image

SIZE = 100
SS = 2                      # matches the C# supersample loop
INK = (14, 32, 52)
TILE = (242, 235, 217)
PAGE = (14, 32, 52)


# --- the shape vocabulary, mirroring the local helpers in UISkillArt.Shapes ---------------

def slash(x: int) -> list[int]:
    return [x, 82, x + 5, 46, x + 35, 14, x + 30, 45]


def shield(x: int, w: int) -> list[tuple[list[int], bool]]:
    h = w // 2
    return [
        ([x, 24, x + h, 14, x + w, 24, x + w, 62, x + h, 85, x, 62], True),
        ([x + 8, 30, x + h, 24, x + w - 8, 30, x + w - 8, 57, x + h, 72, x + 8, 57], False),
    ]


def cur() -> dict[str, list[tuple[list[int], bool]]]:
    """The shapes shipped today, transcribed from the C# switch."""
    s: dict[str, list[tuple[list[int], bool]]] = {}

    s["PlusAttack"] = [(slash(30), True)]
    s["DoubleAttack"] = [(slash(13), True), (slash(49), True)]

    s["PlusDefence"] = shield(25, 50)
    s["DoubleDefence"] = shield(13, 42) + shield(47, 42)

    heart = [50, 83, 22, 55, 22, 32, 35, 22, 50, 34, 65, 22, 78, 32, 78, 55]
    s["DoubleHealth"] = [(heart, True)]
    s["TripleHealth"] = [(heart, True),
                         ([14, 27, 7, 36, 7, 61, 25, 78, 25, 65, 15, 54], True),
                         ([86, 27, 93, 36, 93, 61, 75, 78, 75, 65, 85, 54], True)]

    s["Xue"] = [([50, 12, 26, 54, 25, 70, 36, 84, 56, 88, 74, 73, 73, 56], True),
                ([50, 43, 63, 64, 61, 76, 52, 80, 54, 65], False)]

    s["Shi"] = [([28, 18, 56, 12, 75, 31, 58, 44, 33, 38], True),
                ([23, 24, 29, 43, 51, 52, 38, 82, 16, 60], True),
                ([61, 50, 81, 35, 88, 57, 66, 85, 45, 86], True)]

    s["Long"] = [([28, 15, 65, 15, 81, 34, 62, 34, 68, 41, 45, 50, 73, 62, 64, 84,
                   36, 89, 17, 70, 36, 70, 30, 63, 54, 56, 26, 43, 37, 31, 19, 31], True)]

    s["Bian"] = [([47, 12, 70, 23, 85, 46, 74, 55, 61, 32, 45, 25, 29, 35, 20, 29], True),
                 ([83, 58, 82, 77, 52, 90, 45, 76, 66, 68, 66, 48, 80, 43], True),
                 ([34, 87, 14, 73, 14, 45, 30, 40, 29, 62, 44, 70, 49, 57, 61, 65], True)]

    s["Tong"] = [([20, 31, 80, 31, 80, 43, 20, 43], True),
                 ([20, 58, 80, 58, 80, 70, 20, 70], True)]

    s["Mo"] = [([12, 50, 39, 22, 39, 41, 64, 41, 64, 20, 90, 47, 64, 75, 64, 57,
                 39, 57, 39, 78], True)]

    chevron = [50, 12, 86, 75, 74, 84, 50, 42, 26, 84, 14, 75]
    s["Seng"] = [(chevron, True)]
    s["Dao"] = [(chevron, True), ([22, 48, 78, 48, 84, 58, 16, 58], True)]

    lozenge = [50, 9, 71, 49, 50, 88, 29, 49]
    immortal = [(lozenge, True),
                ([50, 28, 59, 49, 50, 68, 41, 49], False),
                ([22, 34, 32, 66, 13, 53, 5, 27], True),
                ([78, 34, 68, 66, 87, 53, 95, 27], True)]
    s["Xian"] = list(immortal)
    s["SuperImmortal"] = list(immortal) + [([83, 3, 89, 3, 89, 11, 97, 11, 97, 17, 89, 17,
                                            89, 25, 83, 25, 83, 17, 75, 17, 75, 11, 83, 11], True)]

    crown = [12, 31, 35, 49, 50, 17, 65, 49, 88, 31, 78, 77, 22, 77]
    crown_cut = [27, 62, 73, 62, 71, 68, 29, 68]
    s["Di"] = [(crown, True), (crown_cut, False)]
    s["SuperEmperor"] = [(crown, True), (crown_cut, False),
                         ([83, 3, 89, 3, 89, 11, 97, 11, 97, 17, 89, 17,
                           89, 25, 83, 25, 83, 17, 75, 17, 75, 11, 83, 11], True)]

    # Shen is not cased in the C#, so it falls through to the generic hexagon ring.
    s["Shen"] = [([50, 10, 84, 30, 84, 70, 50, 90, 16, 70, 16, 30], True),
                 ([50, 25, 71, 38, 71, 62, 50, 75, 29, 62, 29, 38], False)]
    s["SuperGod"] = [([50, 10, 84, 30, 84, 70, 50, 90, 16, 70, 16, 30], True),
                     ([50, 25, 71, 38, 71, 62, 50, 75, 29, 62, 29, 38], False),
                     ([83, 3, 89, 3, 89, 11, 97, 11, 97, 17, 89, 17,
                       89, 25, 83, 25, 83, 17, 75, 17, 75, 11, 83, 11], True)]
    return s


ORDER = ["PlusAttack", "DoubleAttack", "Long", "Bian", "PlusDefence", "DoubleDefence",
         "Shi", "DoubleHealth", "TripleHealth", "Xue", "Mo", "Tong", "Di", "Xian",
         "Shen", "Seng", "Dao", "SuperGod", "SuperImmortal", "SuperEmperor"]


PLUS = ([83, 3, 89, 3, 89, 11, 97, 11, 97, 17, 89, 17, 89, 25, 83, 25, 83, 17,
         75, 17, 75, 11, 83, 11], True)


def proposed() -> dict[str, list[tuple[list[int], bool]]]:
    """Revised set: solid weight, one idea per icon, no shared silhouettes."""
    s: dict[str, list[tuple[list[int], bool]]] = {}

    def strike(x: int) -> list[int]:
        """A bold diagonal cut: wide flat base, tapering point. Stays inside the 100 box."""
        return [x, 90, x + 16, 94, x + 50, 14, x + 34, 10]

    # Offense: strikes, now with weight.
    s["PlusAttack"] = [(strike(22), True)]
    s["DoubleAttack"] = [(strike(6), True), (strike(44), True)]

    # Dragon: a fang - the bite that carries both numbers at once.
    s["Long"] = [([12, 14, 88, 14, 50, 94], True),
                 ([34, 46, 66, 46, 50, 74], False)]

    # Transform: a dial - set the damage to any value.
    s["Bian"] = [([50, 8, 70, 15, 85, 30, 92, 50, 85, 70, 70, 85, 50, 92, 30, 85,
                   15, 70, 8, 50, 15, 30, 30, 15], True),
                 ([50, 26, 63, 31, 72, 40, 76, 50, 72, 60, 63, 69, 50, 74, 37, 69,
                   28, 60, 24, 50, 28, 40, 37, 31], False),
                 ([44, 50, 70, 16, 78, 26, 58, 54], True)]

    # Defense: a pointed crest; halving it splits the same crest in two.
    crest = [14, 16, 86, 16, 86, 56, 50, 94, 14, 56]
    s["PlusDefence"] = [(crest, True),
                        ([22, 32, 78, 32, 78, 44, 50, 66, 22, 44], False)]
    s["DoubleDefence"] = [(crest, True),
                          ([14, 50, 86, 50, 86, 58, 14, 58], False)]

    # Stone: a flat faceted block with two facet lines.
    s["Shi"] = [([24, 78, 24, 40, 50, 16, 78, 32, 76, 72, 46, 86], True),
                ([50, 16, 42, 46, 76, 52], False),
                ([24, 40, 46, 86], False)]

    # Vitality: one heart, then the same heart split into three bands.
    heart = [50, 86, 20, 58, 20, 34, 34, 22, 50, 36, 66, 22, 80, 34, 80, 58]
    s["DoubleHealth"] = [(heart, True)]
    s["TripleHealth"] = [(heart, True),
                         ([37, 36, 45, 33, 45, 72, 37, 75], False),
                         ([55, 33, 63, 36, 63, 75, 55, 72], False)]

    # Blood: a drop with a single clean facet cut.
    s["Xue"] = [([50, 8, 24, 52, 24, 70, 36, 86, 50, 92, 64, 86, 76, 70, 76, 52], True),
                ([50, 34, 63, 62, 60, 78, 50, 84, 54, 64], False)]

    # Demon: damage returned - an arrow that rebounds.
    s["Mo"] = [([8, 50, 34, 22, 34, 42, 62, 42, 62, 28, 92, 50, 62, 72, 62, 58,
                 34, 58, 34, 78], True)]

    # Same: the two numbers you hold, side by side.
    s["Tong"] = [([14, 34, 44, 34, 44, 66, 14, 66], True),
                 ([56, 34, 86, 34, 86, 66, 56, 66], True)]

    # Emperor: crown.
    s["Di"] = [([10, 26, 34, 48, 50, 14, 66, 48, 90, 26, 82, 82, 18, 82], True),
               ([26, 64, 74, 64, 72, 72, 28, 72], False)]

    # Immortal: a cut diamond.
    s["Xian"] = [([50, 6, 86, 50, 50, 94, 14, 50], True),
                 ([50, 30, 70, 50, 50, 70, 30, 50], False)]

    # God: a heavy hexagonal sigil with a small hole at its centre.
    s["Shen"] = [([50, 8, 88, 30, 88, 70, 50, 92, 12, 70, 12, 30], True),
                 ([50, 38, 61, 44, 61, 56, 50, 62, 39, 56, 39, 44], False)]

    # Extra steps: one chevron for a step, two for another step.
    s["Seng"] = [([50, 14, 86, 58, 74, 70, 50, 42, 26, 70, 14, 58], True),
                 ([50, 56, 74, 84, 62, 94, 50, 84, 38, 94, 26, 84], True)]
    s["Dao"] = [([50, 12, 84, 54, 72, 66, 50, 40, 28, 66, 16, 54], True),
                ([30, 78, 70, 78, 70, 90, 30, 90], True)]

    # Super tier: base + the paper punch mark.
    s["SuperGod"] = list(s["Shen"]) + [PLUS]
    s["SuperImmortal"] = list(s["Xian"]) + [PLUS]
    s["SuperEmperor"] = list(s["Di"]) + [PLUS]
    return s


LABELS = {
    "PlusAttack": "Attack Up", "DoubleAttack": "Double Attack", "Long": "Dragon",
    "Bian": "Transform", "PlusDefence": "Defense Up", "DoubleDefence": "Double Defense",
    "Shi": "Stone", "DoubleHealth": "Double Health", "TripleHealth": "Triple Health",
    "Xue": "Blood", "Mo": "Demon", "Tong": "Same", "Di": "Emperor", "Xian": "Immortal",
    "Shen": "God", "Seng": "Seng", "Dao": "Dao", "SuperGod": "Super God",
    "SuperImmortal": "Super Immortal", "SuperEmperor": "Super Emperor",
}


# --- rasteriser, matching UISkillArt.Create ----------------------------------------------

def contains(points: list[int], x: float, y: float) -> bool:
    inside = False
    n = len(points)
    j = n - 2
    for i in range(0, n, 2):
        if (points[i + 1] > y) != (points[j + 1] > y):
            if x < (points[j] - points[i]) * (y - points[i + 1]) / (points[j + 1] - points[i + 1]) + points[i]:
                inside = not inside
        j = i
    return inside


def render(shapes: list[tuple[list[int], bool]]) -> np.ndarray:
    """Returns a float coverage mask in 0..1 at SIZE x SIZE."""
    cov = np.zeros((SIZE, SIZE), dtype=np.float32)
    for y in range(SIZE):
        for x in range(SIZE):
            hits = 0
            for sy in range(SS):
                for sx in range(SS):
                    px, py = x + (sx + 0.5) / SS, y + (sy + 0.5) / SS
                    ink = False
                    for pts, filled in shapes:
                        if contains(pts, px, py):
                            ink = filled
                    if ink:
                        hits += 1
            cov[y, x] = hits / (SS * SS)
    return cov


def tile(mask: np.ndarray, box: int) -> Image.Image:
    """Composite one icon onto an ivory card tile at the drawn size."""
    icon = Image.fromarray((mask * 255).astype(np.uint8), mode="L").resize((box, box), Image.LANCZOS)
    out = Image.new("RGB", (box, box), TILE)
    out.paste(Image.new("RGB", (box, box), INK), (0, 0), icon)
    return out


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--box", type=int, default=72)
    ap.add_argument("--cols", type=int, default=5)
    ap.add_argument("--set", choices=["current", "proposed"], default="current")
    args = ap.parse_args()

    shapes = cur() if args.set == "current" else proposed()
    names = [n for n in ORDER if n in shapes]
    rows = (len(names) + args.cols - 1) // args.cols
    pad, cap = 12, 20
    cell = args.box + 2 * pad
    sheet = Image.new("RGB", (args.cols * cell, rows * (cell + cap)), PAGE)
    for i, name in enumerate(names):
        x = (i % args.cols) * cell + pad
        y = (i // args.cols) * (cell + cap) + pad
        sheet.paste(tile(render(shapes[name]), args.box), (x, y))
    sheet.save(args.out)
    print(f"{args.out}  {sheet.width}x{sheet.height}  icons={len(names)}  set={args.set}")


if __name__ == "__main__":
    main()
