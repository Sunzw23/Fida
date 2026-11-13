# Fida Gum layouts

Open `Fida.gumx` in the Gum editor. The project contains `Dialogue`, `Pause`, and
`Outcome` screens, each with `Landscape` and `Portrait` variants.

Gum owns element geometry, parenting, percentage sizing and anchors. The game
reads those elements through `UIGumLayout`; the existing paper renderer and
button state machines use the same calculated rectangles. Containers appear as
editable outlines in Gum. Text, textured paper layers, dynamic rewards, portraits
and gameplay events are still rendered/bound by the game, so the editor is a
layout view rather than a full visual preview of the paper effects.

- Move or resize `Panel` to move the entire group. Its children use `Parent`.
- `Dialogue` uses percentage dimensions and bottom anchoring.
- `Pause` and `Outcome` center their panels, with child elements local to them.
- Keep the named elements: C# looks them up by name and reports missing elements.
- Save the Gum project, rebuild, and restart the game to use the edited files.
- Desktop builds copy the entire folder; Android packages it under `Content/Gum`.

The runtime is pinned to `Gum.MonoGame` 2026.7.22.1. Use a compatible Gum editor.
The desktop UI is rendered into a 4-sample MSAA target, then resolved to the
window with linear filtering. This smooths rotated sheet edges and primitive
strokes without enabling MSAA on the window's back buffer. The seal uses a
512-pixel coverage-smoothed texture. Android retains its existing single-sample
render target; its content files include the same layouts and artwork.

Render the three screens with their real game renderer:

```powershell
dotnet run --project tools/UiPreview/UiPreview.csproj -c UiReview -- 1600 1000 artifacts/gum-ui --concept-only
```

Artwork generation and prompts are recorded in `Doc/ConceptUiAssets.md`.
