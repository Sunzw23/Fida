"""ComfyUI portrait generation for Fida.

Generates the 3x2 paper/origami character portrait atlas used by the cold numeric UI
(Content/portraits/paper_roster.png -> loaded at runtime as "cold_roster").

ComfyUI is assumed to be running locally on 127.0.0.1:8188 with:
  diffusion_models/z_image_turbo_bf16.safetensors
  text_encoders/qwen_3_4b.safetensors      (CLIPLoader type "lumina2")
  vae/ae.safetensors

The graph mirrors ComfyUI's own image_z_image_turbo template:
  UNETLoader -> ModelSamplingAuraFlow(3) -> KSampler(8 steps, cfg 1, res_multistep, simple)
  CLIPLoader(lumina2) -> CLIPTextEncode -> positive
                      -> ConditioningZeroOut -> negative
  EmptySD3LatentImage(1024,1024) -> latent

Usage:
  python fida_portraits.py calibrate          # style probes on one character
  python fida_portraits.py full               # 6 characters x N seeds
  python fida_portraits.py img2img            # fidelity passes from the current atlas
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

HOST = os.environ.get("COMFY_HOST", "http://127.0.0.1:8188")
UNET = "z_image_turbo_bf16.safetensors"
CLIP = "qwen_3_4b.safetensors"
VAE = "ae.safetensors"

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT_ROOT = os.path.join(ROOT, "artifacts", "portraits-comfy")
RAW_DIR = os.path.join(OUT_ROOT, "raw")

# --------------------------------------------------------------------------------------
# Shared style block. Everything that makes the set read as one family lives here.
# --------------------------------------------------------------------------------------

STYLE_BASE = (
    "Flat folded-paper character design, hand-cut origami low-poly, each form built from a "
    "few large matte paper planes, hard straight creases, not a single gradient inside a plane. "
    "Strict two-material palette: deep desaturated navy blue paper and warm ivory cream paper, "
    "each rendered in three flat tonal steps (ice white, warm cream, pale ash / mid navy, deep navy, near black). "
    "One soft key light from the upper left, a thin bright edge highlight riding the ivory creases, "
    "gentle ambient occlusion in the deep folds where planes overlap, faint paper fibre grain, "
    "very fine deckled cut line on every silhouette edge. "
    "Elegant restrained art-deco geometry, sharp confident design, strong value contrast, "
    "dignified dark-fantasy tone. "
    "Centred bust framed from the shoulders up, cropped at the chest, symmetric and calm, "
    "generous empty margin around the figure, subject isolated with clean crisp cut edges against a "
    "plain flat pure white void. "
    "No text, no letters, no numerals, no runes, no watermark, no frame, no border, no scenery, "
    "no props, no cast shadow on the floor, one character only, no photorealism, no 3d render gloss."
)

# Second calibration round: push hard away from "photographed paper sculpture" toward
# flat graphic illustration, because the runtime art is flat cut-paper UI.
# Third round: production style. Locks the medium (graphic paper-cut illustration with
# only a whisper of relief), the two-material palette, the framing, and a chroma backdrop
# so the atlas can be keyed to hard alpha.
STYLE_PROD = (
    "Flat graphic paper-cut illustration in the manner of a screen-printed character poster, "
    "built from a handful of large angular folded-paper planes. Each plane is one solid matte "
    "colour; folds read as a single thin darker crease line plus a narrow one-step tonal change on "
    "the neighbouring plane, never as smooth photographic shading. "
    "STRICT two-material palette and nothing else: deep desaturated navy blue paper and warm ivory "
    "cream paper, each in three flat steps (ivory, pale ash, warm cream / mid navy, deep navy, near "
    "black). The face and skin are the same ivory paper as the costume, carved with navy creases: "
    "absolutely no beige, no peach, no pink, no tan, no skin colour anywhere in the image, no third hue. "
    "Even soft shadowless light, no cast shadow, no ground plane, no vignette. "
    "Crisp hard cut edges, restrained art-deco geometry, noble and quiet, strong readable silhouette, "
    "elegant negative space. "
    "FRAMING, identical for every character: chest-up bust seen straight on, top of the head just "
    "below the top edge of the frame, the shoulders reaching both side edges, the chest cut off "
    "cleanly by the bottom edge, figure centred on the horizontal axis, head and neck together "
    "about one third of the frame height. "
    "The figure is pasted on a completely uniform flat vivid magenta chroma-key backdrop, one single "
    "pure #FF00FF magenta value everywhere behind the character, no gradient, no shadow, no texture. "
    "No text, no letters, no numerals, no runes, no watermark, no frame, no border, no scenery, "
    "one character only, no photorealism, no glossy 3d render."
)

STYLE_VARIANTS_FLAT = {
    "d_flatposter": (
        "A flat 2D graphic illustration, drawn not photographed: a character poster in the style of a "
        "modern screen-print. The figure is assembled from large angular paper-cut shapes, each shape "
        "filled with one absolutely flat matte colour, no gradient, no airbrush, no photographic "
        "lighting, no cast shadow, no floor, no backdrop object. Depth comes only from three flat "
        "tonal steps per colour. A thin dark edge runs along each fold where two shapes meet. "
        "Palette strictly limited to deep desaturated navy blue and warm ivory cream. "
        "Crisp hard vector-like edges, confident geometric design, elegant negative space, "
        "strong graphic silhouette. Chest-up centred bust, generous empty margin, figure floating on an "
        "exactly uniform pure white field, no gradient in the background, no vignette, no shadow under "
        "the subject. No text, no letters, no numerals, no watermark, no frame, no border, no scenery, "
        "one character only, not a photo, not a 3d render, no realistic skin."
    ),
    "e_papercut": (
        "Flat cut-paper illustration, two-tone graphic art, the character built from a handful of large "
        "torn-and-folded paper shapes pasted flat onto the page like a collage. Every shape is a solid "
        "matte colour block with a clean hard silhouette; folds are suggested by one neighbouring shape "
        "a single step lighter or darker, never by a gradient. Slight fibrous paper texture, faint "
        "deckled edge. No lighting set-up, no drop shadow, no studio photograph, no modelled volume. "
        "Only deep navy blue paper and warm ivory paper, three flat steps of each. "
        "Graphic, poster-like, elegant, quietly severe. Chest-up centred bust with generous margin, "
        "cut cleanly out of a perfectly flat pure white page. "
        "No text, no letters, no numerals, no watermark, no frame, no scenery, one character only, "
        "not photographic, not 3d rendered, no beige skin tones."
    ),
    "f_blockprint": (
        "Bold graphic woodblock-meets-screenprint character portrait: heavy flat shapes, posterised to "
        "a handful of values, no smooth shading anywhere. The head and shoulders are constructed as "
        "folded origami planes but rendered as pure flat ink and paper blocks, the way a printmaker "
        "reduces a paper sculpture to six flat colours. Deep desaturated navy blue, warm ivory cream, "
        "and the near-black of the deepest fold. Hard clean edges, slight ink bite in the texture. "
        "Monumental, dignified, decorative, museum poster quality. "
        "Chest-up centred bust, calm frontal, generous empty margin, floating on a completely uniform "
        "pure white background with no shadow and no gradient. "
        "No text, no letters, no numerals, no watermark, no border, no scenery, one character only, "
        "no photorealism, no glossy render, no photographic papercraft."
    ),
}

# Style probes used during calibration only.
STYLE_VARIANTS = {
    "a_paperfold": STYLE_BASE,
    "b_editorial": (
        "Editorial paper-craft character illustration: a bust assembled from large flat folded paper "
        "planes in the manner of modern origami sculpture, shapes cut with a ruler so every edge is a "
        "perfect straight line, planes tilted in shallow 3/4 so neighbouring faces catch slightly "
        "different light. Only two materials: cold desaturated navy blue card and warm ivory card, "
        "each with three flat tonal steps. Single soft key light from upper left, faint dark occlusion "
        "line where two planes meet, a hairline of bright ivory along the lit edges, subtle paper tooth. "
        "Graphic, poster-like, quietly menacing, museum-grade craft object. "
        "Centred chest-up bust, symmetric, wide empty margin, character cut out cleanly on pure flat white. "
        "No text, no letters, no numerals, no watermark, no frame, no scenery, one character only, "
        "no photorealism, no glossy 3d render, no gradients on a single plane."
    ),
    "c_faceted": (
        "Stylised game character portrait built from large faceted origami planes, low-poly paper "
        "sculpture aesthetic, deliberate chunky geometry rather than hyper detail. Every facet is one "
        "flat matte colour with a crisp edge; folds are few and expressive. Palette locked to deep "
        "navy blue and warm ivory cream with three flat tonal steps each. Key light upper left, thin "
        "rim light on the ivory folds, soft contact shadows between layered planes, delicate paper "
        "grain. Bold readable silhouette, noble bearing, premium indie-game art direction. "
        "Chest-up centred bust, calm frontal composition, generous clean margin, figure isolated on a "
        "pure flat white background with crisp cut-out edges. "
        "No text, no letters, no numerals, no watermark, no frame, no scenery, one character only, "
        "no photorealism, no shiny render, no colour outside the two-material palette."
    ),
}

# --------------------------------------------------------------------------------------
# The six characters, in the atlas order the runtime expects:
#   0 Arith   1 Mara   2 Sentinel
#   3 Withered King   4 Warden   5 Echo
# --------------------------------------------------------------------------------------

# --------------------------------------------------------------------------------------
# Mara's eyes. The whole read of the character hinges on this one clause, so the options
# live here and can be rendered side by side before one is chosen as the default.
# --------------------------------------------------------------------------------------

MARA_EYES = {
    "giant_holes": (
        "two enormous empty eye sockets, each one nearly a third as tall as the whole face, cut "
        "straight through the ivory paper so darkness shows through them; no eyeball, no iris, no "
        "pupil, no eyelid, no lashes, just two deep hollow openings ringed by a thin raised paper "
        "edge, with the brow folded down into shadow above them"
    ),
    "sunk_void": (
        "two very large sunken eye sockets, the paper dented inward around them, each socket a deep "
        "near-black void that swallows the light; no eyeball, no iris, no pupil, no eyelid, nothing "
        "inside except darkness, and a faint cold edge where the socket cuts the ivory cheek"
    ),
    "rim_glow": (
        "two huge empty almond eye sockets whose inner rim is a thin line of pale cold ivory light, "
        "the inside of each socket pure featureless darkness; no eyeball, no iris, no pupil, no "
        "eyelid, no lashes, so the face reads as a mask with two dark windows cut into it"
    ),
    "void_hollow": (
        "two large hollow eye sockets cut clean out of the paper, each a deep featureless navy "
        "void with nothing inside it at all - no pupil, no iris, no white, no catchlight, no "
        "eyelid - only empty darkness where the eyes should be"
    ),
    "void_glimmer": (
        "two large hollow eye sockets of deep navy void, each holding one single tiny pale ivory "
        "point of light far down inside the darkness, no pupil, no iris, no eyelid"
    ),
    "void_rim": (
        "two large hollow almond eye sockets of deep navy void edged by a thin raised ivory paper "
        "rim, completely empty inside, no pupil, no iris, no catchlight"
    ),
    "glow_pale": (
        "two large almond eyes of flat pale ivory light with no pupil and no iris, burning quietly "
        "out of deep navy shadow, no eyelid, no lashes"
    ),
}

MARA_EYES_DEFAULT = "rim_glow"

MARA_MOOD = (
    "Her expression is serene, patient and completely unreadable, the stillness of someone who "
    "already knows how this ends; she is not warm, not smiling, faintly uncanny, and she never "
    "looks away. "
)


def mara_face(eye_key: str) -> str:
    return "Her face is carved ivory paper, not flesh: " + MARA_EYES[eye_key] + ", " + mara_face_tail()


def mara_face_tail() -> str:
    return (
        "The rest of the face is minimal and calm: one long folded plane for the nose, and a single "
        "flat horizontal crease for a closed mouth, no smile, no blush. "
    )


CHARS = {
    "arith": dict(
        index=0,
        label="ARITH",
        design=(
            "The Arithmetist, an androgynous young scholar-warrior who treats arithmetic as occult law. "
            "ASYMMETRIC swept-back ivory hair cut as four large sharp folded planes, one long blade of a "
            "lock slicing diagonally across the forehead. Narrow angular face, real readable features: "
            "two dark navy eye planes set deep under a straight ivory brow fold, a single clean nose "
            "plane, a flat unsmiling mouth line. Calm severe stare straight ahead. "
            "A tall navy collar flares up behind the neck in two folded wings; an ivory stole folds into "
            "a narrow sharp V on the chest, no necktie and no shirt collar; one ivory shoulder plate on "
            "the left, navy on the right. "
            "SIGNATURE SILHOUETTE: spiky asymmetrical ivory crown of hair above a wide flared collar, "
            "shoulders tapering down into a V."
        ),
    ),
    "mara": dict(
        index=1,
        label="MARA",
        design=(
            "The defining feature of this face: " + MARA_EYES[MARA_EYES_DEFAULT] + ". "
            "Mara Venn, the guide who teaches the numbers, a woman around thirty, built from the same "
            "stiff folded paper planes as the rest of the series: her short navy bob is three large "
            "rigid paper planes ending in a hard straight edge at the jaw, with one pale ivory paper "
            "strip in the front fringe. "
            + mara_face_tail()
            + MARA_MOOD
            + "A chunky ivory scarf wraps the neck in two "
            "thick folded bands, its loose end a single stiff plane hanging over the left shoulder. "
            "Below, a navy paper coat with two large lapel planes meeting in a soft V and one small "
            "round ivory pin on the left lapel. "
            "SIGNATURE SILHOUETTE: a stiff angular bob above a wide chunky scarf mass. "
            "Constructed as hard flat paper planes exactly like the other portraits, "
            "not a smooth vector character, no soft rounded hair, no glossy lips."
        ),
    ),
    "sentinel": dict(
        index=2,
        label="SENTINEL",
        design=(
            "The Sentinel, a masked elite guard. A tall narrow closed ivory helm made of long vertical "
            "folded planes, a single thin horizontal dark eye slit cut across the front, and a sharp "
            "fin-like crest ridge running from brow to nape. No mouth, no face. "
            "Two broad square navy pauldrons, each cut as one big angular plane with an ivory trim edge, "
            "a navy gorget of stacked bands under the chin, and a narrow ivory chest wedge. "
            "SIGNATURE SILHOUETTE: tall narrow helm and blade crest over broad squared shoulders, "
            "rigid, upright, disciplined."
        ),
    ),
    "king": dict(
        index=3,
        label="WITHERED KING",
        design=(
            "The Withered King, an ancient exhausted monarch still giving orders to a dead court. "
            "A five-point ivory crown of sharp folded spikes, slightly tilted. A gaunt long face: "
            "deep hollow cheek planes in mid navy, two sunken eyes under heavy ivory brow folds, "
            "a long straight ivory beard cut as three narrow folded wedges falling to the chest. "
            "A torn imperial mantle in deep navy with a wide ivory fur collar spreading outward in "
            "short stacked folds across both shoulders. "
            "SIGNATURE SILHOUETTE: five crown spikes above a beard wedge above a flared mantle, "
            "a narrow vertical silhouette widening at the base."
        ),
    ),
    "warden": dict(
        index=4,
        label="WARDEN",
        design=(
            "The Warden, an armoured gaoler of the dead numeric kingdom. A broad faceless navy helm built "
            "as a blocky chevron: flat angled faceplate with no slit or eyes at all, wide squared jaw "
            "plate. Two ivory pauldrons hammered into broad low pyramids on very wide shoulders, "
            "navy arm cylinders. The signature element is a hanging iron chain of small dark links "
            "draped in a shallow curve across the whole chest from one shoulder to the other. "
            "SIGNATURE SILHOUETTE: the widest shoulders of the set, a blunt featureless helm block, and "
            "the low curve of the chain reading as a dark line across the ivory chest."
        ),
    ),
    "echo": dict(
        index=5,
        label="ECHO",
        design=(
            "Echo, a spectral servant, more absence than person. A tall deep ivory hood folded as a "
            "single pointed cowl, the point leaning slightly back, with a wide ivory shoulder cape "
            "flaring in two folded planes. Inside the hood, a completely blank void of near-black paper "
            "where the face should be, with one thin horizontal pale glowing lit slit floating in that "
            "darkness; no halo, no eyes, no mouth. Below the void, a navy torso with two folded navy "
            "shoulders. "
            "SIGNATURE SILHOUETTE: the tallest and narrowest hood of the set, a pointed peak with no "
            "crown and no crest, plus a bright single horizontal light inside the dark face."
        ),
    ),
}

ORDER = ["arith", "mara", "sentinel", "king", "warden", "echo"]


# --------------------------------------------------------------------------------------
# ComfyUI transport
# --------------------------------------------------------------------------------------

def _post(path: str, payload: dict) -> dict:
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        HOST + path, data=data, headers={"Content-Type": "application/json"}
    )
    with urllib.request.urlopen(req, timeout=60) as resp:
        return json.loads(resp.read().decode("utf-8"))


def _get(path: str, params: dict | None = None):
    url = HOST + path
    if params:
        url += "?" + urllib.parse.urlencode(params)
    with urllib.request.urlopen(url, timeout=120) as resp:
        return resp.read()


def build_t2i(prompt: str, seed: int, width: int = 1024, height: int = 1024,
              steps: int = 8, shift: float = 3.0, prefix: str = "fida_portrait") -> dict:
    return {
        "1": {"class_type": "UNETLoader",
              "inputs": {"unet_name": UNET, "weight_dtype": "default"}},
        "2": {"class_type": "ModelSamplingAuraFlow",
              "inputs": {"model": ["1", 0], "shift": shift}},
        "3": {"class_type": "CLIPLoader",
              "inputs": {"clip_name": CLIP, "type": "lumina2", "device": "default"}},
        "4": {"class_type": "CLIPTextEncode",
              "inputs": {"text": prompt, "clip": ["3", 0]}},
        "5": {"class_type": "ConditioningZeroOut", "inputs": {"conditioning": ["4", 0]}},
        "6": {"class_type": "EmptySD3LatentImage",
              "inputs": {"width": width, "height": height, "batch_size": 1}},
        "7": {"class_type": "KSampler",
              "inputs": {"model": ["2", 0], "seed": seed, "steps": steps, "cfg": 1.0,
                         "sampler_name": "res_multistep", "scheduler": "simple",
                         "positive": ["4", 0], "negative": ["5", 0],
                         "latent_image": ["6", 0], "denoise": 1.0}},
        "8": {"class_type": "VAELoader", "inputs": {"vae_name": VAE}},
        "9": {"class_type": "VAEDecode",
              "inputs": {"samples": ["7", 0], "vae": ["8", 0]}},
        "10": {"class_type": "SaveImage",
               "inputs": {"images": ["9", 0], "filename_prefix": prefix}},
    }


def build_i2i(prompt: str, seed: int, image_name: str, denoise: float,
              width: int = 1024, height: int = 1024,
              steps: int = 10, shift: float = 3.0, prefix: str = "fida_portrait_i2i") -> dict:
    g = build_t2i(prompt, seed, width=width, height=height, steps=steps,
                  shift=shift, prefix=prefix)
    g["20"] = {"class_type": "LoadImage", "inputs": {"image": image_name}}
    g["21"] = {"class_type": "ImageScale",
               "inputs": {"image": ["20", 0], "upscale_method": "lanczos",
                          "width": width, "height": height, "crop": "disabled"}}
    g["22"] = {"class_type": "VAEEncode",
               "inputs": {"pixels": ["21", 0], "vae": ["8", 0]}}
    g["7"]["inputs"]["latent_image"] = ["22", 0]
    g["7"]["inputs"]["denoise"] = denoise
    return g


def submit(graph: dict, client_id: str) -> str:
    resp = _post("/prompt", {"prompt": graph, "client_id": client_id})
    if resp.get("node_errors"):
        raise RuntimeError(f"node errors: {json.dumps(resp['node_errors'])[:2000]}")
    return resp["prompt_id"]


def wait_for(prompt_id: str, timeout: float = 900.0) -> dict:
    deadline = time.time() + timeout
    while time.time() < deadline:
        raw = _get(f"/history/{prompt_id}")
        hist = json.loads(raw.decode("utf-8"))
        if prompt_id in hist:
            entry = hist[prompt_id]
            status = entry.get("status", {})
            if status.get("status_str") == "error" or status.get("completed") is False:
                if status.get("status_str") == "error":
                    raise RuntimeError(f"generation failed: {json.dumps(status)[:2000]}")
            return entry
        time.sleep(1.0)
    raise TimeoutError(f"prompt {prompt_id} did not finish in {timeout}s")


def fetch_images(entry: dict, dest_dir: str, stem: str) -> list[str]:
    os.makedirs(dest_dir, exist_ok=True)
    saved = []
    for node_id, out in entry.get("outputs", {}).items():
        for i, img in enumerate(out.get("images", [])):
            blob = _get("/view", {"filename": img["filename"],
                                  "subfolder": img.get("subfolder", ""),
                                  "type": img.get("type", "output")})
            suffix = "" if i == 0 else f"_{i}"
            path = os.path.join(dest_dir, f"{stem}{suffix}.png")
            with open(path, "wb") as fh:
                fh.write(blob)
            saved.append(path)
    return saved


# --------------------------------------------------------------------------------------
# Prompt assembly
# --------------------------------------------------------------------------------------

DIALOGUE = {
    "mara": dict(
        label="MARA (dialogue)",
        design=(
            "The defining feature of this face: " + MARA_EYES[MARA_EYES_DEFAULT] + ". "
            "Mara Venn, the guide who teaches the numbers, shown larger and closer than on a unit card. "
            "Waist-up, turned a quarter toward the viewer, one shoulder slightly forward, looking "
            "straight at the viewer and holding the look. She is built from the same stiff "
            "folded paper planes as the rest of the series: her short navy bob is three large rigid paper "
            "planes ending in a hard straight edge at the jaw with one pale ivory paper strip in the front "
            "fringe. "
            + mara_face_tail()
            + MARA_MOOD
            + "A chunky ivory scarf wraps the neck in two thick folded bands, its loose end a single stiff "
            "plane hanging down the front. Below, a navy paper coat with two large lapel planes meeting in "
            "a soft V and one small round ivory pin on the left lapel. Both arms folded down out of frame, "
            "no hands visible. "
            "Constructed as hard flat paper planes, not a smooth vector character, no soft rounded hair."
        ),
    ),
}


def prompt_for(char_key: str, style_variant: str = "a_paperfold") -> str:
    if style_variant == "g_prod":
        style = STYLE_PROD
    elif style_variant in STYLE_VARIANTS_FLAT:
        style = STYLE_VARIANTS_FLAT[style_variant]
    elif style_variant in STYLE_VARIANTS:
        style = STYLE_VARIANTS[style_variant]
    else:
        raise KeyError(f"unknown style variant {style_variant!r}")
    design = CHARS[char_key]["design"]
    return (
        f"Character design sheet: {design} "
        f"Rendered as folded paper. {style}"
    )


def run_batch(jobs: list[dict], tag: str) -> list[dict]:
    client_id = str(uuid.uuid4())
    results = []
    for n, job in enumerate(jobs, 1):
        graph = job["graph"]
        stem = job["stem"]
        sys.stdout.write(f"[{n}/{len(jobs)}] {stem} ... ")
        sys.stdout.flush()
        t0 = time.time()
        try:
            pid = submit(graph, client_id)
            entry = wait_for(pid)
            files = fetch_images(entry, RAW_DIR, stem)
            dt = time.time() - t0
            print(f"ok {dt:.1f}s -> {len(files)} file(s)")
            results.append({"stem": stem, "files": files, "prompt_id": pid,
                            "seconds": round(dt, 1), "job": job["meta"]})
        except Exception as exc:  # noqa: BLE001 - surface and continue the batch
            print(f"FAILED: {exc}")
            results.append({"stem": stem, "files": [], "error": str(exc),
                            "job": job["meta"]})
    stamp = time.strftime("%Y%m%d-%H%M%S")
    manifest = os.path.join(OUT_ROOT, f"manifest-{tag}-{stamp}.json")
    os.makedirs(OUT_ROOT, exist_ok=True)
    with open(manifest, "w", encoding="utf-8") as fh:
        json.dump(results, fh, indent=2)
    print(f"\nmanifest: {manifest}")
    return results


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("mode", choices=["calibrate", "full", "img2img", "dialogue", "eyes"])
    ap.add_argument("--chars", default=",".join(ORDER))
    ap.add_argument("--seeds", default="1001,1002,1003")
    ap.add_argument("--variant", default="a_paperfold")
    ap.add_argument("--variants", default="d_flatposter,e_papercut,f_blockprint")
    ap.add_argument("--eyes", default="", help="comma separated MARA_EYES keys; empty = all")
    ap.add_argument("--denoise", type=float, default=0.55)
    args = ap.parse_args()

    seeds = [int(s) for s in args.seeds.split(",") if s.strip()]

    if args.mode == "calibrate":
        chars = [c for c in args.chars.split(",") if c]
        jobs = []
        for variant in [v for v in args.variants.split(",") if v]:
            for key in chars:
                for seed in seeds[:1]:
                    jobs.append(dict(
                        stem=f"cal2-{key}-{variant}-s{seed}",
                        meta={"char": key, "variant": variant, "seed": seed},
                        graph=build_t2i(prompt_for(key, variant), seed,
                                        prefix=f"fida_cal2_{key}_{variant}"),
                    ))
        return 0 if run_batch(jobs, "calibrate") else 1

    if args.mode == "full":
        jobs = []
        for key in [c for c in args.chars.split(",") if c]:
            for seed in seeds:
                jobs.append(dict(
                    stem=f"{key}-{args.variant}-s{seed}",
                    meta={"char": key, "variant": args.variant, "seed": seed},
                    graph=build_t2i(prompt_for(key, args.variant), seed,
                                    prefix=f"fida_{key}"),
                ))
        return 0 if run_batch(jobs, "full") else 1

    if args.mode == "eyes":
        # Same character, same style block, only the eye clause changes.
        jobs = []
        for eye_key in [k for k in (args.eyes or "").split(",") if k] or list(MARA_EYES):
            design = (
                "The defining feature of this face: " + MARA_EYES[eye_key] + ". "
                "Mara Venn, the guide who teaches the numbers, a woman around thirty, built from the "
                "same stiff folded paper planes as the rest of the series: her short navy bob is three "
                "large rigid paper planes ending in a hard straight edge at the jaw, with one pale "
                "ivory paper strip in the front fringe. "
                + mara_face_tail()
                + MARA_MOOD
                + "A chunky ivory scarf wraps the neck in two thick folded bands, its loose end a single "
                "stiff plane hanging over the left shoulder. Below, a navy paper coat with two large "
                "lapel planes meeting in a soft V and one small round ivory pin on the left lapel. "
                "Constructed as hard flat paper planes, not a smooth vector character, no soft rounded hair."
            )
            prompt = f"Character design sheet: {design} Rendered as folded paper. {STYLE_PROD}"
            for seed in seeds:
                jobs.append(dict(
                    stem=f"eyes-{eye_key}-s{seed}",
                    meta={"char": "mara", "eyes": eye_key, "seed": seed},
                    graph=build_t2i(prompt, seed, prefix=f"fida_eyes_{eye_key}"),
                ))
        return 0 if run_batch(jobs, "eyes") else 1

    if args.mode == "dialogue":
        jobs = []
        for key in [c for c in args.chars.split(",") if c]:
            if key not in DIALOGUE:
                continue
            prompt = f"Character design sheet: {DIALOGUE[key]['design']} Rendered as folded paper. {STYLE_PROD}"
            for seed in seeds:
                jobs.append(dict(
                    stem=f"dialogue-{key}-s{seed}",
                    meta={"char": key, "seed": seed, "kind": "dialogue"},
                    graph=build_t2i(prompt, seed, prefix=f"fida_dialogue_{key}"),
                ))
        return 0 if run_batch(jobs, "dialogue") else 1

    # img2img fidelity pass: needs the source cells uploaded into ComfyUI's input dir.
    manifest_path = os.path.join(OUT_ROOT, "i2i_sources.json")
    if not os.path.exists(manifest_path):
        print(f"missing {manifest_path}: run the upload helper first")
        return 2
    with open(manifest_path, encoding="utf-8") as fh:
        sources = json.load(fh)
    jobs = []
    for key in [c for c in args.chars.split(",") if c]:
        if key not in sources:
            continue
        for seed in seeds:
            jobs.append(dict(
                stem=f"i2i-{key}-s{seed}",
                meta={"char": key, "seed": seed, "denoise": args.denoise},
                graph=build_i2i(prompt_for(key, args.variant), seed,
                                sources[key], args.denoise, steps=12,
                                prefix=f"fida_i2i_{key}"),
            ))
    return 0 if run_batch(jobs, "img2img") else 1


if __name__ == "__main__":
    raise SystemExit(main())
