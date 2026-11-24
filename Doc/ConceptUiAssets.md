# Concept UI artwork

The two assets below were created with the built-in image_gen tool from the
user-provided four-screen paper UI reference. They are reconstructions, not
pixel-identical extractions. Existing assets were preserved.

## Dialogue portrait

Saved asset: `Content/portraits/dialogue_mara_concept.png` (640 x 640, transparent).

The asset was regenerated with the local ComfyUI / Z-Image Turbo pipeline so the speaking
portrait matches the current portrait atlas. The superseded version was a hooded, faceless
figure, which no longer agreed with the unit card after Mara was given her own face and bob.

Prompt (rendered against a magenta chroma backdrop, then keyed by `tools/comfy/build_atlas.py`):

> The defining feature of this face: two huge empty almond eye sockets whose inner rim is a thin
> line of pale cold ivory light, the inside of each socket pure featureless darkness; no eyeball,
> no iris, no pupil, no eyelid, no lashes, so the face reads as a mask with two dark windows cut
> into it. Mara Venn, the guide who teaches the numbers, shown larger and closer than on a unit
> card. Waist-up, turned a quarter toward the viewer, one shoulder slightly forward, looking
> straight at the viewer and holding the look. She is built from the same stiff folded paper
> planes as the rest of the series: her short navy bob is three large rigid paper planes ending in
> a hard straight edge at the jaw with one pale ivory paper strip in the front fringe. The rest of
> the face is minimal and calm: one long folded plane for the nose, and a single flat horizontal
> crease for a closed mouth, no smile, no blush. Her expression is serene, patient and completely
> unreadable, the stillness of someone who already knows how this ends; she is not warm, not
> smiling, faintly uncanny, and she never looks away. A chunky ivory scarf wraps the neck in two
> thick folded bands, its loose end a single stiff plane hanging down the front. Below, a navy
> paper coat with two large lapel planes meeting in a soft V and one small round ivory pin on the
> left lapel. Both arms folded down out of frame, no hands visible. Constructed as hard flat paper
> planes, not a smooth vector character, no soft rounded hair. Rendered as folded paper. [shared
> style block from `Content/portraits/paper_roster.prompt.txt`]

The hollow sockets are deliberate. At the size the battle screen draws a unit, a readable pair of
eyes made Mara the most conventional face in the cast; empty almond voids keep her a recognisable
young woman through the bob, scarf and coat while making her the one character you cannot read.
It also lifts a purely practical problem: on the dialogue card's ivory paper an ivory face had no
value separation, and the dark sockets give the head its contrast back.

The earlier prompt, kept for reference:

> Edit/extract the large Mara character portrait in the dialogue card in the top-right quadrant of this reference image into a standalone transparent game UI sprite. Preserve her exact original design and composition as closely as possible: angular cream paper hood, dark featureless face, long pointed hood and layered dark navy folded-paper cloak, head and shoulders facing forward with head slightly tilted, detailed cut-paper edges and texture. Remove everything else including card, text, other characters and background. True transparent background. Do not invent a visible face, eyes, accessories or a new art style. Character cutout fills canvas, waist/shoulders end at bottom edge.

## Outcome landscapes (victory and defeat)

Saved assets: `Content/paper/victory_road_concept.png` (victory, "Node Cleared" and a won duel)
and `Content/paper/defeat_ground.png` (defeat, campaign or duel).

Both were regenerated with the local ComfyUI / Z-Image Turbo pipeline in
`tools/comfy/scene_art.py`: one flat paper-cut strip each, magenta chroma backdrop, then keyed,
colour-unified and cropped to its silhouette at build time.

The earlier version was the problem the rewrite fixes. Victory used a soft gouache landscape with
gradients, grain and a green cast; defeat borrowed a cell from the `chapters` atlas, so it drew a
painted rectangle - sky included - straight onto the card, and it showed whichever chapter happened
to be nearest rather than the loss itself. Two different media, neither matching the flat navy and
ivory paper-cut used everywhere else.

They are now one kind of artwork: flat planes, three tonal steps, no gradient, no grain, a
transparent sky that lets the panel through, and no rectangle edge. The strips are authored at
roughly the 1350 x 330 scene band's aspect (4:1) and cropped to the silhouette, so
`UIPaperOutcome.DrawStrip` only needs a uniform fit and a bottom anchor - no alpha read-back.

Victory prompt:

> Paper-cut landscape illustration: A road still ahead after a won battle. Tall dark navy folded
> paper peaks fill the left and right edges of the frame from top to bottom. Between them a low
> saddle, and through it a single smooth pale ivory paper road sweeps from the bottom centre up to
> the horizon, with no markings of any kind on it. On the saddle stands a small pale ivory castle of
> five thin spires. One flat pale ivory moon disc sits in the narrow sky gap above the saddle. Calm,
> cold, open. [shared scene style block from `tools/comfy/scene_art.py`]

Defeat prompt:

> Paper-cut landscape illustration: The ground where the duel was lost. A tall half-collapsed pale
> ivory paper arch stands just left of centre and reaches nearly to the top edge, its top broken off
> at an angle. Two snapped navy columns lean beside it and one tall leaning tower stump fills the
> right edge of the frame from top to bottom. Dark rubble planes stack across the bottom of the frame
> in three flat steps. No road, no bright light, no moon. Most masses are nearly black. Silent,
> broken, absolute. [shared scene style block]

The shared style block fixes the medium: large angular planes, one flat matte fill per shape, no
gradient, no haze, no glow, no grain, three navy tones plus warm ivory, a magenta chroma field where
the sky is, and a panoramic 4:1 band.

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
