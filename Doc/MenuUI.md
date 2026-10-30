# Menu UI

The main menu, settings and duel lobby use `UIEditorialSurface` for artwork and
`UIEditorialButton` for named actions. Copy is English, with no promotional
subtitle. These screens are built in `UIScreenBuilder.Editorial.cs`; their old
JSON descriptions are no longer used by the live builders.

- Main menu: Campaign, Online, Rules, Settings and Exit. The connected menu
  retains Campaign, Duel, Connection, Rules, Settings, Disconnect and Exit.
  There is no separate local duel feature to advertise.
- Settings: current window mode and motion option are filled white. Full and
  Reduced set their respective value, so selecting the active option is safe.
  Android displays device-managed display settings and retains motion controls.
- Lobby: local and remote readiness are read from the existing lobby votes.
  Player numbers follow host/client identity, while the local player remains
  on the left. The ready button becomes unavailable after voting. Existing
  shared-draft handoff, disconnect and return-to-menu behavior is preserved.

`UIEditorialLayout` supplies the button geometry used for drawing, mouse input
and controller focus. Portrait layouts stack menu artwork above navigation and
setting labels above their controls. Decoration moves independently of the
buttons; reduced motion stops numeric drift and portrait breathing.

The desktop campaign browser keeps the original folder hierarchy, graph, camera
and campaign unlock data. `CampaignMapLayout` reserves a persistent details pane
beside the map, or below it in portrait windows. Clicking a mission only selects
it; Configure skills (or Enter / controller A) confirms it. Locked missions can
be inspected but cannot be entered. Folder actions open the selected chapter;
Back returns through the hierarchy. Zoom and pan remain available. Node sizes
are world units, avoiding a second viewport scale before the camera scales them.
Android retains its touch-oriented mission list with simplified frames.

The main-menu numerals use a separately cached display-resolution mask of the
existing numeral geometry; combat numerals retain their original atlas.

## Verification

Run the regular unit suite and build both desktop and Android. The local preview
runner also exercises map inspect/confirm, locked mission rejection, cross-target
mouse release and single-consumption of navigation events without modifying saves.

```powershell
dotnet test Fida.Tests/Fida.Tests.csproj -c UiReview --no-restore -m:1 -p:BuildInParallel=false
dotnet build tools/UiPreview/UiPreview.csproj -c UiReview --no-restore -m:1 -p:BuildInParallel=false
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1600 1000 artifacts/editorial-ui --menus-only
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1280 720 artifacts/editorial-ui --menus-only
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 720 1280 artifacts/editorial-ui --menus-only
```

Screenshots include the normal and connected menus, settings, campaign chapters,
a real chapter route, and lobby before/after local readiness. This checks actual
MonoGame rendering rather than reproducing the concept art. Android packaging
and shared code compile checks do not replace physical touch/device testing.

## Rules and online reference screens

Rules uses the same header/back geometry as Settings, with five directly
selectable chapters. Instructions and ordinary-size numeric examples are text
rows; there is no display-size numeric artwork. Skill pages use the canonical
skill names and the same descriptions as the loadout details. Portrait windows
place navigation above the reference and show skills in a single column.

Online separates Create room and Join room. A four-cell code field retains the
existing letters/digits keyboard handling, length limit and Android keyboard.
Join is unavailable until a complete relay code is entered. Enter queues a join
intent for state input handling, avoiding a screen rebuild inside control Update.
Direct-server sessions hide the relay-code input and can join without a code.
Connected, hosting and connecting screens share the header and actions; status
updates preserve the existing named-label interfaces. Connection messages wrap.

Use `--reference-only` in UiPreview to render every Rules page plus online,
connected, hosting and connecting screens. It exercises incomplete/complete code
entry, length limiting, deletion, Enter submission and Join activation using the
actual controls, without opening network connections or changing user saves.

Campaign chapter cards now include their order, real completion progress and a
miniature of their actual mission graph. The selected chapter pane previews that
graph, an available mission's story (falling back to the first mission), and the
number of available missions. Single-mission chapters show their mission ID.
Route nodes show names when space permits, and selected incident routes use the
ice accent. Header/back geometry matches Settings and Rules. All graph positions,
unlock checks, hit regions and confirmation behavior remain driven by the same
campaign data and layout; miniature graphs are noninteractive previews.
