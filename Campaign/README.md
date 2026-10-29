# Campaign Module

Dependency flow:

```text
Chapter JSON files -> JsonCampaignNodeProvider -> validation -> CampaignCatalog -> CampaignBattleFactory
Skill costs -> CampaignLoadout -> Loadout UI
CampaignBattleSetup -> EncounterCoordinator -> PlayingSceneManager
CampaignProgressStore -> CampaignRuntime -> Campaign map UI
```

- `Content/campaign/chapters/`: authoritative campaign nodes, split by chapter. Add a JSON file to add a chapter without recompiling.
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

If no unlocked selectable skill fits the node's budget, the loadout is hidden:
after its introduction, a local battle starts automatically. Online play readies
automatically and retains the usual partner readiness/join handshake.

The route begins P -> T01 -> T02 -> T03 -> Chapter I. Existing clears remain
valid. Node F supplies two `endings` (id, label, pages), freely selectable after
the ferry scene; their history is stored without requiring optional clears.
Online players read introductions before readying and choose their own endings.

`selfTargetingEnemy` constrains the Withered King's damaging target to himself.
His number and skill choices still use the same value table as ordinary agents.

## Campaign map extension points

- `CampaignMapOptions` controls card geometry, viewport margins, and zoom limits.
- `CampaignMapCamera` owns resolution-independent pan, zoom, fit, and coordinate transforms.
- `CampaignMapNavigator` implements spatial selection independently of catalog order.
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
