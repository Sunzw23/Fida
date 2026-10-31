# Paper UI

Branch: `codex/paper-ui`.

The approved paper concept is the visual reference: navy fibrous paper, ivory number tiles, cyan selection, physical cut edges and offset shadows. Main-menu artwork and six character busts are bitmap assets; text, controls, icons and numeric values remain live UI. Character busts use the same faceless angular cut-paper construction as the battle concept.

## Assets

`Content/paper/` contains the background, main-menu artwork, four paper skins and six chapter scenes. `Content/portraits/paper_roster.png` contains a transparent 3×2 roster: Arith, Mara, Sentinel / Withered King, Warden, Echo. Each asset has an adjacent `.prompt.txt`; all were generated with the built-in image generator using the approved concepts as references. Both desktop and Android content manifests include the assets.

`UIPaper` locates the largest opaque sheet in each material cell, ignores detached margin flecks, and nine-slices the clean paper surfaces with soft offset shadows. Battle uses a sparse ruins backdrop (`Content/paper/battle.png`). Other pages use a quiet navy base; visible fibers, scratches, repeated texture tiles and extra decorative planes have been removed.

## Selected concepts

This revision follows concepts 01–08 and 13–16: main menu, chapter browser, mission route, loadout, battle, dialogue, pause, node cleared, online, hosting, connecting and lobby. Chapter art uses a few silhouette layers. Battle portraits use broad folds above a centered name/health strip and fixed number cards. Loadout details, equipped skills and Start share the right column. Dialogue adds the speaker portrait and a clear Next tab. Outcomes use a simple seal, reward strip and road scene. Hosting displays four live code cells; connecting uses three restrained moving markers. Lobby identities use six broad vector paper shapes rather than battle artwork.

Route cards are arranged by the actual parent dependencies, preserving unlock rules. Chapter and mission counts, rewards and all skill costs come from the canonical game data, so they can differ from the illustrative numbers in the concept boards.

## Interaction and motion

Gameplay numbers keep fixed hit rectangles. Portrait breathing and action motion do not move the numeric controls. Battle layout supports uneven teams and overflow rows. Existing skill costs, budgets, campaign progression, room codes and network callbacks are preserved.

Pages crossfade over 200 ms using two viewport-sized render targets. Resizing recreates them; UI disposal releases them. At 1600×1000 the pair uses approximately 12.2 MiB of color buffers. Early transition input is guarded and input snapshots prevent a held press from activating a new page. Reduced motion skips page blending and portrait idle motion.

Pause retains a separate frozen battle texture (approximately 6.1 MiB at 1600×1000), darkens it and draws the centered paper menu. Returning through Settings or Rules keeps that battle frame. UI disposal releases the snapshot.

## Verification

`tools/UiPreview` renders the real controls without reading or writing user saves. It covers menus, all Rules pages, Campaign and route view, loadout states, 3v3/4v4 battle, dialogue, online screens, pause and outcomes. It verifies map actions, skill restrictions, room-code entry, transition frame blending, reduced motion, portrait motion and stable number pixels/hit targets.

Preview: `dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1600 1000 artifacts/paper-simple`.

Android compilation is a compatibility check; it does not replace device interaction or live multiplayer testing.
