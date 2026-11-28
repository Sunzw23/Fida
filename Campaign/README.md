# Campaign Module

Dependency flow:

```text
Chapter JSON files -> JsonCampaignNodeProvider -> validation -> CampaignCatalog -> CampaignBattleFactory
Chapter index (chapter-index.json) -> CampaignChapters -> map UI / chapter audits
Skill costs -> CampaignLoadout -> Loadout UI
Companion roster (companions.json) -> CampaignCompanions -> CampaignLoadout -> Loadout UI
CampaignBattleSetup -> EncounterCoordinator -> PlayingSceneManager
CampaignProgressStore -> CampaignRuntime -> Campaign map UI
```

- `Content/campaign/chapters/`: authoritative campaign nodes, split by chapter. Add a JSON file to add a chapter without recompiling.
- `Content/campaign/chapter-index.json`: chapter registry (kind, palette, art, summary, order). Adding a chapter is a node file plus one index entry; no code change.
- `Infrastructure/`: strict JSON loading and persistence adapters.
- `Battle/`: agent controllers, the controller factory, and reinforcement orchestration.
- `CampaignModels.cs`: domain models with no UI dependency.
- `CampaignSkillBalance.cs`: the single source of truth for skill costs and conflicts.
- Campaign JSON uses string enum names for `skills`, `rewards`, and `agentType`.
- Every loaded file is combined and the complete graph is validated before use. Duplicate IDs, missing parents, cycles, invalid skills, empty encounters, and teams larger than four are rejected.

The UI reads campaign models and submits node/loadout choices. Unlock, budget, and
reinforcement rules belong to the campaign layer.

## The Long Road story

Chapter JSON also owns short `beforeStory` and `afterStory` arrays of
`{ "speaker": "Arith", "text": "..." }` pages. Introductions appear before
the loadout becomes interactive; aftermath appears only after a clear. Click,
Enter, Space or controller confirm advances; Escape skips the current sequence.
Keep each page brief. The complete Chinese story remains in `Doc/Story.md`.
Arith is a silent protagonist: use brief visible actions instead of spoken lines
or internal monologues. Other characters should not explain his feelings for him.

If no unlocked selectable skill fits the node's budget and nobody may join the battle,
the loadout is hidden: after its introduction, a local battle starts automatically.
Online play readies automatically and retains the usual partner readiness/join handshake.
A node that offers a companion always opens the companion screen first, and that screen
replaces the loadout entirely on zero-SP lessons, so the party choice is never skipped.

## Companions

`Content/campaign/companions.json` is the global roster of allies a player may invite.
Adding a companion is a data edit; no code or node change is needed.

```json
{ "id": "mara_venn", "name": "Mara Venn", "portraitKey": "tutorial_ally",
  "artKey": "dialogue_mara", "atlasCell": 1, "health": 10,
  "agentType": "Adaptive", "skills": [], "unlockNode": "", "description": "..." }
```

- `agentType: "Adaptive"` selects `AdaptiveCompanionController` (Mara): her decision
  strength follows the party's lowest health ratio instead of the default agent profile.
- `passive: "gate_ward"` gives Bram the first-hit ward while both of his numbers are 1.
- `summons: { name, health, skills }` gives Sable an Ink Echo; `CampaignBattleRules`
  respawns it after two of her turns and the scene rebuilds the board on the
  `Summoned` / `Removed` events.
- `unlockNode` hides a companion until that node is cleared; `Ila` unlocks from `AS3`.
  Pass `CampaignCompanions.Available(node, progress)` for the real roster and omit the
  progress for the unfiltered list used by tests and previews.
- The party is chosen on a dedicated roster screen (`UICampaignCompanionSelect`) with
  character art. It starts empty: the player fights alone unless they invite someone.
  `CampaignCompanions.PartyLimit` caps a party at two companions, and the effective limit
  for a node is the smaller of that cap and the roster the node offers.
- A node may narrow the roster with an optional `companions` array of ids. An explicit
  `[]` forbids companions for that battle; the prologue uses it because Arith has not met
  Mara yet. Unknown ids are rejected by `CampaignGraphValidator`.
- `CampaignLoadout` owns every party rule: `SanitizeCompanions` (unknown/forbidden/over-limit
  ids dropped), `ToggleCompanion` (invite or dismiss) and `CanSelectCompanion`.
  `GameSetup.SelectedCompanionIds` holds the current party and resets on node selection.
- `CampaignBattleFactory.Create(node, skills, localPlayerId, companionIds)` seats each
  invited companion on faction `0` with ids `1..n`, shifting enemies to `n+1..`; the
  invitation order decides the ids. Cooperative battles never add a campaign companion:
  the second human fills that slot.
- Art resolution lives in `UICompanionArt`: `artKey` names a registered texture,
  `artPath` registers one on first use, `atlasCell` falls back to the shared 3x2 roster
  atlas, and anything unresolved still draws an atlas bust. Adding a companion with its
  own 立绘 therefore needs only a content entry plus `artKey`/`artPath`.
