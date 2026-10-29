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
