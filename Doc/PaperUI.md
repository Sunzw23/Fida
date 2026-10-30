# Paper UI

Branch: `codex/paper-ui`.

The approved paper concept is the visual reference: navy fibrous paper, ivory number tiles, cyan selection, physical cut edges and offset shadows. Main-menu artwork and six character busts are bitmap assets; text, controls, icons and numeric values remain live UI. Character busts use the same faceless angular cut-paper construction as the battle concept.

## Assets

`Content/paper/` contains the background, main-menu artwork, four paper skins and six chapter scenes. `Content/portraits/paper_roster.png` contains a transparent 3×2 roster: Arith, Mara, Sentinel / Withered King, Warden, Echo. Each asset has an adjacent `.prompt.txt`; all were generated with the built-in image generator using the approved concepts as references. Both desktop and Android content manifests include the assets.

`UIPaper` locates the largest opaque sheet in each material cell, ignores detached margin flecks, and tiles paper grain at a stable scale while retaining cut corners and shadows. Battle uses a quiet paper background so the portraits and number tiles remain dominant. Other pages share the layered paper background, headers and navigation.

## Interaction and motion

Gameplay numbers keep fixed hit rectangles. Portrait breathing and action motion do not move the numeric controls. Battle layout supports uneven teams and overflow rows. Existing skill costs, budgets, campaign progression, room codes and network callbacks are preserved.

Pages crossfade over 200 ms using two viewport-sized render targets. Resizing recreates them; UI disposal releases them. At 1600×1000 the pair uses approximately 12.2 MiB of color buffers. Early transition input is guarded and input snapshots prevent a held press from activating a new page. Reduced motion skips page blending and portrait idle motion.

## Verification

`tools/UiPreview` renders the real controls without reading or writing user saves. It covers menus, all Rules pages, Campaign and route view, loadout states, 3v3/4v4 battle, dialogue, online screens, pause and outcomes. It verifies map actions, skill restrictions, room-code entry, transition frame blending, reduced motion, portrait motion and stable number pixels/hit targets.

Preview: `dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1600 1000 artifacts/paper-reference`.

Android compilation is a compatibility check; it does not replace device interaction or live multiplayer testing.
