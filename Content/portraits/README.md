# Portrait Assets

The cold numeric UI uses `cold_roster.png`, a transparent 3-column × 2-row atlas:
Arith, Mara, Sentinel, Withered King, Warden, Echo. The full built-in image generation
prompt is in `cold_roster.prompt.txt`. Runtime regions are selected in `UIPlayerManager`.
See `Doc/ColdNumericUI.md` for integration and motion details. The older portraits below
remain available as source assets.

These portraits were generated as a visually consistent card set. Runtime text and UI frames are intentionally excluded from the images.

## Adding a portrait

1. Add a square PNG to this directory.
2. Register it in `Content.mgcb` and `ResourceManager.LoadContent` as `portrait_<key>`.
3. Set the campaign enemy's `portraitKey` to `<key>` in data or code.

Unregistered keys use the cold atlas with a team/name-based fallback. Custom registered
`portrait_<key>` textures and explicit per-player overrides still take precedence.

## `arithmetist.png`

> Use case: stylized-concept. Asset type: square character portrait for a dark fantasy strategy game card. Primary request: create a portrait of The Arithmetist, a solitary scholar-warrior who manipulates numbers as occult laws. Scene/backdrop: plain warm aged-paper field with a subtle circular engraved halo, no scenery. Subject: androgynous adult arithmetist, calm severe expression, half-length frontal portrait, dark academic robe, small brass counting tokens and geometric sigils, no weapons. Style/medium: authentic 18th-century copperplate engraving and woodcut hybrid, dense black cross-hatching, hand-printed ink texture, antique book illustration, restrained ochre accents only. Composition/framing: centered bust, generous margin, readable at small card size, square crop. Lighting/mood: solemn, mysterious, high-contrast etched shading. Color palette: black ink, warm ivory paper, muted sepia and antique gold. Constraints: one character only, no text, no letters, no numerals, no UI frame, no watermark, no photorealism, consistent clean silhouette.

## `withered_king.png`

> Use case: stylized-concept. Asset type: square enemy portrait for a dark fantasy strategy game card. Primary request: create The Withered King, an ancient exhausted monarch repeating the last commands of a vanished crown. Scene/backdrop: plain warm aged-paper field with a cracked circular crown halo, no scenery. Subject: one elderly gaunt king, half-length frontal portrait, hollow but lucid eyes, weathered ceremonial crown, torn royal mantle, frail posture, silent menace rather than gore. Style/medium: authentic 18th-century copperplate engraving and woodcut hybrid, dense black cross-hatching, hand-printed ink texture, antique book illustration, restrained ochre accents only. Composition/framing: centered bust, generous margin, readable at small card size, square crop, visually consistent with a scholarly occult portrait set. Lighting/mood: funereal, dignified, uncanny, high-contrast etched shading. Color palette: black ink, warm ivory paper, muted sepia and tarnished antique gold. Constraints: one character only, no text, no letters, no numerals, no UI frame, no watermark, no blood or gore, no photorealism, clean silhouette.

## `enemy_fallback.png`

> Use case: stylized-concept. Asset type: square fallback enemy portrait for a dark fantasy strategy game card. Primary request: create an anonymous Echo Guard, a spectral armored servant of a dead numerical kingdom, suitable as the default portrait for many enemy units. Scene/backdrop: plain warm aged-paper field with a faint fractured geometric halo, no scenery. Subject: one faceless medieval guard, half-length frontal portrait, worn dark plate and layered cloth, closed mask with narrow shadowed eye slit, restrained supernatural presence. Style/medium: authentic 18th-century copperplate engraving and woodcut hybrid, dense black cross-hatching, hand-printed ink texture, antique book illustration, restrained ochre accents only. Composition/framing: centered bust, generous margin, readable at small card size, square crop, visually consistent with The Arithmetist and Withered King portrait set. Lighting/mood: austere, ominous, impersonal, high-contrast etched shading. Color palette: black ink, warm ivory paper, muted sepia and tarnished antique gold. Constraints: one character only, no text, no letters, no numerals, no UI frame, no watermark, no blood or gore, no photorealism, clean silhouette.
