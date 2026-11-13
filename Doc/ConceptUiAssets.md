# Concept UI artwork

The two assets below were created with the built-in image_gen tool from the
user-provided four-screen paper UI reference. They are reconstructions, not
pixel-identical extractions. Existing assets were preserved.

## Dialogue portrait

Saved asset: `Content/portraits/dialogue_mara_concept.png`.

Prompt:

> Edit/extract the large Mara character portrait in the dialogue card in the top-right quadrant of this reference image into a standalone transparent game UI sprite. Preserve her exact original design and composition as closely as possible: angular cream paper hood, dark featureless face, long pointed hood and layered dark navy folded-paper cloak, head and shoulders facing forward with head slightly tilted, detailed cut-paper edges and texture. Remove everything else including card, text, other characters and background. True transparent background. Do not invent a visible face, eyes, accessories or a new art style. Character cutout fills canvas, waist/shoulders end at bottom edge.

## Node-cleared landscape

Saved asset: `Content/paper/victory_road_concept.png`.

Prompt:

> Create an isolated wide landscape game UI illustration matching the Node Cleared artwork in the bottom right of the supplied reference. Reconstruct the same horizontal composition: dark navy angular paper-cut ruins and jagged hills at the edges, a pale winding road leading from the lower foreground toward a small distant pale castle slightly right of center, and a small textured ivory moon to the right above the mountains. Preserve its dark blue, charcoal and ivory palette, subtle paper texture, layered cut-paper look, quiet night mood and visual proportions. It will be overlaid on a dark paper panel. Use a truly transparent background behind the landscape and moon, no sky rectangle. Wide low horizontal strip, no text, no buttons, no UI, no borders or banner.

Both PNGs retain their alpha channel and are processed by MGCB with
`PremultiplyAlpha=True` for the game's `AlphaBlend` renderer. Desktop and Android
content manifests include them.

## Campaign cut-paper location atlases

Created with the built-in image generation tool using the supplied campaign
reference image as a style reference. Assets: `Content/paper/campaign_cutpaper_locations.png`
and `Content/paper/campaign_cutpaper_locations_extra.png`.

Prompts: Create two 3-by-2 atlases of six distinct, self-contained wide
landscapes in the reference's simple flat cut-paper style: angular blue-gray
mountains, dark navy foreground, empty ivory sky and subtle paper grain. Use
few large shapes, no text, people, ornate texture or UI. The first atlas shows
a forge, gate and bridge, hospice, bell tower, altar and throne; the second
shows a dragon tomb, archive, ferry, oracle stones, mirror and watchtower.

## Per-node Campaign artwork

Created with the built-in image generation tool using the supplied cut-paper
reference. Five 3-by-2 atlases are saved as `Content/paper/campaign_nodes_1.png`
through `campaign_nodes_5.png`. Each scene uses flat angular silhouettes,
navy and slate mountain layers, an ivory sky, sparse details, and paper grain.
The prompts requested, in atlas order: king ruins, signal beacon, river crossing,
echo canyon, forge, gateway; hospice, general's fortress, blood hunt, twin
bells, idols, bell tower; altar, gallery, throne, oracle stones, watchtower,
mirror; scroll, broken seal, dragon tomb, dragon bones, archive, scribe; ash
conduit, dragon reflection, void portal, paper machine, crown, and ferry.
