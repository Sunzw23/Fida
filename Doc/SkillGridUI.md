# Skill selection grid

The cold UI loadout concept is implemented with a four-column grid, fixed detail
panel, cost preview and removable equipped chips. The complete 20-skill catalog
uses two desktop pages; the All/Offense/Defense/Control filters retain focus when
possible. A portrait viewport uses three columns and stacked details.

White outlines indicate the skill being inspected; cyan outlines and checkmarks
indicate equipped skills. Locked, over-budget and conflicting states include
text, not just color. Clicking a tile inspects it; the detail action equips or
removes it. Equipped chips remove their skill and an overflow button exposes
additional equipped entries without introducing a slot limit.

Budget costs, unlocks and conflict checks use `CampaignLoadout` and
`CampaignSkillBalance`. The screen reports intent; campaign and network states
still own the selected loadout. Numerical effect examples are marked as base and
result values. Geometric icons are antialiased masks generated at runtime.

## Party strip

`SkillSelectionLayout.Party` reserves a band under the grid (between the page arrows and
the detail card in portrait). `UICampaignPartyStrip` draws it as one paper ticket: a
`PARTY n / limit` label, the companions already invited as read-only chips with their art
and health, and a `CHANGE` affordance on the right.

The ticket is a single target, not a control cluster: a click anywhere on it, `C`, gamepad
`Y`, or Back returns to `UICampaignCompanionSelect`, which owns the party choice (roster
cards, portraits, and the limit). The strip therefore duplicates no rules - it mirrors
`GameSetup.SelectedCompanionIds` every frame and reports one "change party" intent.
Companion selection itself is documented in `Doc/CompanionSelectUI.md`.

## Skill marks

`UI/Campaign/UISkillArt.cs` holds the whole icon set: one 100 x 100 polygon mask per
`SkillType`, built once into a single atlas with 2 x 2 supersampling and drawn tinted, so the
marks cost no texture asset and every screen shares them. Fill and cut polygons are painted in
order, which is how the holes in the crest, the heart and the dial are made.

The vocabulary is deliberately one idea per mark, cut as paper shapes rather than pictographic
detail, because the marks are read at 30-60 px on a card:

- offense is a strike - a bold diagonal cut, doubled for Double Attack, a fang for Dragon;
- defense is a pointed crest, split cleanly through the middle for Double Defense;
- vitality is a heart, banded into three for Triple Health, and a drop for Blood;
- control uses sigils - a dial for Transform, a diamond for Immortal, a crown for Emperor,
  a heavy hexagon for God, chevrons for the extra-step skills, two number tiles for Same;
- the super tier is the base sigil plus one small punch mark in the corner.

The rules codex used to print a two-letter code (`A+`, `DR`, `TR`) where an icon belongs, so the
same skill looked different in the loadout and in the index. `UISkillCodexTile`, the codex medal
and the codex watermark now draw the shared mark. The letter codes survive only in the compact
hover list on a battle card, where they act as short bullets.

Iterate shapes with `tools/comfy/skill_icon_lab.py`, which reproduces the rasteriser exactly
(same 100 x 100 space, same fill/cut order, same supersampling) and renders the set at any size, so
a shape can be judged without rebuilding the game; then paste the points back into the C# switch.

## Compatibility and controls

`UICampaignSkillWheel` retains the established public state-machine contract and
delegates the live UI to `UICampaignSkillGrid`. Its previous implementation is
preserved. Duel ban, pick, lock and cooperative ready flags use the same contract;
disabled Start and frozen selections are enforced before input is dispatched.

- Mouse: click a tile, then Equip/Remove; wheel and arrows change pages.
- Keyboard: arrows navigate by grid position; Enter/Space equip or remove;
  Q/E or Tab change category; Page Up/Down change page; F5 starts; Escape returns.
- Gamepad: D-pad/left stick navigate; A equips/removes; shoulders change category;
  Start confirms; B/Back returns.

## Validation

```powershell
dotnet test Fida.Tests/Fida.Tests.csproj -c UiReview --no-restore -m:1 -p:BuildInParallel=false
dotnet build tools/UiPreview/UiPreview.csproj -c UiReview -m:1 -p:BuildInParallel=false
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1600 1000 artifacts/skill-grid --skills-only
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1280 720 artifacts/skill-grid --skills-only
```

UiReview keeps verification separate from a game running in Visual Studio's
Debug output. The preview copies compiled game content after project references
have built, so a fresh configuration can be verified in one build.

The preview drives real click sequences through the compatibility facade and
checks inspect/equip/remove, duplicate events, unlocks, budget, conflicts, disabled
start, frozen selection, modal blocking, category/page navigation, drag cancellation
and ban-versus-start semantics. It saves normal, second-page, filtered, budget-full,
ready and ban views. Pure tests also cover spatial navigation and desktop/portrait
hit-region containment. Android builds are checked; physical touch/gamepad hardware
has not been exercised by the automated preview.
