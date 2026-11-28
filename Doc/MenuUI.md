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
it; Begin mission (or Enter / controller A) confirms it. Locked missions can
be inspected but cannot be entered, so their detail ticket is dimmed, reads
"Locked" and carries the lock stamp instead of the action arrow. Folder actions
open the selected chapter;
Back returns through the hierarchy. Zoom and pan remain available. Node sizes
are world units, avoiding a second viewport scale before the camera scales them.

Android prints the same browser on paper. The chapter page lists chapter cards and
the mission page lists mission cards, both cut from the chapter stock with their own
scene plate, a progress counter (`ROUTES 20 / 64` at the root, `MISSIONS 1 / 6` inside
a chapter) and a printed state badge. Every mission keeps a row: clearing a mission no
longer removes it from the page, so the cleared row (stamped `CLEARED`) stays the way
back into that battle. A mission still closed by its parents prints as grey stock
stamped `LOCKED` instead of disappearing, so the rest of the chapter stays readable.
Tapping a row only inspects it; the row opens the mission sheet, which is the chapter
detail pane's three-sheet stack with the mission artwork, the difficulty/vow strip, the
story, `N enemies / N SP`, the reward seal and one action ticket (`Begin mission`,
`Replay mission`, or a dimmed `Locked`). The ticket is the only start affordance.

`UI/Campaign/CampaignMissionList.cs` owns the row states and the replay rule,
`UI/Campaign/CampaignMissionLayout.cs` owns the sheet geometry, and
`UI/Campaign/UICampaignRouteView.cs` draws both levels. A clear outranks the parent
gate, and `GameSetup.SelectCampaignNode` accepts a cleared node, so a co-op clear that
merged into a lane without its parents is still replayable.

The main-menu numerals use a separately cached display-resolution mask of the
existing numeral geometry; combat numerals retain their original atlas.

## Verification

Run the regular unit suite and build both desktop and Android. The local preview
runner also exercises map inspect/confirm, locked mission rejection, locked
chapter sheet colour, the locked detail ticket, cross-target mouse release and
single-consumption of navigation events without modifying saves.

The Android layout is previewed from the same source with `FIDA_ANDROID` defined.
Its chapter pass also taps a mission row, draws the mission sheet, and asserts the
cleared row's `Replay mission` ticket starts that node while the locked row's ticket
never does:

```powershell
dotnet test Fida.Tests/Fida.Tests.csproj -c UiReview --no-restore -m:1 -p:BuildInParallel=false
dotnet build tools/UiPreview/UiPreview.csproj -c UiReview --no-restore -m:1 -p:BuildInParallel=false
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1600 1000 artifacts/editorial-ui --menus-only
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1280 720 artifacts/editorial-ui --menus-only
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 720 1280 artifacts/editorial-ui --menus-only
dotnet build tools/UiPreview/UiPreview.csproj -c UiReview -p:DefineConstants=FIDA_ANDROID -p:SkipDotnetToolRestore=true -m:1
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1080 2400 artifacts/android-paper --chapters-only --tolerant
```

Screenshots include the normal and connected menus, settings, campaign chapters,
a real chapter route, and lobby before/after local readiness. This checks actual
MonoGame rendering rather than reproducing the concept art. Android packaging
and shared code compile checks do not replace physical touch/device testing.

## Rules book and online reference screens

Rules is a small book: three reference pages (a turn, number effects, the skill
stages), a skill index, then one page per discovered skill. Page numbering is
`3 + 1 + N` and belongs to `UI/Rules/RulesBook.cs`. Discovery is owned (campaign
unlocks in either lane) plus encountered (the codex, and the skills implied by
finished campaign nodes); undiscovered skills never appear.

A skill page carries four blocks: kicker (family, cost, NEW), medal and name, one
effect sentence with one worked example between hairlines, and a single footnote
line with stage, limit and source. The timing page lists the eight stages and
links the discovered skills that fire at each; those names open their page.

The index is a paginated grid of paper tiles (4x3 landscape, 2x5 portrait): filled
tiles are owned, faded ones were only seen, and a cyan dot marks an unread page.
There is no legend line - the tile state is meant to read without one.
Chips jump between the four chapters, and left/right turn pages in every input
mode. Portrait windows keep the same structure with two chip rows.

`SkillCodex` persists discovery to `%LOCALAPPDATA%/Fida/skill-codex.json` by skill
name, so enum reordering cannot break a save. It is append-only; campaign
completion is derived at read time instead of migrating old saves. The rules
screen only reads it.

Online separates Create room and Join room. A four-cell code field retains the
existing letters/digits keyboard handling, length limit and Android keyboard.
Join is unavailable until a complete relay code is entered. Enter queues a join
intent for state input handling, avoiding a screen rebuild inside control Update.
Direct-server sessions hide the relay-code input and can join without a code.
Connected, hosting and connecting screens share the header and actions; status
updates preserve the existing named-label interfaces. Connection messages wrap.

Use `--reference-only` in UiPreview to render the book — reference pages, the
index (both index pages when the profile has more than twelve skills), the first
and last skill page, and the empty profile — plus online, connected, hosting and
connecting screens. It exercises incomplete/complete code entry, length limiting,
deletion, Enter submission and Join activation using the actual controls, without
opening network connections or changing user saves.

Campaign chapter cards now include their order, real completion progress and a
miniature of their actual mission graph. The selected chapter pane previews that
graph, an available mission's story (falling back to the first mission), and the
number of available missions. Single-mission chapters show their mission ID.
Route nodes show names when space permits, and selected incident routes use the
ice accent. Locked missions and locked chapters share one grey sheet, faded
artwork and a lock stamp, so a chapter cannot look available just because it is
one level above the mission that is actually closed. Header/back geometry
matches Settings and Rules. All graph positions, unlock checks, hit regions and
confirmation behavior remain driven by the same campaign data and layout;
miniature graphs are noninteractive previews.
