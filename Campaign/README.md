# Campaign Module

Dependency flow:

```text
Chapter data / JSON extensions -> CampaignCatalog -> CampaignBattleFactory
Skill costs -> CampaignLoadout -> Loadout UI
CampaignBattleSetup -> EncounterCoordinator -> PlayingSceneManager
CampaignProgressStore -> CampaignRuntime -> Campaign map UI
```

- `Data/`: built-in nodes split by chapter. Add a partial file for a new built-in chapter.
- `Infrastructure/`: JSON node extensions and persistence adapters.
- `Battle/`: agent controllers, the controller factory, and reinforcement orchestration.
- `CampaignModels.cs`: domain models with no UI dependency.
- `CampaignSkillBalance.cs`: the single source of truth for skill costs and conflicts.
- `Content/campaign/nodes.json`: adds or overrides nodes without recompiling.

The UI reads campaign models and submits node/loadout choices. Unlock, budget, and
reinforcement rules belong to the campaign layer.

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
