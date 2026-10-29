# Text rendering

Build the fonts with the repository's MGCB 3.8.5 tools (`dotnet tool restore`).
The former MGCB 3.8.4 output discarded left side bearings while preserving
advance widths. For IBM Plex Sans at 42 pt, `l` had a zero left bearing instead
of 4 pixels, and `I` had zero instead of 3. This shifted ink inside each character
cell and produced uneven letter spacing. MGCB 3.8.5 preserves those metrics;
keep `UseKerning` enabled and `Spacing` at zero.

The desktop project now builds `Content/Content.mgcb` during normal builds so
font source changes cannot silently leave an old XNB in the output directory.
Both body and title fonts include the punctuation used by the Swiss UI.

`UITextRenderer` provides pixel-aligned text origins and a single fit operation.
Controls fit against their physical bounds using the desktop scale as the
preferred size. They must not multiply by desktop scale again after fitting.
Box-label layout is refreshed when bounds or alignment change.

Verified using real MonoGame DesktopGL render targets at 1280 × 720 and
1920 × 1200, including a menu and mixed wide/narrow letters, digits, spaces,
and punctuation. Glyph advance widths and ink bounds were compared with
FreeType/Pillow output for the source TTF. Android rendering was not exercised.

## Cold UI readability pass

Keep IBM Plex Sans Regular for body text (42 pt source atlas) and SemiBold for
large headings (76 pt). Dedicated `ui_caption.spritefont` (Regular, 18 pt) and
`ui_label.spritefont` (SemiBold, 22 pt) reduce the downsampling needed for combat
metadata. All four atlases use kerning, zero extra spacing, matching punctuation
coverage, premultiplied alpha and lossless Color textures on desktop and Android.

`UITypography` defines five text roles. These numbers are **physical line-box
heights**, including leading, not glyph heights or font point sizes:

| Role | Height on the 1920×1200 canvas | Minimum on smaller desktop windows |
| --- | ---: | ---: |
| Small (skill codes) | 22 px | 18 px |
| Caption (health/HUD) | 26 px | 22 px |
| Name (SemiBold) | 30 px | 24 px |
| Body (commands/descriptions/dialogue) | 36 px | 28 px |
| Heading | 52 px | 34 px |

Labels opt into a role through `TypographyRole`; their physical width and height
bounds still take precedence. Combat allocates line boxes before positioning the
portrait, so readable names, health and skills stay separated from the large
numeric buttons. A skill line is reserved for every unit, keeping names and health
aligned even when skill counts differ.

Descriptions wrap into left-aligned paragraphs at the body size before using
smaller text to fit a constrained area. `WrapLines` preserves paragraphs and splits
long identifiers. Dialogue uses separate speaker, body and progress sizes.

The preview tool also exports `dialogue` and `type-specimen` screenshots and
checks minimum name/health line-box heights and non-overlapping combat metadata.
Render with 1280×720 and 1600×1000; the atlas contains tests for Il1, O0, 8B and 5S.