- The skill loadout only shows a read-only party strip. `C`/gamepad `Y`, a click on the
  strip, or Back from the loadout returns to the roster screen.

The route begins P -> T01 -> T02 -> T03 -> Chapter I. Existing clears remain
valid. Node F supplies two `endings` (id, label, pages), freely selectable after
the ferry scene; their history is stored without requiring optional clears.
Online players read introductions before readying and choose their own endings.

`selfTargetingEnemy` constrains the Withered King's damaging target to himself.
His number and skill choices still use the same value table as ordinary agents.

## Chapters and side chapters

`Content/campaign/chapter-index.json` declares every chapter once. A node inherits all of
it from its chapter path (its `chapter` string split on `" - "`), so adding content never
touches the UI:

```json
{ "id": "side-smith", "path": ["The Smith's Errand"], "title": "The Smith's Errand",
  "kind": "side", "palette": "frost", "order": 31,
  "summary": "A finished sword, and a delivery nobody has been able to make." }
```

- `kind` is `main` or `side`. Side chapters never gate a main node; `CampaignChapters.Audit`
  also reports main chapters that lead nowhere ("pretend mainline").
- `palette` is a key, not a colour: `paper` (main), `frost` (side), `slate` (final approach).
  The UI maps keys to stock/ink/accent in `CampaignChapterPalette`; no new textures or screens.
- `art` reuses one of the existing chapter plates (`bell tower`, `courtyard`, `dragon tomb`,
  `blank archive`); unknown/absent values fall back to `UIPaper.ChapterIndex`.
- The map renders one kind per level: a page shows chapter cards when it has sub-chapters,
  otherwise mission cards. `Chapter I`, `Chapter II` and `Chapter III` are therefore pure
  containers (`The Wall` holds Chapter I's missions); side chapters hang under their own
  container, and `The Roofless Shelter` is the one top-level side chapter.
- Main chapter counters count main nodes only; side chapters count their own nodes.
  Main folders are numbered `01`, `02`…; side folders `S1`, `S2`… on their level.

## Difficulty contract

Nodes may declare `tier` (1-4), `vows` (snake_case strings) and `playerHealth` (default 11).
`CampaignDifficulty.Audit` checks that the battle data actually matches what the node claims:

| tier | units | total health | vows |
|---|---:|---:|---:|
| 1 | 2-3 | 6-12 | 0 |
| 2 | 3 | 12-17 | 0-1 |
| 3 | 3-4 | 15-21 | 1-2 |
| 4 | 4 | 21-28 | 2-3 |

- `bare_hands` forces `skillBudget: 0` and allows at most 3 units / 18 health, never combined
  with `wounded`; `alone` requires an explicit empty `companions` array; `wounded` requires
  `playerHealth` 5-9; `outnumbered` requires four starting enemies and no reinforcements.
- `Xue`/`Shi`/`Shen` may sit on two enemies at most (sustain turns fights into grinds), and
  reinforcements cannot arrive before round 2.
- Nodes without a `tier` are unclassified and skip the contract, which keeps pre-expansion
  content valid. `CampaignGraphValidator.Validate` returns every problem as text and the
  catalog refuses to load invalid content.

## Campaign map extension points

- `CampaignMapOptions` controls card geometry, viewport margins, and zoom limits.
- `CampaignMapCamera` owns resolution-independent pan, zoom, fit, and coordinate transforms.
- `CampaignMapNavigator` implements spatial selection independently of catalog order.
- `CampaignMissionList` (`UI/Campaign/CampaignMissionList.cs`) owns the phone mission rows.
  Every mission on the page keeps a row: a cleared mission prints as `CLEARED` and stays the
  way back into that battle, a locked mission prints as grey stock instead of disappearing, and
  `InitialFocus` picks the next open mission. A clear outranks the parent gate, so a merged
  co-op clear is still replayable; `GameSetup.SelectCampaignNode` accepts the same case.
- `CampaignMissionLayout` owns the phone mission sheet geometry, and `UICampaignRouteView` draws
  the rows and the sheet with the chapter stock, vow strip, reward seal and shared action ticket.
- `UICampaignMap` is the input/rendering adapter. It consumes any `IReadOnlyList<CampaignNode>`,
  so additional JSON or built-in nodes appear without new buttons or state bindings.

Supported browsing input:

- Mouse: click a node, drag the canvas, use the wheel to zoom, or use `- / Fit / +`.
- Keyboard: arrows select nodes, WASD pans, `+/-` zooms, Home resets, Enter opens.
- Controller: D-pad selects, right stick pans, shoulder buttons/triggers zoom, Y resets, A opens.

## Skill wheel extension points

- `SkillWheelModel` owns circular single-focus selection and can be tested without graphics.
- `CampaignSkillWheelOptions` controls visible neighbors, spacing, card size, and text scale.
- `UICampaignSkillWheel` renders the wheel and reports toggle/back/start intents without mutating game state.
- `CampaignLoadout.Evaluate` supplies explicit availability states for locked, conflicting, and over-budget skills.

The wheel always highlights one focused skill. Confirming it equips or removes that skill, while the
loadout may still contain multiple skills up to the node's point budget.
