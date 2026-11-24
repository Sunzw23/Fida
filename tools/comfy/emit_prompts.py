"""Emit the provenance prompt file that sits next to the atlas.

  python emit_prompts.py --out Content/portraits/paper_roster.prompt.txt
"""

from __future__ import annotations

import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fida_portraits as F  # noqa: E402

HEADER = """\
Built with ComfyUI (local, 127.0.0.1:8188), model Z-Image Turbo.
Generation prompt for Content/portraits/paper_roster.png, the transparent 3 x 2 portrait atlas
that ResourceManager loads as "cold_roster" and UIPlayerManager slices into six unit portraits.

Pipeline
  UNETLoader z_image_turbo_bf16.safetensors
  ModelSamplingAuraFlow shift 3
  CLIPLoader qwen_3_4b.safetensors type lumina2
  CLIPTextEncode -> positive, ConditioningZeroOut -> negative (cfg 1.0)
  EmptySD3LatentImage 1024 x 1024
  KSampler 8 steps, cfg 1.0, res_multistep, simple, denoise 1.0
  VAEDecode ae.safetensors
One render per character at 1024 x 1024. Regenerate with tools/comfy/fida_portraits.py and
assemble with tools/comfy/build_atlas.py.

Post-processing (tools/comfy/build_atlas.py)
  - chroma-key the magenta backdrop to straight alpha, close pinholes, erode 1px
  - repaint peach/skin pixels as ivory paper so the whole cast stays on two materials
  - blend 50% toward the shared navy/ivory swatches so six renders read as one set
  - bottom-anchor each bust in a 512px cell, because UIPlayer draws cells aspect-fit and
    bottom-centred and the chest must be cut by the cell edge
MGCB premultiplies alpha (PremultiplyAlpha=True), so the hard matte edges stay clean on charcoal.

Atlas order (row-major, matches ColdPortraitRegion in UIPlayerManager):
  0 Arith   1 Mara        2 Sentinel
  3 Withered King   4 Warden   5 Echo
"""


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--variant", default="g_prod")
    args = ap.parse_args()

    parts = [HEADER]
    for key in F.ORDER:
        meta = F.CHARS[key]
        parts.append(f"\n## cell {meta['index']} - {meta['label']}\n")
        parts.append(F.prompt_for(key, args.variant).strip() + "\n")

    with open(args.out, "w", encoding="utf-8") as fh:
        fh.write("".join(parts))
    print(f"{args.out}  {os.path.getsize(args.out)} bytes")


if __name__ == "__main__":
    main()
